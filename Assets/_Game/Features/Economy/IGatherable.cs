using UnityEngine;

namespace Homebound.Features.Economy
{
    public interface IGatherable
    {
        string Name { get; }
        
        // Aplicamos el daño al recurso, si el recurso se rompe, retorna true
        bool Gather(float efficiency);
        
        //Que item suelta y cuanto de este suelta
        InventorySlot GetDrop();
        
        //Posición para que el aldeano se acerque para recoletar
        Vector3 GetPosition();
        
        //El objeto fisico del tipo Transform asociado al recurso
        Transform Transform { get; }
    }
}