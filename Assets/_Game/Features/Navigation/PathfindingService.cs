using System;
using UnityEngine;
using System.Collections.Generic;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class PathfindingService : MonoBehaviour
    {
        //Variables
        private GridManager _gridManager;
        
        //Limite de seguridad para evitar bucles
        private const int MAX_ITERATIONS = 5000;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PathfindingService>();
        }

        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            if (_gridManager == null) return null;

            // 1. Convertir posiciones de Mundo a Grilla (Asumiendo escala 1:1)
            PathNode startNode = _gridManager.GetNode(Mathf.RoundToInt(startPos.x), Mathf.RoundToInt(startPos.y), Mathf.RoundToInt(startPos.z));
            PathNode targetNode = _gridManager.GetNode(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y), Mathf.RoundToInt(targetPos.z));

            // Validaciones básicas
            if (startNode == null || targetNode == null)
            {
                // Debug.LogWarning("Pathfinding: Inicio o Destino fuera de límites.");
                return null;
            }

            // Opcional: Si el destino no es caminable, abortar inmediatamente
            // (A veces queremos ir 'cerca', pero por ahora strict check)
            if (!targetNode.IsWalkable) return null;

            // 2. Preparar listas (OpenSet y ClosedSet)
            List<PathNode> openSet = new List<PathNode>();
            HashSet<PathNode> closedSet = new HashSet<PathNode>();

            openSet.Add(startNode);
            
            // Ciclo de seguridad
            int iterations = 0;

            // 3. Bucle A*
            while (openSet.Count > 0)
            {
                // Early Exit: Evitar congelar Unity si la ruta es imposible y muy larga
                iterations++;
                if (iterations > MAX_ITERATIONS)
                {
                    Debug.LogWarning("[Pathfinding] Límite de iteraciones alcanzado. Ruta cancelada.");
                    return null;
                }

                // Buscar el nodo con menor FCost en el OpenSet
                PathNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost || (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                // HEMOS LLEGADO AL DESTINO
                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }

                // Procesar vecinos
                foreach (PathNode neighbor in _gridManager.GetNeighbors(currentNode))
                {
                    // Si no es caminable o ya lo visitamos, saltar
                    if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    // Coste del movimiento (simple = +10 para ortogonal)
                    // Usamos 10 para enteros para evitar floats, o simplemente gCost actual + distancia
                    int newMovementCostToNeighbor = currentNode.GCost + GetDistance(currentNode, neighbor);

                    if (newMovementCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GCost = newMovementCostToNeighbor;
                        neighbor.HCost = GetDistance(neighbor, targetNode);
                        neighbor.Parent = currentNode;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            // No se encontró ruta
            return null;
        }
        
        private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                // Convertir nodo a posición de mundo (+0.5f para centrar en el bloque)
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

            return (dstX + dstY + dstZ) * 10;
        }
        
    }
}
