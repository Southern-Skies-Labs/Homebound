using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class PathfindingService : MonoBehaviour
    {
        private GridManager _gridManager;
        private const int MAX_ITERATIONS = 5000; 

        private void Awake() => ServiceLocator.Register(this);
        private void Start() => _gridManager = ServiceLocator.Get<GridManager>();
        private void OnDestroy() => ServiceLocator.Unregister<PathfindingService>();

        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            if (_gridManager == null) return null;

            // 1. Obtener Nodos Base
            PathNode startNode = GetValidNodeNear(startPos);
            PathNode targetNode = GetValidNodeNear(targetPos);

            if (startNode == null || targetNode == null) 
            {
                Debug.LogWarning($"[Pathfinding] No se encontró nodo válido cerca de Inicio {startPos} o Fin {targetPos}");
                return null;
            }

            // 2. Algoritmo A* (Estándar)
            List<PathNode> openSet = new List<PathNode> { startNode };
            HashSet<PathNode> closedSet = new HashSet<PathNode>();

            startNode.GCost = 0;
            startNode.HCost = GetDistance(startNode, targetNode);
            
            int iterations = 0;

            while (openSet.Count > 0)
            {
                if (iterations++ > MAX_ITERATIONS) return null;

                PathNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost || openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost)
                        currentNode = openSet[i];
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode) return RetracePath(startNode, targetNode);

                foreach (PathNode neighbor in _gridManager.GetNeighbors(currentNode))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    float moveCost = currentNode.GCost + (GetDistance(currentNode, neighbor) * neighbor.MovementPenalty);
                    
                    if (moveCost < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GCost = moveCost;
                        neighbor.HCost = GetDistance(neighbor, targetNode);
                        neighbor.Parent = currentNode;

                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    }
                }
            }
            return null;
        }

        // --- MÉTODO DE BÚSQUEDA TOLERANTE ---
        // Si la coordenada exacta falla, busca arriba/abajo/lados.
        private PathNode GetValidNodeNear(Vector3 pos)
        {
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);
            int z = Mathf.RoundToInt(pos.z);

            // 1. Intento Directo
            PathNode node = _gridManager.GetNode(x, y, z);
            if (IsValid(node)) return node;

            // 2. Intento Vertical (Prioridad: Arriba -> Abajo)
            // Útil si el bot está hundido o el click fue en el suelo
            PathNode up = _gridManager.GetNode(x, y + 1, z);
            if (IsValid(up)) return up;
            
            PathNode down = _gridManager.GetNode(x, y - 1, z);
            if (IsValid(down)) return down;

            // 3. Intento Horizontal (Por si clicamos un muro)
            // (Opcional, expandir si es necesario)
            
            return null;
        }

        private bool IsValid(PathNode n)
        {
            return n != null && n.IsWalkableSurface;
        }

        private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode currentNode = endNode;
            while (currentNode != startNode)
            {
                path.Add(new Vector3(currentNode.X + 0.5f, currentNode.Y, currentNode.Z + 0.5f));
                currentNode = currentNode.Parent;
            }
            path.Reverse();
            return path;
        }

        private float GetDistance(PathNode nodeA, PathNode nodeB)
        {
            int dstX = Mathf.Abs(nodeA.X - nodeB.X);
            int dstY = Mathf.Abs(nodeA.Y - nodeB.Y);
            int dstZ = Mathf.Abs(nodeA.Z - nodeB.Z);
            return dstX + dstZ + dstY;
        }
    }
}