using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class GridManager : MonoBehaviour
    {
        private PathNode[,,] _grid;
        private int _width;
        private int _height;
        private int _depth;

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister<GridManager>();

        public void InitializeGrid(int width, int height, int depth)
        {
            _width = width; _height = height; _depth = depth;
            _grid = new PathNode[width, height, depth];

            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    for (int y = 0; y < height; y++)
                        _grid[x, y, z] = new PathNode(x, y, z);
            
            Debug.Log($"[GridManager] Grilla Táctica inicializada: {width}x{height}x{depth}");
        }

        // --- API DE ACTUALIZACIÓN ---

        // Llamado por el Chunk o ConstructionSite
        public void SetNode(int x, int y, int z, NodeType type)
        {
            if (!IsInsideGrid(x, y, z)) return;

            PathNode node = _grid[x, y, z];
            node.Type = type;

            // Configuramos las propiedades según el tipo
            switch (type)
            {
                case NodeType.Ground:
                    node.IsWalkable = true;
                    node.MovementCost = 1.0f; // Velocidad normal
                    break;
                
                case NodeType.Road:
                    node.IsWalkable = true;
                    node.MovementCost = 0.5f; // ¡Doble de rápido! (Prioridad A*)
                    break;

                case NodeType.Air:
                    node.IsWalkable = false; // No se pisa el aire
                    break;

                case NodeType.ObstacleNatural:
                case NodeType.ObstaclePlayer:
                    node.IsWalkable = false; // Bloqueado
                    break;
            }
        }

        // --- API DE CONSULTA ---

        public bool IsInsideGrid(int x, int y, int z)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height && z >= 0 && z < _depth;
        }

        public PathNode GetNode(int x, int y, int z)
        {
            if (IsInsideGrid(x, y, z)) return _grid[x, y, z];
            return null;
        }

        public List<PathNode> GetNeighbors(PathNode node)
        {
            List<PathNode> neighbors = new List<PathNode>();
            int[] xDir = { 0, 1, 0, -1 };
            int[] zDir = { 1, 0, -1, 0 };

            for (int i = 0; i < 4; i++)
            {
                int checkX = node.X + xDir[i];
                int checkZ = node.Z + zDir[i];

                // Lógica Voxel Simplificada para Fase 1:
                // Un vecino es válido si:
                // 1. La casilla destino NO es un obstáculo (Muro).
                // 2. La casilla DEBAJO del destino ES sólida (Suelo/Camino).
                //    (Opcional: Si implementamos saltos/caídas, esto cambia).
                
                // Revisamos la celda al mismo nivel (Caminar plano)
                CheckAndAddNeighbor(checkX, node.Y, checkZ, neighbors);
                
                // Revisamos celda abajo (Bajar escalón)
                CheckAndAddNeighbor(checkX, node.Y - 1, checkZ, neighbors);

                // Revisamos celda arriba (Subir escalón)
                CheckAndAddNeighbor(checkX, node.Y + 1, checkZ, neighbors);
            }
            return neighbors;
        }

        private void CheckAndAddNeighbor(int x, int y, int z, List<PathNode> list)
        {
            if (IsInsideGrid(x, y, z))
            {
                PathNode targetNode = _grid[x, y, z];
                
                // Regla: La celda destino debe ser AIRE (espacio para la cabeza)
                // Y la celda de abajo debe ser SUELO/ROAD.
                // PERO: Como definimos SetNode, si NodeType es Ground/Road, 
                // asumimos que el nodo representa la SUPERFICIE caminable.
                
                // Si tu Chunk define (x,0,z) como Tierra, ese nodo es Ground.
                // El bot camina SOBRE (x,1,z).
                // Ajustaremos esto en la integración.
                
                if (targetNode.IsWalkable)
                {
                    list.Add(targetNode);
                }
            }
        }
        
        // --- DEBUG GIZMOS (Para ver caminos y obstáculos) ---
        private void OnDrawGizmos()
        {
            if (_grid == null) return;
            Vector3 cam = Camera.main ? Camera.main.transform.position : Vector3.zero;

            for (int x = 0; x < _width; x++)
                for (int z = 0; z < _depth; z++)
                    if (Vector3.Distance(new Vector3(x, 0, z), new Vector3(cam.x, 0, cam.z)) < 30) // Optimización
                        for (int y = 0; y < _height; y++)
                        {
                            PathNode n = _grid[x, y, z];
                            if (n.Type == NodeType.ObstaclePlayer) 
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawWireCube(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), Vector3.one * 0.9f);
                            }
                            else if (n.Type == NodeType.Road)
                            {
                                Gizmos.color = Color.blue;
                                Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.1f, z + 0.5f), new Vector3(0.8f, 0.1f, 0.8f));
                            }
                        }
        }
    }
}