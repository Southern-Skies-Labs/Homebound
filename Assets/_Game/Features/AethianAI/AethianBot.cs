using Homebound.Features.AethianAI.States;
using UnityEngine;
// using UnityEngine.AI;
using Homebound.Features.TaskSystem;
using System;
using Homebound.Features.TimeSystem;
using Homebound.Core;
using Homebound.Features.Identity;
using Homebound.Features.Navigation;
using Homebound.Features.Navigation.Pathfinding;

namespace Homebound.Features.AethianAI
{
    [RequireComponent(typeof(GridMovementAgent))]
    public class AethianBot : MonoBehaviour
    {
        //Variables
        [Header("Data")] 
        public AethianStats Stats = new AethianStats();

        public event Action<string> OnStateChanged; 
        
        [Header("Debug")]
        [SerializeField] private string _currentStateName;

        [Header("Anti-Stuck Settings")] 
        [SerializeField] private float _stuckThreshold = 2.0f;
        
        //Monitoreo 
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private bool _isRecovering;
        
        private TimeManager _timeManager;
        private float _lastHourCheck;
        
        
        //Componentes
        public GridMovementAgent Agent { get; private set; }
        private VoxelPathfinder _pathfinder;
        
        //Estado actual
        private AethianState _currentState;
        public JobRequest CurrentJob { get; set; } //Tarea actual
        
        //Definicion de los estados posibles, para que no se creen en cada frame
        public AethianState StateIdle { get; private set; }
        public AethianState StateWorking { get; private set; }
        public AethianState StateSurvival { get; private set; }
        
        public AethianState StateSleep { get; private set; }
        public AethianState StateGather { get; private set; }
        
        //Metodos
        protected virtual void Awake()
        {
            Agent = GetComponent<GridMovementAgent>();
            // Buscamos o añadimos el Pathfinder. Idealmente ServiceLocator o Componente
            _pathfinder = GetComponent<VoxelPathfinder>();
            if (_pathfinder == null) _pathfinder = gameObject.AddComponent<VoxelPathfinder>();

            StateIdle = new StateIdle(this);
            StateWorking = new StateWorking(this);
            StateSurvival = new StateSurvival(this);
            StateSleep = new StateSleep(this);
            StateGather = new StateGather(this);
        }
        
        protected virtual System.Collections.IEnumerator Start()
        {
            // --- FASE 1: ATERRIZAJE SEGURO ---
            // Simplemente validamos que estamos en un lugar válido del Grid
            // El CharacterController se encarga del suelo
            
            // --- FASE 2: CONEXIÓN AL TIEMPO ---
            // Ahora que ya estamos en el suelo, buscamos el reloj
            int timeRetries = 0;
            while (_timeManager == null && timeRetries < 10)
            {
                _timeManager = ServiceLocator.Get<TimeManager>();
                if (_timeManager == null) yield return new WaitForSeconds(0.1f);
                timeRetries++;
            }

            if (_timeManager != null)
            {
                _lastHourCheck = _timeManager.CurrentHour;
                // Debug.Log($"[AethianBot] {name} conectado al sistema de tiempo.");
            }
            else
            {
                Debug.LogError($"[AethianBot] {name} NO encontró el TimeManager. Sus necesidades no bajarán.");
            }

            AssignRandomIdentity();            
            // --- FASE 3: INICIO DE IA ---
            ChangeState(StateIdle);
            
            
            
        }
        
        protected virtual void Update()
        {
            // 1. Metabolismo: Bajar necesidades según el tiempo del juego
            if (_timeManager != null)
            {
                float currentHour = _timeManager.CurrentHour;
                
                // Calculamos cuánto tiempo pasó desde el último frame
                float deltaHours = currentHour - _lastHourCheck;
                
                // Ajuste por si pasamos de las 23:59 a las 00:00
                if (deltaHours < 0) deltaHours += 24f; 

                if (deltaHours > 0)
                {
                    Stats.UpdateNeeds(deltaHours);
                    _lastHourCheck = currentHour;
                }
            }
            
            //*MONITOR ANTI STUCK*
            UpdateAntiStuck();
            
            // 2. Transiciones de Estado
            CheckGlobalTransitions();

            // 3. Ejecutar Estado Actual
            _currentState?.Tick();
        }
        
        //Metodos de Movimiento
        public void MoveTo(Vector3 position)
        {
             // Solicitamos path y se lo asignamos al agente
             var path = _pathfinder.FindPath(transform.position, position);
             if (path != null)
             {
                 Agent.SetPath(path);
             }
             else
             {
                 // Si no hay camino, quizás deberíamos activar anti-stuck inmediatamente o notificar
                 Debug.LogWarning($"[AethianBot] No se encontró camino hacia {position}");
             }
        }

        public void StopMoving()
        {
            Agent.Stop();
        }

        public bool HasReachedDestination()
        {
            return Agent.HasReachedDestination();
        }
        
        // MEtodos de Gestión de estados
   
