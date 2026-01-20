using UnityEngine;
using System;

namespace Homebound.Features.Navigation.FailSafe
{
    [RequireComponent(typeof(UnitMovementController))]
    public class StuckMonitor : MonoBehaviour
    {
        [Header("Sensitivity Settings")]
        [Tooltip("Intervalo en segundos para verificar el progreso del movimiento")]
        [SerializeField] private float _checkInterval = 2.0f;

        [Tooltip("Distancia minima que debe haberse movido en el invervalo para considerarse válido")] 
        [SerializeField] private float _minProgressThreshold = 0.5f;

        [Tooltip("Numero de fallos consecutivos antes de declarar que la unidad está atascada")] 
        [SerializeField] private int _maxStuckStrikes = 3;

        public event Action OnStuckDetected;

        private UnitMovementController _mover;
        private Vector3 _lastCheckPosition;
        private float _timer;
        private int _currentStrikes;
        
        private void Awake()
        {
            _mover = GetComponent<UnitMovementController>();
        }

        private void Start()
        {
            _lastCheckPosition = transform.position;
        }

        private void Update()
        {
            // Si no se mueve, reseteamos el monitor de movimiento físico...
            if (!_mover.IsMoving)
            {
                ResetMonitor();
                return;
            }

            _timer += Time.deltaTime;

            if (_timer >= _checkInterval)
            {
                CheckProgress();
                _timer = 0f;
            }
        }

        
        public void ReportPathfindingFailure()
        {
            Debug.LogWarning($"[StuckMonitor] {name}: Reporte de fallo crítico de ruta (Sin camino posible). Forzando estado de ATASCO.");
            
            TriggerStuckEvent();
        }
        

        private void CheckProgress()
        {
            float distanceTraveled = Vector3.Distance(transform.position, _lastCheckPosition);

            if (distanceTraveled < _minProgressThreshold)
            {
                _currentStrikes++;
                Debug.LogWarning($"[StuckMonitor] {name} parece atascado físicamente. Strike {_currentStrikes}/{_maxStuckStrikes}.");
                
                if (_currentStrikes >= _maxStuckStrikes)
                {
                    TriggerStuckEvent();
                }
            }
            else
            {
                _currentStrikes = 0;
            }

            _lastCheckPosition = transform.position;
        }

        private void TriggerStuckEvent()
        {
            Debug.LogError($"[StuckMonitor] ¡ALERTA! {name} está totalmente atascado. Solicitando intervención.");
            OnStuckDetected?.Invoke();
            ResetMonitor();
        }

        private void ResetMonitor()
        {
            _currentStrikes = 0;
            _timer = 0f;
            _lastCheckPosition = transform.position;
        }
    }
}