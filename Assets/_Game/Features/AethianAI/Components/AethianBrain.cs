using Homebound.Core;
using Homebound.Features.AethianAI.States;
using Homebound.Features.TimeSystem;
using UnityEngine;

namespace Homebound.Features.AethianAI.Components
{
    [RequireComponent(typeof(AethianBot))]
    [RequireComponent(typeof(AethianStats))]
    public class AethianBrain : MonoBehaviour
    {
        private AethianBot _bot;
        private AethianStats _stats;
        private TimeManager _timeManager;

        // Configuración de umbrales 
        [Header("Decision Thresholds")]
        [SerializeField] private float _hungerPanicLevel = 0f; 
        //[SerializeField] private float _energyCriticalLevel = 5f;

        private void Awake()
        {
            _bot = GetComponent<AethianBot>();
            _stats = GetComponent<AethianStats>();
        }

        private void Start()
        {
            _timeManager = ServiceLocator.Get<TimeManager>();
        }

        private void Update()
        {
            Think();
        }

        private void Think()
        {
            if (EvaluateSurvivalInstincts()) return;

            if (_timeManager != null)
            {
                EvaluateSchedule(_timeManager.CurrentTime.Hour);
            }
        }

        private bool EvaluateSurvivalInstincts()
        {
            if (_stats.Hunger.Value <= _hungerPanicLevel)
            {
                if (!_bot.IsState<StateSurvival>())
                {
                    Debug.Log("[Brain] ¡Hambre crítica! Activando instinto de supervivencia.");
                    _bot.ChangeState(_bot.StateSurvival);
                }
                return true; 
            }
            return false;
        }

        private void EvaluateSchedule(float currentHour)
        {
            bool isNight = currentHour >= 22 || currentHour < 6;

            if (isNight)
            {
                if (!_bot.IsState<StateSleep>())
                {
                    _bot.ChangeState(_bot.StateSleep);
                }
            }
            else
            {
                // Es de día. ¿Tenemos trabajo?
                if (_bot.CurrentJob != null)
                {
                    // Si tengo trabajo y estoy Idle o Wandering, ponte a trabajar
                    if (_bot.IsState<StateIdle>())
                    {

                    }
                }
                else
                {
                    if (!_bot.IsState<StateIdle>() && !_bot.IsState<StateWorking>())
                    {
                        _bot.ChangeState(_bot.StateIdle);
                    }
                }
            }
        }
    }
}