using UnityEngine;
using TMPro; 
using Homebound.Core;
using Homebound.Features.TimeSystem;

namespace Homebound.Features.UI
{
    public class TimeHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _dateText;
        
        private TimeManager _timeManager;

        private void Start()
        {
            _timeManager = ServiceLocator.Get<TimeManager>();
            
            if (_timeManager != null)
            {
                _timeManager.OnMinuteChanged += UpdateUI;
                
                UpdateUI(_timeManager.CurrentTime);
            }
        }

        private void OnDestroy()
        {
            if (_timeManager != null)
            {
                _timeManager.OnMinuteChanged -= UpdateUI;
            }
        }

        private void UpdateUI(GameDateTime time)
        {
            // Formato reloj digital: "08:05"
            _timeText.text = $"{time.Hour:00}:{time.Minute:00}";
            
            _dateText.text = $"{time.Season}, Day {time.Day} (Y{time.Year})";
        }
        
        public void SetSpeed(float speed)
        {
            if (_timeManager != null)
            {
                _timeManager.SetTimeScale(speed);
            }
        }
    }
}