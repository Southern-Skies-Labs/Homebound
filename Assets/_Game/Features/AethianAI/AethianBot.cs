using Homebound.Features.AethianAI.States;
using UnityEngine;
using Homebound.Features.TaskSystem;
using System;
using System.Collections;
using Homebound.Features.TimeSystem;
using Homebound.Core;
using Homebound.Features.Identity;
using Homebound.Features.Navigation;
using Homebound.Features.Navigation.FailSafe;
using Homebound.Features.VoxelWorld;
using Homebound.Features.AethianAI.Strategies;
using System.Collections.Generic;
using Homebound.Features.AethianAI.Components;

namespace Homebound.Features.AethianAI
{
    public class AethianBot : MonoBehaviour, IJobWorker
    {
        //Variables
        [Header("Data")] 
        public AethianStats Stats;

        public event Action<string> OnStateChanged; 
        
        [Header("Debug")]
        [SerializeField] private string _currentStateName;

        [Header("Behavior")] public ScheduleProfile Schedule;
        
        private TimeManager _timeManager;
        private Dictionary<JobType, IJobStrategy> _jobStrategies;
        

        public Vector3 Position => transform.position;

        private UnitMovementController _mover; 
        
        public UnitClass Class => Stats.Class;
        public float _workTimer;

        //Variables FailSafe
        private StuckMonitor _stuckMonitor;
        //private PathfindingService _pathfinder;
        private AethianSafetySystem _safetySystem;

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
            _safetySystem = GetComponent<AethianSafetySystem>();
            if (_safetySystem == null)
            {
                _safetySystem = gameObject.AddComponent<AethianSafetySystem>();
            }

            var metabolism = GetComponent<AethianMetabolism>();
            if (metabolism == null)
            {
                gameObject.AddComponent<AethianMetabolism>();
            }

            var brain = GetComponent<AethianBrain>();
            if (brain == null)
            {
                gameObject.AddComponent<AethianBrain>();
            }

            Stats = GetComponent<AethianStats>();
            if (Stats == null) Stats = gameObject.AddComponent<AethianStats>();

            _stuckMonitor = GetComponent<StuckMonitor>();

            if (_stuckMonitor != null)
                _stuckMonitor.OnStuckDetected += HandleStuckSituation;

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
             // CONEXIÓN AL TIEMPO
            int timeRetries = 0;
            while (_timeManager == null && timeRetries < 10)
            {
                _timeManager = ServiceLocator.Get<TimeManager>();
                if (_timeManager == null) yield return new WaitForSeconds(0.1f);
                timeRetries++;
            }

            AssignRandomIdentity();            
            
            //INICIO DE IA
            ChangeState(StateIdle);
        }
        
        protected virtual void Update()
        {
            if (_safetySystem.IsRecovering) return;

            if (_currentState != null) _currentStateName = _currentState.GetType().Name;

            if (CurrentJob != null)
            {
                if (JobStrategies.TryGetValue(CurrentJob.JobType, out var strategy))
                {
                    strategy.Execute(this, Time.deltaTime);
                }
            }

            if (CurrentJob != null && CurrentJob.IsCancelled)
            {
                StopMoving();
                CurrentJob = null;
                ChangeState(StateIdle);
                return;
            }

            _currentState?.Tick();
        }

        public bool IsState<T>() where T : AethianState
        {
            return _currentState is T;
        }

        private void HandleStuckSituation()
        {
            Vector3 targetParams = (CurrentJob != null) ? CurrentJob.Position : transform.position;

            if (Vector3.Distance(transform.position, targetParams) < 1f)
            {

            }

            _safetySystem.TriggerEmergencyProtocol(targetParams);
        }



        public void MoveTo(Vector3 position)
        {
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
        public void CompleteCurrentJob() 
        {
            if (CurrentJob != null)
            {
                var jobManager = ServiceLocator.Get<JobManager>();

                // jobManager.CompleteJob(CurrentJob); 
                
                Debug.Log($"[AethianBot] Trabajo '{CurrentJob.JobName}' completado.");
                CurrentJob = null;
            }

            _workTimer = 0f;
            ChangeState(StateIdle); // Volver a Idle para buscar nueva tarea o descansar
        }
        
        private void ReturnCurrentJob()
        {
            if (CurrentJob != null)
            {
                var jobManager = ServiceLocator.Get<JobManager>();
                jobManager?.ReturnJob(CurrentJob); 
                CurrentJob = null;
                StopMoving(); // Freno de seguridad
            }
        }

        private Dictionary<JobType, IJobStrategy> JobStrategies
        {
            get
            {
                if (_jobStrategies == null)
                {
                    _jobStrategies = new Dictionary<JobType, IJobStrategy>
                    {
                        // Aca registro las estrategias de trabajo disponibles
                        { JobType.Mine, new MiningJobStrategy() }
                        // Futuro: { JobType.Build, new BuildingJobStrategy() }
                    };
                    Debug.Log($"[AethianBot] Estrategias de trabajo inicializadas (Lazy Init) para {name}");
                }
                return _jobStrategies;
            }
        }

    }
}
