using UnityEngine;

namespace Homebound.Features.Navigation
{
    public enum NodeType
    {
        Air,            // Vacío
        Solid,          // Muro/Tierra (No se puede estar DENTRO)
        Road,           // Camino construido (Bonificador de velocidad)
        Obstacle        // Bloqueo temporal (Muebles, Trampas)
    }

    public class PathNode
    {
        public int X;
        public int Y;
        public int Z;

        public NodeType Type;
        
        // Cache de navegación: True si este nodo es Aire PERO tiene suelo firme debajo.
        public bool IsWalkableSurface; 
        
        // 1.0f = Normal, 0.5f = Rápido (Road)
        public float MovementPenalty; 

        // Variables A*
        public float GCost;
        public float HCost;
        public PathNode Parent;

        public float FCost => GCost + HCost;

        public PathNode(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
            Type = NodeType.Air;
            IsWalkableSurface = false;
            MovementPenalty = 1.0f;
        }
    }
}