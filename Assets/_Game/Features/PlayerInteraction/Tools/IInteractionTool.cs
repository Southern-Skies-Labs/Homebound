using UnityEngine;

namespace Homebound.Features.PlayerInteraction.Tools
{
    public interface IInteractionTool
    {
        void EnterTool();
        void ExitTool();
        void UpdateTool();
        void OnDrawGizmos(); // Para debug visual si es necesario
    }
}
