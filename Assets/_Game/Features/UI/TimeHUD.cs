using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Homebound.Core;
using Homebound.Features.TimeSystem;

namespace Homebound.Features.UI
{
    public class TimeHUD : MonoBehaviour
    {
        //Variables
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _dayText;
        [SerializeField] private Image _dayNightBar;
        
        private TimeManager _timeManager;
        
        //Metodos

        private void Start()
        {
            _timeManager = ServiceLocator.Get<TimeManager>();
            if (_timeManager != null)
            {
                _timeManager.OnHourChanged += UpdateTimeText;
                _timeManager.OnDayChanged += UpdateDayText;
                
                UpdateDayText(_timeManager.CurrentDay);
                UpdateTimeText((int)_timeManager.CurrentHour);
            }
        }

        private void OnDestroy()
        {
            if (_timeManager!= null)
            {
                _timeManager.OnHourChanged -= UpdateTimeText;
                _timeManager.OnDayChanged -= UpdateDayText;
            }
        }

        private void Update()
        {
            if (_timeManager != null && _dayNightBar != null)
            {
                _dayNightBar.fillAmount = _timeManager.CurrentHour / 24f;
            }
        }

        private void UpdateTimeText(int hour)
        {
            if (_timeText != null) _timeText.text = $"{hour:00}:00";
        }

        private void UpdateDayText(int day)
        {
            if (_dayText != null) _dayText.text = $"Día {day}";
        }

        public void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            Debug.Log($"[TimeHUD] Velocidad: x{scale}");
        }
        
        
        
    }
}
