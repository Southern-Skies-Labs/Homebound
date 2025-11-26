using Homebound.Features.AethianAI.States;
using UnityEngine;
using UnityEngine.AI;
using Homebound.Features.TaskSystem;
using System;
using Homebound.Features.TimeSystem;
using Homebound.Core;
using Homebound.Features.Identity;

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
        
        
    }
}

