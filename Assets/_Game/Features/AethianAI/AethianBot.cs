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
        public float _workTimer;
        
        //Variables FailSafe
        private StuckMonitor _stuckMonitor;
        private IFailSafeStrategy _failSafeStrategy;
        private FailSafeBuilder _builder;
        private PathfindingService _pathfinder;
        
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
            _stuckMonitor = GetComponent<StuckMonitor>();
            _failSafeStrategy = GetComponent<IFailSafeStrategy>();

            if (_stuckMonitor != null)
                _stuckMonitor.OnStuckDetected += HandleStuckSituation;

            _mover = GetComponent<UnitMovementController>();
            _builder = ServiceLocator.Get<FailSafeBuilder>();
            
            if (_builder == null) _builder = FindFirstObjectByType<FailSafeBuilder>();
            

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
            // // FASE 1: ATERRIZAJE SEGURO 
            // bool sueloEncontrado = false;
            // int intentos = 0;
            //
            // while (!sueloEncontrado && intentos < 20)
            // {
            //     // Lanzamos un rayo desde un poco arriba de la unidad hacia abajo
            //     Ray ray = new Ray(transform.position + Vector3.up * 2f, Vector3.down);
            //     RaycastHit hit;
            //
            //     // Buscamos colisión con cualquier cosa sólida (VoxelWorld)
            //     if (Physics.Raycast(ray, out hit, 1000f))
            //     {
            //         // "Teletransportamos" la unidad justo al punto de impacto
            //         transform.position = hit.point;
            //         sueloEncontrado = true;
            //         Debug.Log($"[AethianBot] Aterrizaje físico exitoso en {hit.point}.");
            //     }
            //     else
            //     {
            //         yield return new WaitForSeconds(0.1f);
            //         intentos++;
            //     }
            // }
            //
            // if (!sueloEncontrado)
            // {
            //     Debug.LogError($"[AethianBot] CRÍTICO: La unidad {name} está flotando en el vacío (No hay suelo debajo).");
            //     
            // }

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

            // 1. Metabolismo (Sin cambios)
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
              
            // --- LÓGICA DE MINERÍA CORREGIDA ---
            if (CurrentJob != null && CurrentJob.JobType == JobType.Mine)
            {
                // Usamos una distancia un poco más holgada para evitar que choque con el collider del bloque
                if (Vector3.Distance(transform.position, CurrentJob.Position) < 2.0f) 
                {
                    // [CRÍTICO] ¡Frenar los motores!
                    // Si no hacemos esto, el StuckMonitor pensará que estamos atascados contra la piedra.
                    StopMoving(); 

                    _workTimer += Time.deltaTime;
                    
                    // Feedback visual opcional: Rotar hacia la piedra
                    Vector3 dir = (CurrentJob.Position - transform.position).normalized;
                    dir.y = 0;
                    if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

                    if (_workTimer >= 2.0f) 
                    {
                        // Efectuar Minería
                        Vector3 checkPos = CurrentJob.Position + new Vector3(0.5f, 0.5f, 0.5f);
                        Collider[] hits = Physics.OverlapSphere(checkPos, 0.8f, LayerMask.GetMask("Terrain"));               
                        
                        if (hits != null && hits.Length > 0)
                        {
                            // Priorizamos el objeto que tenga el componente Chunk
                            Chunk targetChunk = null;
                            foreach (var hit in hits)
                            {
                                targetChunk = hit.GetComponent<Chunk>();
                                if (targetChunk != null) break;
                            }

                            if (targetChunk != null)
                            {
                                Debug.Log($"[AethianBot] Rompiendo bloque en {CurrentJob.Position} del Chunk {targetChunk.name}");
                                targetChunk.DestroyBlockAtWorldPos(CurrentJob.Position);
                                // UnitInventory.Add(StoneData, 1); 
                            }
                            else
                            {
                                Debug.LogError($"[AethianBot] ERROR: Detecté colisión en {CurrentJob.Position} pero el objeto no tiene script 'Chunk'. Objeto: {hits[0].name}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"[AethianBot] ERROR: No encontré ningún 'Terrain' en la posición {CurrentJob.Position}. ¿Radio muy pequeño o Layer incorrecta?");
                        }
            
                        CompleteJob();
                    }
                }
                else
                {
                    // [OPTIMIZACIÓN] Solo ordenamos mover si NO nos estamos moviendo ya
                    // Esto evita recalcular el pathfinding 60 veces por segundo.
                    // Asumimos que _mover es tu UnitMovementController.
                    if (!HasReachedDestination()) 
                    {
                         // Si ya estamos caminando hacia allá, no spameamos la orden.
                         // Nota: Si el UnitMovementController no expone 'TargetPosition', simplemente llamamos MoveTo
                         // pero idealmente deberíamos chequear si ya tenemos ese destino.
                         
                         // Para tu código actual, una forma simple de evitar spam es chequear IsMoving:
                         if (!_mover.IsMoving)
                         {
                             MoveTo(CurrentJob.Position);
                         }
                    }
                }
            }
            // ---------------------------------
            
            if (CurrentJob != null && CurrentJob.IsCancelled)
            {
                Debug.Log($"[Aethian] {name}: Mi tarea '{CurrentJob.JobName}' fue cancelada. Parando.");
                StopMoving(); 
                CurrentJob = null; 
                ChangeState(StateIdle); 
                return; 
            }
            
            CheckGlobalTransitions();
            _currentState?.Tick();
        }
        
        private void HandleStuckSituation()
        {
            Debug.LogWarning($"[AethianBot] {name} ATASCADO. Iniciando protocolo Fail-Safe...");
            
            StopMoving(); // Detener intento actual

            // 1. ¿A dónde queríamos ir?
            // Si tenemos un Job, el destino es el Job. Si no, quizás el Banner.
            Vector3 targetParams = (CurrentJob != null) ? CurrentJob.Position : transform.position; 
            // Nota: Si estamos Idle y atascados, targetParams = self es inútil. 
            // Asumiremos que si está atascado es porque quería ir a algún lado.
            
            // Si no hay destino claro, usamos el SafePosition de la estrategia
            if (Vector3.Distance(transform.position, targetParams) < 1f)
            {
                targetParams = _failSafeStrategy.GetSafePosition();
            }

            StartCoroutine(ExecuteEscapeRoutine(targetParams));
        }
        
        private IEnumerator ExecuteEscapeRoutine(Vector3 target)
        {
            if (_pathfinder == null) _pathfinder = ServiceLocator.Get<PathfindingService>();

            // 2. Calcular Ruta de Emergencia (Simulación)
            Debug.Log("[AethianBot] Calculando ruta de emergencia...");
            var emergencyPath = _pathfinder.FindEmergencyPath(transform.position, target);

            if (emergencyPath != null && emergencyPath.Count > 0)
            {
                Debug.Log($"[AethianBot] Solución encontrada: Construir puente de {emergencyPath.Count} bloques.");
                
                // 3. CONSTRUIR (Esperamos a que termine)
                yield return StartCoroutine(_builder.BuildEmergencyRouteRoutine(emergencyPath));

                // 4. MOVERSE (Ahora el camino es válido para el pathfinding normal)
                MoveTo(target);
            }
            else
            {
                // 5. ULTIMO RECURSO: TELEPORT
                Debug.LogError("[AethianBot] ¡Imposible construir ruta! Ejecutando TELEPORT de emergencia.");
                Vector3 safePos = _failSafeStrategy.GetSafePosition();
                
                // Efecto visual (Opcional)
                // Instantiate(TeleportVFX, transform.position...);
                
                transform.position = safePos;
                yield return null; // Esperar un frame físico
                
                // Resetear cerebro
                CurrentJob = null;
                ChangeState(StateIdle);
            }
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
                // CASO 1: Estoy trabajando -> Parar inmediatamente (Tu código actual)
                if (_currentState is StateWorking)
                {
                    Debug.Log($"[Aethian] {name}: Hora de descanso. Dejando trabajo.");
                    ReturnCurrentJob(); // Helper para no repetir código
                    ChangeState(StateIdle);
                }
        
                // CASO 2 (NUEVO): Estoy en IDLE pero tengo un trabajo "pegado" (ej: al despertar)
                // Si no lo soltamos, el bot intentará ir a trabajar, creando el bucle.
                else if (CurrentJob != null)
                {
                    Debug.Log($"[Aethian] {name}: Tengo tarea pendiente pero es hora de Ocio. La devuelvo.");
                    ReturnCurrentJob();
                    // Ya estamos en Idle o similar, no hace falta cambiar estado, pero aseguramos limpieza.
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
        private void CompleteJob() 
        {
            if (CurrentJob != null)
            {
                var jobManager = ServiceLocator.Get<JobManager>();
                // Asumimos que JobManager tiene un método para cerrar el trabajo.
                // Si JobManager solo crea y no rastrea finalización, basta con esto:
                
                // Opción A: Notificar al Manager (Recomendado si JobManager lo soporta)
                // jobManager.CompleteJob(CurrentJob); 
                
                // Opción B (Directa): Limpiar referencia local
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
        
    }
}
