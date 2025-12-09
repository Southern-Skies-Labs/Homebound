using UnityEngine;
using System;
using Homebound.Core;
using Homebound.Features.TimeSystem;


namespace Homebound.Features.TimeSystem
{
    public class TimeManager : MonoBehaviour, ITickable
    {
        //Variables
        [Header("Configuration")]
        [SerializeField] private TimeSettings _settings;

        [Header("Debug Status")]
        [SerializeField] private float _timeAccumulator;
        [SerializeField] private GameDateTime _currentTime;
        [SerializeField] private bool _isPaused;

        // Eventos Públicos (Observer Pattern)
        public event Action<GameDateTime> OnMinuteChanged;
        public event Action<GameDateTime> OnHourChanged;
        public event Action<GameDateTime> OnDayChanged;
        public event Action<Season> OnSeasonChanged;

        // Propiedades Públicas
        public GameDateTime CurrentTime => _currentTime;
        public bool IsNight => _currentTime.Hour >= _settings.SunsetHour || _currentTime.Hour < _settings.SunriseHour;
        
        public float NormalizedDayTime
        {
            get
            {
                float totalMinutesInDay = 1440f; // 24 * 60
                float currentMinutes = (_currentTime.Hour * 60) + _currentTime.Minute;
                return currentMinutes / totalMinutesInDay;
            }
        }
        
        //Metodos
        private void Awake()
        {
            ServiceLocator.Register<TimeManager>(this);
            InitializeTime();
        }
        
        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterTickable(this);
            }
            else
            {
                Debug.LogError("[TimeManager] CRITICAL: GameManager instance not found. Time will not advance.");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TimeManager>();
            
            if(GameManager.Instance != null)
                GameManager.Instance.UnregisterTickable(this);
        }

        public void InitializeTime()
        {
            if (_settings == null)
            {
                Debug.LogError("[TimeManager] CRITICAL: TimeSettings not assigned!");
                return;
            }

            _currentTime = new GameDateTime(
                _settings.StartYear,
                _settings.StartSeason,
                _settings.StartDay,
                _settings.StartHour,
                0
                );
        }


        public void Tick(float deltaTime)
        {
            if (_isPaused || _settings == null) return;
            
            _timeAccumulator += deltaTime;
            
            float realSecondsPerGameMinute = _settings.RealSecondsPerGameHour / 60f;
            
            while (_timeAccumulator >= realSecondsPerGameMinute)
            {
                AdvanceMinute();
                _timeAccumulator -= realSecondsPerGameMinute;
            }
        }
        
        private void AdvanceMinute()
        {
            int newMinute = _currentTime.Minute + 1;
            int newHour = _currentTime.Hour;
            int newDay = _currentTime.Day;
            Season newSeason = _currentTime.Season;
            int newYear = _currentTime.Year;

            bool hourChanged = false;
            bool dayChanged = false;
            bool seasonChanged = false;

            if (newMinute >= 60)
            {
                newMinute = 0;
                newHour++;
                hourChanged = true;
                
                if (newHour >= 24)
                {
                    newHour = 0;
                    newDay++;
                    dayChanged = true;

                    if (newDay > _settings.DaysPerSeason)
                    {
                        newDay = 1;
                        newSeason++;
                        seasonChanged = true;

                        if ((int)newSeason > 3)
                        {
                            newSeason = Season.Spring;
                            newYear++;
                        }
                    }

                }
            }

            _currentTime = new GameDateTime(newYear, newSeason, newDay, newHour, newMinute);
            
            OnMinuteChanged?.Invoke(_currentTime);
            
            if (hourChanged)
                OnHourChanged?.Invoke(_currentTime);

            if (dayChanged)
                OnDayChanged?.Invoke(_currentTime);

            if (seasonChanged)
                OnSeasonChanged?.Invoke(newSeason);
        }

        public void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            _isPaused = scale == 0;
        }
    
        //**Experimental**
        public void SkipTime(int hoursToSkip)
        {
            long minutesToSkip = hoursToSkip * 60;

            var timeAwareObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var obj in timeAwareObjects)
            {
                if (obj is ITimeAware timeAware)
                {
                    timeAware.OnTimeSkipped(minutesToSkip);
                }
            }

            for (int i = 0; i < minutesToSkip; i++)
            {
                AdvanceMinute();
            }
            Debug.Log($"[TimeManager] Skipped {hoursToSkip} hours.");
        }
        
    }
}