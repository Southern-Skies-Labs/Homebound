using UnityEngine;
using Homebound.Core;
using Homebound.Features.AethianAI;

namespace Homebound.Features.PlayerInteraction.Tools
{
    public class InspectionTool : IInteractionTool
    {
        private InteractionController _controller;
        private Camera _camera;
        private LayerMask _unitLayer;

        public InspectionTool(InteractionController controller, Camera camera, LayerMask unitLayer)
        {
            _controller = controller;
            _camera = camera;
            _unitLayer = unitLayer;
        }

        public void EnterTool()
        {
            // Debug.Log("[InspectionTool] Modo Inspección.");
        }

        public void ExitTool() { }

        public void UpdateTool()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleSelection();
            }
        }

        public void OnDrawGizmos() { }

        private void HandleSelection()
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _unitLayer))
            {
                var bot = hit.collider.GetComponentInParent<AethianBot>();
                if (bot != null)
                {
                    _controller.SelectUnit(bot);
                    return;
                }
            }

            // Si click en nada, deseleccionar
            _controller.SelectUnit(null);
        }
    }
}
