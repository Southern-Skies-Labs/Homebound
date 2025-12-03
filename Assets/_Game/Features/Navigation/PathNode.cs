namespace Homebound.Features.Navigation
{
    public enum NodeType
    {
        Air,            // Vacío (No se puede pisar, pero se puede atravesar si hay suelo abajo)
        Ground,         // Terreno natural (Coste 1)
        Road,           // Camino construido (Coste 0.5 - ¡Más rápido!)
        ObstacleNatural,// Montaña/Agua (Bloqueo natural)
        ObstaclePlayer  // Muro/Edificio (Bloqueo artificial)
    }

    public class PathNode
    {
        public int X;
        public int Y;
        public int Z;

        public NodeType Type;
        public bool IsWalkable; 
        
        // Penalización de movimiento 
        public float MovementCost; 

        // Variables A*
        public float GCost; 
        public float HCost;
        public PathNode Parent;

        public float FCost => GCost + HCost;

        public PathNode(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
            Type = NodeType.Air;
            IsWalkable = false;
            MovementCost = 1.0f;
        }

        public override string ToString()
        {
            return $"Node ({X},{Y},{Z}) - {Type}";
        }
    }
}