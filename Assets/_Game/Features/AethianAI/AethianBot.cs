using Homebound.Features.AethianAI.States;
using UnityEngine;
using UnityEngine.AI;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.AethianAI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AethianBot : MonoBehaviour
    {
        //Variables
        [Header("Data")] 
        public AethianStats Stats = new AethianStats();

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
            
            //Inicializamos los estados
            StateIdle = new StateIdle(this);
            StateWorking = new StateWorking(this);
            StateSurvival = new StateSurvival(this);
        }
        
        protected virtual void Start()
        {
            ChangeState(StateIdle);
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
        
        private void CheckGlobalTransitions()
        {
            //Hook de combate: Si "ShouldIgnoreHunger" es true, no forzamos el survival
            if (!ShouldIgnoreHunger() && Stats.Hunger < AethianStats.HUNGER_CRITICAL_THRESHOLD)
            {
                if (_currentState != StateSurvival)
                {
                    Debug.LogWarning($"[AethianBot] {name} tiene mucha hambre! Forzando modo supervivencia.");
                    ChangeState(StateSurvival);
                }
            }
        }
        
        
        //Metodo virtual para que clases hijas (clases de combate) puedan sobreescribirlo
        protected virtual bool ShouldIgnoreHunger()
        {
            return false; //Por defecto, no ignoramos la hambre
        }

        public void ChangeState(AethianState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentStateName = _currentState.GetType().Name;
            _currentState.Enter();
        }
    }
}

