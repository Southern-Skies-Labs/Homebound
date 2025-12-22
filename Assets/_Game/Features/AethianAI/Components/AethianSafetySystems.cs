using Homebound.Core;
using Homebound.Features.AethianAI.FailSafe;
using Homebound.Features.Navigation;
using Homebound.Features.Navigation.FailSafe;
using System.Collections;
using UnityEngine;

namespace Homebound.Features.AethianAI.Components
{
    public class AethianSafetySystem : MonoBehaviour
    {
        public bool IsRecovering { get; private set; }

        // Variables privadas
        private FailSafeBuilder _builder;
        private IFailSafeStrategy _failSafeStrategy;
        private PathfindingService _pathfinder;
        private UnitMovementController _movementController;

        private void Awake()
        {
            _movementController = GetComponent<UnitMovementController>();

        }

        private void Start()
        {
            _pathfinder = ServiceLocator.Get<PathfindingService>();
            _builder = ServiceLocator.Get<FailSafeBuilder>();

            _failSafeStrategy = GetComponent<IFailSafeStrategy>() ?? new AethianFailSafeStrategy();
        }


        public void TriggerEmergencyProtocol(Vector3 originalTarget)
        {
            if (IsRecovering) return;

            Debug.LogWarning($"[AethianSafetySystem] Protocolo de emergencia iniciado para {name}");
            IsRecovering = true;

            if (_movementController != null) _movementController.StopMoving();

            StartCoroutine(ExecuteEscapeRoutine(originalTarget));
        }

        private IEnumerator ExecuteEscapeRoutine(Vector3 target)
        {
            try
            {
                // 1. Calcular ruta de escape
                Debug.Log("[SafetySystem] Calculando ruta de escape...");
                var emergencyPath = _pathfinder.FindEmergencyPath(transform.position, target);

                if (emergencyPath != null && emergencyPath.Count > 0)
                {
                    Debug.Log($"[SafetySystem] Solución encontrada: Construir {emergencyPath.Count} bloques.");

                    // 2. Construir
                    if (_builder != null)
                    {
                        yield return StartCoroutine(_builder.BuildEmergencyRouteRoutine(emergencyPath));
                    }
                    else
                    {
                        Debug.LogError("[SafetySystem] Falta FailSafeBuilder!");
                    }

                    yield return new WaitForSeconds(0.2f);

                    // 3. Reanudar movimiento (Delegamos al movimiento, no a la lógica del bot)
                    if (_movementController != null) _movementController.MoveTo(target);
                }
                else
                {
                    // Plan B: Teleport
                    Debug.LogError("[SafetySystem] Ruta imposible. Ejecutando TELEPORT.");
                    Vector3 safePos = _failSafeStrategy.GetSafePosition();
                    transform.position = safePos;
                    yield return null;

                    if (_movementController != null) _movementController.StopMoving();
                }
            }
            finally
            {
                // Importante: Liberar la bandera al terminar
                IsRecovering = false;
                Debug.Log("[SafetySystem] Emergencia finalizada. Sistema estable.");
            }
        }
    }
}