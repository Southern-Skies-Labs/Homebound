using UnityEngine;
using Homebound.Core;
using Homebound.Features.TimeSystem;

namespace Homebound.Features.AethianAI.Components
{
    [RequireComponent(typeof(AethianStats))]
    public class AethianMetabolism : MonoBehaviour
    {
        private AethianStats _stats;
        private TimeManager _timeManager;
        private float _lastHourCheck;

        private void Awake()
        {
            _stats = GetComponent<AethianStats>();
        }

        private void Start()
        {
            _timeManager = ServiceLocator.Get<TimeManager>();

            if (_timeManager != null)
            {
                _lastHourCheck = _timeManager.CurrentTime.Hour;
            }
        }

        private void Update()
        {
            if (_timeManager == null) return;

            float currentHour = _timeManager.CurrentTime.Hour;

            if (Mathf.Abs(currentHour - _lastHourCheck) < 0.01f) return;

            float deltaHours = currentHour - _lastHourCheck;

            if (deltaHours < 0) deltaHours += 24f;

            if (deltaHours > 0)
            {
                _stats.UpdateNeeds(deltaHours);
                _lastHourCheck = currentHour;
            }
        }
    }
}