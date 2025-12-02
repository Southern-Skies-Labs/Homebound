namespace Homebound.Features.Navigation
{
    public class PathNode
    {
        //Variables
        public int X;
        public int Y;
        public int Z;

        public bool IsWalkable;
        
        //Variables especificas para el A*
        public int GCost;
        public int HCost;
        public PathNode Parent;
        
        public int FCost => GCost + HCost;

        public PathNode(int x, int y, int z, bool isWalkable)
        {
            X = x;
            Y = y;
            Z = z;
            IsWalkable = isWalkable;
        }

        public override string ToString()
        {
            return $"Node ({X}, {Y}, {Z}) - Walkable: {IsWalkable}";
        }
    }
}