        // ReSharper disable Unity.PerformanceAnalysis
        private void CheckGlobalTransitions()
        {
            if(ShouldIgnoreHunger()) return;

            bool isNight = _timeManager != null && (_timeManager.CurrentHour >= 20 || _timeManager.CurrentHour < 6);
            if (Stats.Energy.IsCritical() || isNight)
            {
                ChangeState(StateSleep);
            }

            if (Stats.Hunger.IsCritical() && !(_currentState is StateSurvival))
            {
                Debug.LogWarning($"[Aethian] {name} tiene hambre critica!");
                ChangeState(StateSurvival);
            }

        }
        
        
        //Metodo virtual para que clases hijas (clases de combate) puedan sobreescribirlo
        protected virtual bool ShouldIgnoreHunger() => false;


        public void ChangeState(AethianState newState)
        {
            _currentState?.Exit();
            _currentState = newState;

            string stateName = _currentState.GetType().Name.Replace("State", "");
            OnStateChanged?.Invoke(stateName);
            
            _currentState.Enter();
        }
        
        private void AssignRandomIdentity()
        {
            if (!string.IsNullOrEmpty(Stats.CharacterName) && Stats.CharacterName != "Aethian") 
                return;

            var nameService = Homebound.Core.ServiceLocator.Get<NameGeneratorService>();
            if (nameService != null)
            {
                // Aleatorizar género
                Gender rndGender = (UnityEngine.Random.value > 0.5f) ? Gender.Male : Gender.Female;

                // Asignar nombre
                Stats.CharacterName = nameService.GetRandomName(Race.Aethian, rndGender);

                // Actualizar UI inmediatamente para que se vea el cambio
                string stateName = _currentState != null ? _currentState.GetType().Name.Replace("State", "") : "Idle";
                OnStateChanged?.Invoke(stateName);

                Debug.Log($"[AethianBot] Ha nacido/llegado: {Stats.CharacterName}");
            }
        }

        private void UpdateAntiStuck()
        {
            if (_isRecovering || CurrentJob == null)
            {
                _stuckTimer = 0f;
                return;
            }
            
            float distToJob = Vector3.Distance(transform.position, CurrentJob.Position);
            float distMoved = Vector3.Distance(transform.position, _lastPosition);

            // CONDICIÓN DE ATASCO SIMPLIFICADA:
            if (distToJob > 2.0f && distMoved < 0.05f * Time.deltaTime)
            {
                _stuckTimer += Time.deltaTime;
                // Debug.Log($"[AntiStuck] Atascado... {_stuckTimer:F1}");

                if (_stuckTimer > _stuckThreshold)
                {
                    StartCoroutine(ExecuteEmergencyProtocol());
                }
            }
            else
            {
                
                _stuckTimer = 0f;
            }
            
            _lastPosition = transform.position;
        }
        
        private System.Collections.IEnumerator ExecuteEmergencyProtocol()
        {
            _isRecovering = true;
            Debug.LogWarning($"[AntiStuck] Iniciando protocolos de emergencia...");

            var navSolver = ServiceLocator.Get<NavigationSolver>();
            bool ladderBuilt = false;

            if (navSolver != null && CurrentJob != null)
            {
                var solution = navSolver.GetRecoverySolution(transform.position, CurrentJob.Position);

                if (solution.IsValid)
                {
                    var ladderManager = ServiceLocator.Get<LadderManager>();
                    if (ladderManager != null)
                    {
                        ladderManager.BuildLadder(solution);
                        ladderBuilt = true;
                        Debug.Log("[AntiStuck] Solución de navegacion encontrada, escalera construida");
                        StartCoroutine(ResumeMovementRoutine());
                        yield break;
                    }
                }
            }

            //PLAN B: Si la solución dinamica no funciona (Muro muy alto o techo), solo entonces usamos el Warp. Esto DEBE ser una mecanica de emergencia.
            if (!ladderBuilt)
            {
                Debug.LogWarning("[AntiStuck] No se encontró solución de escalada viable. Ejecutando Teletransporte.");
                yield return new WaitForSeconds(1.0f);

                GameObject banner = GameObject.FindGameObjectWithTag("Respawn");
                CharacterController cc = Agent.GetComponent<CharacterController>();

                if (banner != null)
                {
                    if(cc) cc.enabled = false;
                    transform.position = banner.transform.position;
                    if(cc) cc.enabled = true;

                    Agent.Stop();
                    ChangeState(StateIdle);
                }
                else
                {
                    // Teletransporte de emergencia local (salto cuántico hacia arriba)
                    if(cc) cc.enabled = false;
                    transform.position = transform.position + Vector3.up * 3f;
                    if(cc) cc.enabled = true;
                }
            }

            _isRecovering = false;
            _stuckTimer = 0f;
        }
        

        private System.Collections.IEnumerator ResumeMovementRoutine()
        {
            Agent.Stop();
            yield return new WaitForSeconds(0.5f);
            if (CurrentJob != null) MoveTo(CurrentJob.Position);
            _isRecovering = false;
            _stuckTimer = 0f;
        }
    
        //Este metodo verifica si es posible llegar al destino antes de intentarlo.
        public bool IsPathReachable(Vector3 targetPos)
        {
             // Usamos el pathfinder para verificar
             var path = _pathfinder.FindPath(transform.position, targetPos);
             return path != null && path.Count > 0;
        }
        
        
    }
}

