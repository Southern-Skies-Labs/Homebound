using UnityEngine;

namespace Homebound.Testing
{
    /// <summary>
    /// HERRAMIENTA DE PRUEBA: Simula un atasco físico.
    /// Mantén presionada la tecla ESPACIO para "pegar" al bot al suelo.
    /// </summary>
    public class DebugGlueTrap : MonoBehaviour
    {
        private Vector3 _lockedPosition;
        private bool _isTrapped = false;

        private void Update()
        {
            // Al presionar ESPACIO, activamos la trampa
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _lockedPosition = transform.position;
                _isTrapped = true;
                Debug.Log($"[DebugTrap] 🛑 TRAMPA ACTIVADA: {name} está pegado al suelo.");
            }

            // Al soltar, lo liberamos
            if (Input.GetKeyUp(KeyCode.Space))
            {
                _isTrapped = false;
                Debug.Log($"[DebugTrap] 🟢 TRAMPA DESACTIVADA: {name} es libre.");
            }
        }

        // Usamos LateUpdate para anular el movimiento del UnitMovementController
        // que ocurre en Update/Corrutinas
        private void LateUpdate()
        {
            if (_isTrapped)
            {
                transform.position = _lockedPosition;
            }
        }
    }
}