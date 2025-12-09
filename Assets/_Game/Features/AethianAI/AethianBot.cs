using Homebound.Features.AethianAI.States;
using UnityEngine;
using Homebound.Features.TaskSystem;
using System;
using Homebound.Features.TimeSystem;
using Homebound.Core;
using Homebound.Features.Identity;
using Homebound.Features.Navigation;

namespace Homebound.Features.AethianAI
{
    public class AethianBot : MonoBehaviour, IJobWorker
    {
        //Variables
        [Header("Data")] 
        public AethianStats Stats = new AethianStats();

        public event Action<string> OnStateChanged; 
        
        [Header("Debug")]
        [SerializeField] private string _currentStateName;

        [Header("Behavior")] public ScheduleProfile Schedule;
        
        private TimeManager _timeManager;
        private float _lastHourCheck;
        
        public Vector3 Position => transform.position;

        private UnitMovementController _mover; // Nuestro nuevo chofer
        
        public UnitClass Class => Stats.Class;
        
        //Estado actual
        private AethianState _currentState;
        public JobRequest CurrentJob { get; set; } 
        
        // Estados
        public AethianState StateBuilding { get; private set; }
        public AethianState StateIdle { get; private set; }
        public AethianState StateWorking { get; private set; }
        public AethianState StateSurvival { get; private set; }
        public AethianState StateSleep { get; private set; }
        public AethianState StateGather { get; private set; }
        
        protected virtual void Awake()
        {

            _mover = GetComponent<UnitMovementController>();
            

            if (_mover == null) Debug.LogError("¡Falta el UnitMovementController en el Prefab!");

            StateIdle = new StateIdle(this);
            StateWorking = new StateWorking(this);
            StateSurvival = new StateSurvival(this);
            StateSleep = new StateSleep(this);
            StateGather = new StateGather(this);
            StateBuilding = new StateBuilding(this);
        }
        
        protected virtual System.Collections.IEnumerator Start()
        {
            // FASE 1: ATERRIZAJE SEGURO 
            bool sueloEncontrado = false;
            int intentos = 0;

            while (!sueloEncontrado && intentos < 20)
            {
                // Lanzamos un rayo desde un poco arriba de la unidad hacia abajo
                Ray ray = new Ray(transform.position + Vector3.up * 2f, Vector3.down);
                RaycastHit hit;

                // Buscamos colisión con cualquier cosa sólida (VoxelWorld)
                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    // "Teletransportamos" la unidad justo al punto de impacto
                    transform.position = hit.point;
                    sueloEncontrado = true;
                    Debug.Log($"[AethianBot] Aterrizaje físico exitoso en {hit.point}.");
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                    intentos++;
                }
            }

            if (!sueloEncontrado)
            {
                Debug.LogError($"[AethianBot] CRÍTICO: La unidad {name} está flotando en el vacío (No hay suelo debajo).");
                
            }

            // FASE 2: CONEXIÓN AL TIEMPO
            int timeRetries = 0;
            while (_timeManager == null && timeRetries < 10)
            {
                _timeManager = ServiceLocator.Get<TimeManager>();
                if (_timeManager == null) yield return new WaitForSeconds(0.1f);
                timeRetries++;
            }

            if (_timeManager != null)
            {
                _lastHourCheck = _timeManager.CurrentTime.Hour;
            }
            else
            {
                Debug.LogError($"[AethianBot] {name} NO encontró el TimeManager.");
            }

            AssignRandomIdentity();            
            
            //FASE 3: INICIO DE IA
            ChangeState(StateIdle);
        }
        
        protected virtual void Update()
        {
            
            if (_currentState != null) _currentStateName = _currentState.GetType().Name;

            // 1. Metabolismo
            if (_timeManager != null)
            {
                float currentHour = _timeManager.CurrentTime.Hour;
                float deltaHours = currentHour - _lastHourCheck;
                if (deltaHours < 0) deltaHours += 24f; 

                if (deltaHours > 0)
                {
                    Stats.UpdateNeeds(deltaHours);
                    _lastHourCheck = currentHour;
                }
            }
            
            if (CurrentJob != null && CurrentJob.IsCancelled)
            {
                Debug.Log($"[Aethian] {name}: Mi tarea '{CurrentJob.JobName}' fue cancelada. Parando.");
                StopMoving(); // Frenamos el motor
                CurrentJob = null; // Olvidamos la tarea
                ChangeState(StateIdle); // Volvemos a buscar trabajo
                return; // Saltamos el resto del frame
            }
            
            // 2. Transiciones
            CheckGlobalTransitions();

            // 3. Ejecutar Estado
            _currentState?.Tick();
        }
        
        
        public void MoveTo(Vector3 position)
        {
            // Delegamos la tarea a nuestro nuevo controlador
            if (_mover != null)
            {
                _mover.MoveTo(position);
            }
        }

        public void StopMoving()
        {
            if (_mover != null)
            {
                _mover.StopMoving();
            }
        }

        public bool HasReachedDestination()
        {
            if (_mover == null) return true;
            return !_mover.IsMoving;
        }
        
        // ------------------------------------------------
        
