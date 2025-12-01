using System;
using UnityEngine;

namespace Homebound.Features.Navigation.Pathfinding
{
    public struct PathNode : IEquatable<PathNode>
    {
        public Vector3Int Position;

        public PathNode(Vector3Int pos)
        {
            Position = pos;
        }

        public bool Equals(PathNode other)
        {
            return Position.Equals(other.Position);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}
