using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.TimeSystem
{
    public class DayNightController : MonoBehaviour
    {
        [SerializeField] private Light _directionalLight;
        [SerializeField] private Gradient _ambientColor;
        [SerializeField] private Gradient _directionalColor;
        [SerializeField] private Gradient _fogColor;

        private TimeManager _timeManager;

        private void Start()
        {
            _timeManager = ServiceLocator.Get<TimeManager>();
        }

        private void Update()
        {
            if (_timeManager == null) return;
            
            float timePercent = _timeManager.NormalizedDayTime;

            UpdateLighting(timePercent);
        }

        private void UpdateLighting(float timePercent)
        {
            if (_directionalLight != null)
            {
                // Rotar sol: 0 (Medianoche) -> -90, 0.5 (Mediodía) -> 90
                float sunAngle = (timePercent * 360f) - 90f;
                _directionalLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0);
                _directionalLight.color = _directionalColor.Evaluate(timePercent);
            }

            RenderSettings.ambientLight = _ambientColor.Evaluate(timePercent);
            RenderSettings.fogColor = _fogColor.Evaluate(timePercent);
        }
    }
}