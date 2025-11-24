using System;
using UnityEngine;
using Homebound.Core;

    
namespace Homebound.Features.TimeSystem
{
    public class DayNightController : MonoBehaviour
    {
        
        //Variables
        [Header("Referencias")] 
        [SerializeField] private Light _directionalLight;
        [SerializeField] private TimeManager _timeManager;

        [Header("Configuración visual")] 
        [SerializeField] private Color _dayAmbient = Color.white;
        [SerializeField] private Color _nightAmbient = new Color(0.1f, 0.1f, 0.2f);

        [SerializeField] private AnimationCurve _lightIntensityCurve;
        
        //Metodos
        private void Start()
        {
            if (_timeManager == null)
            {
                _timeManager = ServiceLocator.Get<TimeManager>();
            }

            if (_lightIntensityCurve.length == 0)
            {
                _lightIntensityCurve = new AnimationCurve(
                    new Keyframe(0,0),
                    new Keyframe(6,0.2f),
                    new Keyframe(12,1),
                    new Keyframe(18, 0.2f),
                    new Keyframe(24, 0)
                    );
            }
        }

        private void Update()
        {
            if (_timeManager == null) return;

            UpdateSunPosition(_timeManager.CurrentHour);
            UpdateAmbientLight(_timeManager.CurrentHour);
        }

        private void UpdateSunPosition(float time)
        {
            float alpha = time / 24.0f;
            float sunRotation = Mathf.Lerp(-90, 270, alpha);
            
            _directionalLight.transform.rotation = Quaternion.Euler(sunRotation, -30f, 0);
        }

        private void UpdateAmbientLight(float time)
        {
            float intensity = _lightIntensityCurve.Evaluate(time);

            _directionalLight.intensity = intensity;
            
            Color targetColor = intensity > 0.1f ? _dayAmbient : _nightAmbient;
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetColor, Time.deltaTime * 2f);

        }
    
    }
    
}
