using System;
using UnityEngine;
using UnityEngine.UI;
using Homebound.Core;
using Homebound.Features.PlayerInteraction;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.UI
{
    public class CommandHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _btnMove; // Deprecated/Debug
        [SerializeField] private Button _btnChop; // Deprecated/Debug
        [SerializeField] private Button _btnMine; // Nuevo botón de minería

        private InteractionController _interactionController;

        private void Start()
        {
            _interactionController = FindFirstObjectByType<InteractionController>();

            if (_interactionController == null)
            {
                Debug.LogError("[CommandHUD] No se encontró InteractionController.");
                return;
            }

            // Mapeos temporales para mantener funcionalidad existente si es necesaria
            // Idealmente deberíamos migrar Move y Chop a Tools también si se van a usar.
            // Por ahora, solo conectamos la Minería.

            if (_btnMine != null)
            {
                _btnMine.onClick.AddListener(OnMineClicked);
            }
        }

        public void OnMineClicked()
        {
            if (_interactionController != null)
            {
                _interactionController.SetMiningTool();
                Debug.Log("[UI] Modo Minería (Área) Activado.");
            }
        }

        // Método para volver a inspección (podría ser botón Cancel o Esc)
        public void OnCancelClicked()
        {
            if (_interactionController != null)
            {
                _interactionController.SetInspectionTool();
            }
        }

        private void OnDestroy()
        {
            if (_btnMine != null) _btnMine.onClick.RemoveListener(OnMineClicked);
        }
    }
}
