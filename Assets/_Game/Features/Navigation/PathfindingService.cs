using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class PathfindingService : MonoBehaviour
    {
        private GridManager _gridManager;
        private const int MAX_ITERATIONS = 10000; // Seguridad

        private void Awake() => ServiceLocator.Register(this);
        private void Start() => _gridManager = ServiceLocator.Get<GridManager>();
        private void OnDestroy() => ServiceLocator.Unregister<PathfindingService>();

        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            if (_gridManager == null) return null;

            // Convertir Mundo -> Grilla
            PathNode startNode = _gridManager.GetNode(Mathf.RoundToInt(startPos.x), Mathf.RoundToInt(startPos.y), Mathf.RoundToInt(startPos.z));
            PathNode targetNode = _gridManager.GetNode(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y), Mathf.RoundToInt(targetPos.z));

            if (startNode == null || targetNode == null) return null;
            
            // NOTA: A veces el target es un obstáculo (ej: click en muro).
            // Deberíamos buscar el vecino caminable más cercano, pero por ahora strict check.
            if (!targetNode.IsWalkable) return null;

            List<PathNode> openSet = new List<PathNode> { startNode };
            HashSet<PathNode> closedSet = new HashSet<PathNode>();

            startNode.GCost = 0;
            startNode.HCost = GetDistance(startNode, targetNode);
            
            int iterations = 0;

            while (openSet.Count > 0)
            {
                if (iterations++ > MAX_ITERATIONS) break;

                PathNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost || openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost)
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode) return RetracePath(startNode, targetNode);

                foreach (PathNode neighbor in _gridManager.GetNeighbors(currentNode))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    // --- LÓGICA DE COSTES (ROADS) ---
                    // Distancia base * Coste del Terreno.
                    // Si es Road (0.5), moverse cuesta la mitad.
                    float moveCostToNeighbor = currentNode.GCost + (GetDistance(currentNode, neighbor) * neighbor.MovementCost);

                    if (moveCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GCost = moveCostToNeighbor;
                        neighbor.HCost = GetDistance(neighbor, targetNode);
                        neighbor.Parent = currentNode;

                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    }
                }
            }

            return null;
        }

        private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                // +0.5f para centrar en el bloque
                path.Add(new Vector3(currentNode.X + 0.5f, currentNode.Y, currentNode.Z + 0.5f));
                currentNode = currentNode.Parent;
            }
            path.Reverse();
            return path;
        }

        private int GetDistance(PathNode nodeA, PathNode nodeB)
        {
            int dstX = Mathf.Abs(nodeA.X - nodeB.X);
            int dstY = Mathf.Abs(nodeA.Y - nodeB.Y);
            int dstZ = Mathf.Abs(nodeA.Z - nodeB.Z);

            if (dstX > dstZ) return 14 * dstZ + 10 * (dstX - dstZ) + 10 * dstY;
            return 14 * dstX + 10 * (dstZ - dstX) + 10 * dstY;
        }
    }
}