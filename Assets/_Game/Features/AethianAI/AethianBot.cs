using Homebound.Features.AethianAI.States;
using UnityEngine;
using UnityEngine.AI;
using Homebound.Features.TaskSystem;
using System;

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
        
        //Componentes
        public NavMeshAgent Agent { get; private set; }
        
        //Estado actual
        private AethianState _currentState;
        public JobRequest CurrentJob { get; set; } //Tarea actual
        
        //Definicion de los estados posibles, para que no se creen en cada frame
        public AethianState StateIdle { get; private set; }
        public AethianState StateWorking { get; private set; }
        public AethianState StateSurvival { get; private set; }
        
        //Metodos
        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            if (Agent.enabled) Agent.enabled = false;

            StateIdle = new StateIdle(this);
            StateWorking = new StateWorking(this);
            StateSurvival = new StateSurvival(this);
        }
        
        protected virtual System.Collections.IEnumerator Start()
        {
            // Desactivamos por seguridad al inicio
            if (Agent.enabled) Agent.enabled = false;

            // BUCLE DE SEGURIDAD:
            // Intentamos encontrar el NavMesh hasta 20 veces (2 segundos máx)
            int intentos = 0;
            while (intentos < 20)
            {
  
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {

                    // 1. Ajustamos la posición exacta al suelo válido
                    transform.position = hit.position;
                    
                    // 2. Ahora es seguro encender el agente
                    Agent.enabled = true;
                    
                    Debug.Log($"[AethianBot] Aterrizaje exitoso tras {intentos} intentos. Iniciando IA.");
                    
                    // 3. Arrancamos la máquina de estados
                    ChangeState(StateIdle);
                    yield break; 
                }

                // Si no lo encontramos, esperamos un poco y probamos de nuevo
                yield return new WaitForSeconds(0.1f);
                intentos++;
            }

            Debug.LogError($"[AethianBot] CRÍTICO: No se encontró NavMesh debajo de la unidad en {transform.position}. ¿Está el NavMeshSurface bakeado?");
        }
        
        protected virtual void Update()
        {
            //Simulacion de necesidades
            Stats.DecayHunger(2f * Time.deltaTime);  //Baja 2 puntos por segundo
            
            //Check global de supervivencia
            CheckGlobalTransitions();
            
            //Ejecutamos la logica del estado actual
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
   
        private void CheckGlobalTransitions()
        {
            //Hook de combate: Si "ShouldIgnoreHunger" es true, no forzamos el survival
            if (!ShouldIgnoreHunger() && Stats.Hunger < AethianStats.HUNGER_CRITICAL_THRESHOLD)
            {
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
    }
}