        // MEtodos de Gestión de estados
        // ReSharper disable Unity.PerformanceAnalysis
        private void CheckGlobalTransitions()
        {
            if (_timeManager == null || Schedule == null) return;
            
            int currentHour = _timeManager.CurrentTime.Hour;
            ActivityType scheduledActivity = Schedule.GetActivityAt(currentHour);
            
            // Prioridad 1: COMBATE / AMENAZA (Futuro)
            // if (CombatManager.IsUnderAttack) { ChangeState(StateCombat); return; }

            // Prioridad 2: SUEÑO SAGRADO (Por Horario)
            if (scheduledActivity == ActivityType.Sleep)
            {
                if (!(_currentState is StateSleep))
                {
                    ChangeState(StateSleep);
                }
                return; 
            }
            
            if (_currentState is StateSleep && scheduledActivity != ActivityType.Sleep)
            {
                Debug.Log($"[Aethian] {name}: ¡Ring Ring! Hora de levantarse.");
                ChangeState(StateIdle); 
            }

            // Prioridad 3: SUPERVIVENCIA (Hambre/Sed)
            if (Stats.Hunger.IsCritical() && !(_currentState is StateSurvival))
            {
                Debug.LogWarning($"[Aethian] {name}: Hambre crítica ({Stats.Hunger.Value:F1}%). Buscando comida.");
                ChangeState(StateSurvival);
                return; 
            }

            // Prioridad 4: AHORRO DE ENERGÍA (Agotamiento)
            bool isExhausted = Stats.Energy.Value <= 10f;
            if (isExhausted)
            {
                if (_currentState is StateWorking)
                {
                    Debug.Log($"[Aethian] {name}: Estoy agotado ({Stats.Energy.Value:F1}%). Dejo de trabajar por hoy.");
                    
                    if (CurrentJob != null)
                    {
                        var jobManager = ServiceLocator.Get<JobManager>();
                        jobManager?.ReturnJob(CurrentJob); 
                        CurrentJob = null;
                    }
                    
                    ChangeState(StateIdle);
                }
                scheduledActivity = ActivityType.Leisure;
            }

            // Prioridad 5: RUTINA (Trabajo vs Ocio)
            if (scheduledActivity == ActivityType.Work)
            {
                // Si ya tengo trabajo y estoy trabajándolo, bien.
                if (CurrentJob != null)
                {
                    // DISCRIMINACIÓN DE ESTADO SEGÚN TIPO DE TRABAJO
                    // Si es Talar o Recolectar -> Usar StateGather (El que tiene la lógica de distancia y recursos)
                    if (CurrentJob.JobType == JobType.Gather || CurrentJob.JobType == JobType.Chop)
                    {
                        if (!(_currentState is StateGather))
                        {
                            ChangeState(StateGather);
                        }
                    }
                    // Si es Construir -> Usar StateBuilding (Futuro)
                    else if (CurrentJob.JobType == JobType.Build)
                    {
                        if (!(_currentState is StateBuilding))
                        {
                            ChangeState(StateBuilding);
                        }
                    }
                    // Para todo lo demás (Mover, Craft, etc.) -> Usar el genérico StateWorking
                    else 
                    {
                        if (!(_currentState is StateWorking))
                        {
                            ChangeState(StateWorking);
                        }
                    }
                }
            }
            // Caso B: Toca OCIO (Leisure)
            else if (scheduledActivity == ActivityType.Leisure)
            {
                // ... (Misma lógica de antes: soltar trabajo y ponerse en Idle) ...
                if (_currentState is StateWorking)
                {
                    if (CurrentJob != null)
                    {
                        var jobManager = ServiceLocator.Get<JobManager>();
                        jobManager?.ReturnJob(CurrentJob);
                        CurrentJob = null;
                    }
                    ChangeState(StateIdle);
                }
            }
        }

        protected virtual bool ShouldIgnoreHunger() => false;

        public void ChangeState(AethianState newState)
        {
            if (_currentState == newState) return; // Evitar re-entrar al mismo estado

            _currentState?.Exit();
            _currentState = newState;

            string stateName = _currentState.GetType().Name.Replace("State", "");
            OnStateChanged?.Invoke(stateName);
            
            _currentState.Enter();
        }
        
        public bool IsAvailable()
        {
            // Solo disponible si estoy en estado Working (o Idle queriendo trabajar)
            // Y NO estoy agotado
            if (Stats.Energy.Value <= 10f) return false;
            
            // Y si es horario laboral (opcional, pero StateWorking ya filtra esto)
            return true; 
        }
        
        private void AssignRandomIdentity()
        {
            if (!string.IsNullOrEmpty(Stats.CharacterName) && Stats.CharacterName != "Aethian") 
                return;

            var nameService = Homebound.Core.ServiceLocator.Get<NameGeneratorService>();
            if (nameService != null)
            {
                Gender rndGender = (UnityEngine.Random.value > 0.5f) ? Gender.Male : Gender.Female;
                Stats.CharacterName = nameService.GetRandomName(Race.Aethian, rndGender);
                
                string stateName = _currentState != null ? _currentState.GetType().Name.Replace("State", "") : "Idle";
                OnStateChanged?.Invoke(stateName);

                Debug.Log($"[AethianBot] Ha nacido/llegado: {Stats.CharacterName}");
            }
        }
    }
}
