using UnityEngine;
using System;
using Homebound.Core;

namespace Homebound.Features.TimeSystem
{
    public class TimeManager : MonoBehaviour, ITickable
    {
        //Variables
        [Header("Configuración")] 
        [Tooltip("Cuantos segundos reales dura una hora en el juego")]
        public float RealSecondsPerGameHour = 10f;

        [Header("Estado actual (Read Only")] 
        public int CurrentDay = 1;
        public float CurrentHour = 6f;
        public Season CurrentSeason = Season.Spring;
        
        //Eventos globales
        public event Action<int> OnHourChanged;
        public event Action<int> OnDayChanged;
        public event Action<Season> OnSeasonChanged;

        private int _lastHourInt;
        private const float HOURS_IN_DAY = 24f;
        private const int DAYS_IN_SEASON = 20;
        
        
        //Metodos
        private void Awake()
        {
            ServiceLocator.Register<TimeManager>(this);
            _lastHourInt = (int)CurrentHour;
           
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterTickable(this);
            }
            else
            {
                Debug.Log("[TimeManager] GameManager no encontrado");
            }
            
        }


        private void OnDestroy()
        {
            ServiceLocator.Unregister<TimeManager>();
            
            if(GameManager.Instance != null)
                GameManager.Instance.UnregisterTickable(this);
        }


        public void Tick(float deltaTime)
        {
            AdvanceTime(deltaTime);
        }

        private void AdvanceTime(float deltaTime)
        {
            float hourIncrease = deltaTime / RealSecondsPerGameHour;
            CurrentHour += hourIncrease;

            if ((int)CurrentHour > _lastHourInt)
            {
                _lastHourInt = (int)CurrentHour;
                OnHourChanged?.Invoke(_lastHourInt);
            }

            if (CurrentHour >= HOURS_IN_DAY)
            {
                CurrentHour = 0;
                _lastHourInt = 0;
                AdvanceDay();
            }
        }
        
        private void AdvanceDay()
        {
            CurrentDay++;
            OnDayChanged?.Invoke(CurrentDay);

            if (CurrentDay > DAYS_IN_SEASON)
            {
                
            }
            
            Debug.Log($"[TimeManager] Día {CurrentDay} ha comenzado");
        }
        
    }

}