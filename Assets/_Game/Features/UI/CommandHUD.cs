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
        [SerializeField] private Button _btnMove;
        [SerializeField] private Button _btnChop;

        private InteractionController _interactionController;

        private void Start()
        {
            _interactionController = FindFirstObjectByType<InteractionController>();

            if (_interactionController == null)
            {
                Debug.LogError("[CommandHUD] No se encontró InteractionController.");
                return;
            }
            
            _btnMove.onClick.AddListener(() => SetCommandMode(JobType.Move));
            _btnChop.onClick.AddListener(() => SetCommandMode(JobType.Chop));
        }
        
        private void SetCommandMode(JobType jobType)
        {
            Debug.Log($"[CommandHUD] Modo seleccionado: {jobType}");
            _interactionController.SetCommandMode(jobType);
        }
        
        public void OnMineClicked()
        {
            if (_interactionController != null)
            {
                _interactionController.SetCommandMode(JobType.Mine);
                Debug.Log("[UI] Modo Minería activado. Selecciona un bloque.");
            }
        }

        private void OnDestroy()
        {
            _btnChop.onClick.RemoveAllListeners();
            _btnMove.onClick.RemoveAllListeners();
        }
    }
}
