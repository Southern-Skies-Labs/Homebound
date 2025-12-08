using UnityEngine;

namespace Homebound.Features.Economy
{
    public interface IGatherable
    {
        string Name { get; }
        Vector3 Position { get; }
        Transform Transform { get; }
        bool IsDepleted { get; }

      int Gather(float efficiency);

        InventorySlot GetDrop(); 
    }
}