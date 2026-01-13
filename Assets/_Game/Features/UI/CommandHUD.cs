using UnityEngine;
using Homebound.Core;
using Homebound.Features.PlayerInteraction;

namespace Homebound.Features.UI
{
    public class CommandHUD : MonoBehaviour
    {
        private InteractionController _interaction;

        private void Start()
        {
            // Buscamos el controlador al inicio
            _interaction = ServiceLocator.Get<InteractionController>();

            if (_interaction == null)
            {
                Debug.LogError("[CommandHUD] CRÍTICO: No se encontró InteractionController. Asegúrate de que existe en la escena.");
            }
        }

        // --- MÉTODOS PÚBLICOS (Estos sí aparecerán en el Inspector) ---

        public void OnClick_SelectMode()
        {
            Debug.Log("[UI] Click: Seleccionar");
            if (_interaction != null) _interaction.SetCommandMode(CommandMode.Select);
        }

        public void OnClick_MineSingle()
        {
            Debug.Log("[UI] Click: Minar (Simple)");
            if (_interaction != null) _interaction.SetCommandMode(CommandMode.MineSingle);
        }

        public void OnClick_MineArea()
        {
            Debug.Log("[UI] Click: Minar (Área)");
            if (_interaction != null) _interaction.SetCommandMode(CommandMode.MineArea);
        }
    }
}