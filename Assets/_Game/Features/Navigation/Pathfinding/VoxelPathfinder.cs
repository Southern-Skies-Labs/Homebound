using System.Collections.Generic;
using UnityEngine;
using Homebound.Features.VoxelWorld;
using Homebound.Core;

namespace Homebound.Features.Navigation.Pathfinding
{
    public class VoxelPathfinder : MonoBehaviour
    {
        private IVoxelMap _map;

        // Direcciones de movimiento planas (N, S, E, W)
        private readonly Vector3Int[] _directions = new Vector3Int[]
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0)
        };

        private void Start()
        {
            _map = ServiceLocator.Get<IVoxelMap>();
        }

        public List<Vector3> FindPath(Vector3 startPos, Vector3 endPos)
        {
            if (_map == null) _map = ServiceLocator.Get<IVoxelMap>();

            Vector3Int startNode = _map.WorldToBlock(startPos);
            Vector3Int endNode = _map.WorldToBlock(endPos);

            // Validar si el destino es alcanzable "teoricamente" (no está dentro de un bloque sólido)
            if (_map.GetBlock(endNode) != BlockType.Air)
            {
                Debug.LogWarning("[Pathfinder] Destino inválido (dentro de bloque)");
                return null;
            }

            var pathNodes = AStarSearch(startNode, endNode);

            if (pathNodes != null)
            {
                return SimplifyPath(pathNodes);
            }

            return null;
        }

        private List<Vector3Int> AStarSearch(Vector3Int start, Vector3Int end)
        {
            // Open Set: Nodos por evaluar
            List<Vector3Int> openSet = new List<Vector3Int>();
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>(); // Evaluados

            // Diccionarios para guardar estado
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float>();
            Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float>();

            openSet.Add(start);
            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            int maxIterations = 5000; // Seguridad
            int iterations = 0;

            while (openSet.Count > 0)
            {
                iterations++;
                if (iterations > maxIterations)
                {
                    Debug.LogWarning("[Pathfinder] Path too long or loop detected");
                    return null;
                }

                // Obtener nodo con menor F
                Vector3Int current = GetLowestF(openSet, fScore);

                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Vector3Int neighbor in GetNeighbors(current))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    float tentativeG = gScore[current] + Vector3Int.Distance(current, neighbor); // Coste 1 para cardinles, sqrt(2) para pasos

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null; // No path
        }

        private List<Vector3Int> GetNeighbors(Vector3Int current)
        {
            List<Vector3Int> neighbors = new List<Vector3Int>();

            foreach (var dir in _directions)
            {
                Vector3Int target = current + dir;

                // 1. Movimiento Plano
                if (CanMoveTo(target))
                {
                    neighbors.Add(target);
                }
                // 2. Step Up (Target + Up)
                else
                {
                    Vector3Int upTarget = target + Vector3Int.up;
                    if (CanMoveTo(upTarget) && _map.GetBlock(current + Vector3Int.up) == BlockType.Air) // Head clearance
                    {
                        neighbors.Add(upTarget);
                    }
                    // 3. Step Down (Target + Down)
                    else
                    {
                         Vector3Int downTarget = target + Vector3Int.down;
                         if (CanMoveTo(downTarget))
                         {
                             neighbors.Add(downTarget);
                         }
                    }
                }
            }

            // 4. Escaleras / Conexiones
            var connections = _map.GetConnections(current);
            if (connections != null)
            {
                foreach (var conn in connections)
                {
                     // Asumimos que si está registrada, es válida
                     neighbors.Add(conn);
                }
            }

            return neighbors;
        }

        private bool CanMoveTo(Vector3Int target)
        {
            // Debe ser aire (u otro pasable)
            if (_map.GetBlock(target) != BlockType.Air) return false;

            // Debe tener suelo debajo
            if (_map.GetBlock(target + Vector3Int.down) == BlockType.Air) return false;

            // Debe tener espacio para la cabeza (2 bloques de altura)
            if (_map.GetBlock(target + Vector3Int.up) != BlockType.Air) return false;

            return true;
        }

        private Vector3Int GetLowestF(List<Vector3Int> openSet, Dictionary<Vector3Int, float> fScore)
        {
            Vector3Int lowest = openSet[0];
            float minVal = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;

            for (int i = 1; i < openSet.Count; i++)
            {
                float val = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : float.MaxValue;
                if (val < minVal)
                {
                    minVal = val;
                    lowest = openSet[i];
                }
            }
            return lowest;
        }

        private float Heuristic(Vector3Int a, Vector3Int b)
        {
            // Manhattan distance es mejor para grids, pero Euclidean funciona
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }

        private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
        {
            List<Vector3Int> totalPath = new List<Vector3Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Insert(0, current);
            }
            return totalPath;
        }

        private List<Vector3> SimplifyPath(List<Vector3Int> pathNodes)
        {
            List<Vector3> simplified = new List<Vector3>();
            if (pathNodes.Count == 0) return simplified;

            // Convertir a coordenadas de mundo (centros)
            foreach (var node in pathNodes)
            {
                simplified.Add(_map.BlockToWorldCenter(node));
            }
            return simplified;
        }
    }
}
