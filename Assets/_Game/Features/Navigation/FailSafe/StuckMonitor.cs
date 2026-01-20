using UnityEngine;
using System;

namespace Homebound.Features.Navigation.FailSafe
{
    /// <summary>
    /// Componente pasivo que rastrea el progreso de movimiento.
    /// Es controlado por UnitMovementController para permitir una "Respuesta Graduada" (Tiered Response).
    /// </summary>
    [RequireComponent(typeof(UnitMovementController))]
    public class StuckMonitor : MonoBehaviour
    {
        [Header("Sensitivity Settings")]
        [Tooltip("Tiempo sin moverse antes de considerar un bloqueo leve")]
        [SerializeField] private float _softStuckTime = 1.0f;

        [Tooltip("Tiempo sin moverse antes de considerar un bloqueo moderado (repath)")]
        [SerializeField] private float _mediumStuckTime = 3.0f;

        [Tooltip("Tiempo sin moverse antes de declarar bloqueo crítico (hard stuck)")]
        [SerializeField] private float _hardStuckTime = 5.0f;

        [Tooltip("Distancia mínima que debe moverse para resetear el timer")]
        [SerializeField] private float _movementThreshold = 0.1f;

        public event Action OnSoftStuck;   // Tier 1: Nudge
        public event Action OnMediumStuck; // Tier 2: Repath
        public event Action OnHardStuck;   // Tier 3: FailSafe Strategy

        private Vector3 _lastPosition;
        private float _stuckTimer;
        private bool _isMonitoring;

        private void Start()
        {
            _lastPosition = transform.position;
        }

        /// <summary>
        /// Llamado cada frame por UnitMovementController cuando intenta moverse.
        /// </summary>
        public void CheckStuck(float deltaTime)
        {
            if (Vector3.Distance(transform.position, _lastPosition) > _movementThreshold)
            {
                // Se movió, reseteamos
                _stuckTimer = 0f;
                _lastPosition = transform.position;
                return;
            }

            _stuckTimer += deltaTime;

            // --- EVALUACIÓN DE NIVELES ---

            // Tier 1: Nudge
            if (_stuckTimer >= _softStuckTime && _stuckTimer < _mediumStuckTime)
            {
                // Disparamos evento solo una vez por ciclo (opcional, o continuo)
                // Aquí lo haremos continuo para intentar 'empujar'
                OnSoftStuck?.Invoke();
            }
            // Tier 2: Repath
            else if (_stuckTimer >= _mediumStuckTime && _stuckTimer < _hardStuckTime)
            {
                 // Solo invocamos una vez al cruzar el umbral para no spammear pathfinding
                if (Mathf.Abs(_stuckTimer - _mediumStuckTime) < deltaTime * 2)
                {
                     OnMediumStuck?.Invoke();
                }
            }
            // Tier 3: Hard Stuck
            else if (_stuckTimer >= _hardStuckTime)
            {
                Debug.LogError($"[StuckMonitor] {name} HARD STUCK detected (> {_hardStuckTime}s). Escalating.");
                OnHardStuck?.Invoke();
                ResetMonitor(); // Reiniciamos para dar chance al FailSafe de actuar
            }
        }

        public void ReportPathfindingFailure()
        {
            Debug.LogWarning($"[StuckMonitor] {name}: Fallo inmediato de ruta. Escalando a HardStuck.");
            OnHardStuck?.Invoke();
        }

        public void ResetMonitor()
        {
            _stuckTimer = 0f;
            _lastPosition = transform.position;
        }
    }
}
