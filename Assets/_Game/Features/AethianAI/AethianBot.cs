using Homebound.Features.AethianAI.States;
using UnityEngine;
using UnityEngine.AI;
using Homebound.Features.TaskSystem;
using System;
using Homebound.Features.TimeSystem;
using Homebound.Core;
using Homebound.Features.Identity;
using Homebound.Features.Navigation;

namespace Homebound.Features.AethianAI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AethianBot : MonoBehaviour
    {
        //Variables
        [Header("Data")] 
        public AethianStats Stats = new AethianStats();

        public event Action<string> OnStateChanged; 
        
        [Header("Debug")]
        [SerializeField] private string _currentStateName;

        [Header("Anti-Stuck Settings")] 
        [SerializeField] private GameObject _emergencyLadderPrefab;
        [SerializeField] private float _stuckThreshold = 2.0f;
        
        //Monitoreo 
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private bool _isRecovering;
        
        private TimeManager _timeManager;
        private float _lastHourCheck;
        
        
        //Componentes
        public NavMeshAgent Agent { get; private set; }
        
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
          
            Agent = GetComponent<NavMeshAgent>();
            if (Agent.enabled) Agent.enabled = false;

            StateIdle = new StateIdle(this);
            StateWorking = new StateWorking(this);
            StateSurvival = new StateSurvival(this);
            StateSleep = new StateSleep(this);
            StateGather = new StateGather(this);
        }
        
        protected virtual System.Collections.IEnumerator Start()
        {
            // --- FASE 1: ATERRIZAJE SEGURO ---
            if (Agent.enabled) Agent.enabled = false;
            
            bool sueloEncontrado = false;
            int intentos = 0;

            while (!sueloEncontrado && intentos < 20)
            {
                NavMeshHit hit;
                // Buscamos suelo en un radio de 2 metros
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    Agent.enabled = true;
                    sueloEncontrado = true;
                    Debug.Log($"[AethianBot] Aterrizaje exitoso. Unidad lista.");
                }
                else
                {
                    // Si no encuentra suelo, espera un poco
                    yield return new WaitForSeconds(0.1f);
                    intentos++;
                }
            }

            if (!sueloEncontrado)
            {
                Debug.LogError($"[AethianBot] CRÍTICO: La unidad {name} no encontró NavMesh y se desactivará.");
                yield break; // Aquí sí cancelamos porque sin suelo no hay nada que hacer
            }

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
            if (Agent.isOnNavMesh)
            {
                Agent.SetDestination(position);
                Agent.isStopped = false;
            }
        }

        public void StopMoving()
        {
            if (Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
            }
        }

        public bool HasReachedDestination()
        {
            if (!Agent.isOnNavMesh) return false;        
            
            if(Agent.pathPending) return false;
            return Agent.remainingDistance <= Agent.stoppingDistance;
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
            if (_isRecovering || CurrentJob == null || !Agent.isOnNavMesh) 
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

            float maxClimbHeight = 30f;
            float checkDistance = 2.5f;
            LayerMask obstacleLayer = LayerMask.GetMask("Default", "Resource", "Ground");

            Vector3 startPos = transform.position;
            Vector3 forward = transform.forward;
            
            
            //Verificación 1: La subida o escalada de muros
            bool wallDetected = Physics.Raycast(startPos + Vector3.up * 0.5f, forward, out RaycastHit wallHit, checkDistance, obstacleLayer);

            if (wallDetected)
            {
                Debug.Log("[AntiStuck] Obstaculo detectado. Evaluando subida...");
                Vector3 skyOrigin = wallHit.point + (forward * 0.6f) + (Vector3.up * maxClimbHeight);
                
                
                if (Physics.Raycast(skyOrigin, Vector3.down, out RaycastHit topHit, maxClimbHeight, obstacleLayer))
                {
                    float heightDiff = topHit.point.y - startPos.y;

                    if (heightDiff > 0.5f && heightDiff <= maxClimbHeight)
                    {
                        Debug.Log($"[AntiStuck] Subida viable encontrada ({heightDiff:F1}m). Construyendo escalera");
                        
                        BuildEmergencyLadder(startPos - (forward * 1.0f), topHit.point);
                        yield break;
                    }

                }
                else
                {
                    Debug.LogWarning("[AntiStuck] No se encontró techo viable para la escalera.");
                }
            }
            
            //Verificacion 2: La bajada o descenso de muros
            Vector3 ledgeCheckPos = startPos + (forward * 1.5f);

            if (!Physics.Raycast(ledgeCheckPos, Vector3.down, 1.0f, obstacleLayer))
            {
                Debug.Log("[AntiStuck] Posible precipicio. Escaneando fondo...");

                if (Physics.Raycast(ledgeCheckPos, Vector3.down, out RaycastHit groundHit, maxClimbHeight, obstacleLayer))
                {
                    float dropHeight = startPos.y - groundHit.point.y;
                    
                    //Si la bajada o caída es mayor a 1m, o sea 1 bloque
                    if (dropHeight > 1.0f)
                    {
                        Debug.Log($"[AntiStuck] Bajada viable encontrada ({dropHeight:F1}m). Construyendo escalera.");

                        Vector3 ladderTop = startPos + (forward * 0.2f);
                        Vector3 ladderBottom = groundHit.point;

                        ladderBottom = new Vector3(ladderTop.x, groundHit.point.y, ladderTop.z);
                        
                        BuildEmergencyLadder(ladderBottom, ladderTop);
                        yield break;
                    }
                }
            }
            
            //PLAN B: Si la solución dinamica no funciona (Muro muy alto o techo), solo entonces usamos el Warp. Esto DEBE ser una mecanica de emergencia.
            
            Debug.LogWarning("[AntiStuck] No se encontró solución de escalada viable. Ejecutando Teletransporte.");
            yield return new WaitForSeconds(1.0f); 

            GameObject banner = GameObject.FindGameObjectWithTag("Respawn");
            if (banner != null)
            {
                Agent.Warp(banner.transform.position);
                Agent.ResetPath();
                ChangeState(StateIdle);
            }
            else
            {
                // Teletransporte de emergencia local (salto cuántico hacia arriba)
                Agent.Warp(transform.position + Vector3.up * 3f); 
            }

            _isRecovering = false;
            _stuckTimer = 0f;
        }
        
        private void BuildEmergencyLadder(Vector3 bottom, Vector3 top)
        {
            GameObject ladderObj = Instantiate(_emergencyLadderPrefab);
            LadderController ladder = ladderObj.GetComponent<LadderController>();

            ladder.Initialize(bottom, top, LadderType.Emergency, 15f);
            
            var ladderManager = Homebound.Core.ServiceLocator.Get<LadderManager>();
            if (ladderManager != null) ladderManager.RegisterLadder(ladder);

            StartCoroutine(ResumeMovementRoutine());
        }

        private System.Collections.IEnumerator ResumeMovementRoutine()
        {
            Agent.ResetPath();
            yield return new WaitForSeconds(0.5f);
            if (CurrentJob != null) MoveTo(CurrentJob.Position);
            _isRecovering = false;
            _stuckTimer = 0f;
        }
    
        //Este metodo verifica si es posible llegar al destino antes de intentarlo.
        public bool IsPathReachable(Vector3 targetPos)
        {
            NavMeshPath path = new NavMeshPath();
            NavMeshHit hit;

            if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }
            else
            {
                return false;
            }
            
            
            Agent.CalculatePath(targetPos, path);
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                return true;
            }
            
            return path.status == NavMeshPathStatus.PathComplete;
        }
        
        
    }
}

