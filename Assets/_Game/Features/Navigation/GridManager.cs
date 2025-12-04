using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class GridManager : MonoBehaviour
    {
        private PathNode[,,] _grid;
        private int _width, _height, _depth;
        private Vector3Int _gridOriginOffset; // Offset para centrar coordenadas

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister<GridManager>();

        public void InitializeGrid(int width, int height, int depth)
        {
            _width = width; _height = height; _depth = depth;
            _grid = new PathNode[width, height, depth];

            // Offset: Si width=50, offset es -25. El array [0] corresponde a mundo -25.
            int xOff = width / 2;
            int zOff = depth / 2;
            _gridOriginOffset = new Vector3Int(-xOff, 0, -zOff);

            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    for (int y = 0; y < height; y++)
                    {
                        int worldX = x + _gridOriginOffset.x;
                        int worldZ = z + _gridOriginOffset.z;
                        _grid[x, y, z] = new PathNode(worldX, y, worldZ);
                    }
            
            Debug.Log($"[GridManager] Grilla 2.0 inicializada: {width}x{height}x{depth}");
        }

        // Convierte Coordenada Mundo -> Índice Array
        public Vector3Int WorldToArray(int x, int y, int z)
        {
            return new Vector3Int(x - _gridOriginOffset.x, y - _gridOriginOffset.y, z - _gridOriginOffset.z);
        }

        // --- API DE ACTUALIZACIÓN ---
        public void SetNode(int worldX, int worldY, int worldZ, NodeType type)
        {
            Vector3Int idx = WorldToArray(worldX, worldY, worldZ);
            if (!CheckIndexBounds(idx.x, idx.y, idx.z)) return;

            PathNode node = _grid[idx.x, idx.y, idx.z];
            node.Type = type;
            node.MovementPenalty = (type == NodeType.Road) ? 0.5f : 1.0f;

            // Recalcular si este nodo y el de ARRIBA son superficies caminables
            UpdateWalkability(idx.x, idx.y, idx.z);
            UpdateWalkability(idx.x, idx.y + 1, idx.z); 
        }

        private void UpdateWalkability(int ix, int iy, int iz)
        {
            if (!CheckIndexBounds(ix, iy, iz)) return;

            PathNode node = _grid[ix, iy, iz];
            PathNode below = GetNodeByIndex(ix, iy - 1, iz);
            PathNode above = GetNodeByIndex(ix, iy + 1, iz);

            // REGLA DE ORO:
            // 1. Yo soy AIRE (no muro).
            // 2. Abajo hay SÓLIDO (suelo).
            // 3. Arriba hay AIRE (espacio cabeza).
            
            bool isSolid = (node.Type != NodeType.Air);
            bool isSolidBelow = (below != null && below.Type != NodeType.Air);
            bool isHeadClear = (above == null || above.Type == NodeType.Air);

            node.IsWalkableSurface = !isSolid && isSolidBelow && isHeadClear;
        }

        // --- LÓGICA DE VECINOS ---
        public List<PathNode> GetNeighbors(PathNode node)
        {
            List<PathNode> neighbors = new List<PathNode>();
            int[] xDir = { 0, 1, 0, -1 };
            int[] zDir = { 1, 0, -1, 0 };

            // Convertimos a índices internos para buscar
            Vector3Int centerIdx = WorldToArray(node.X, node.Y, node.Z);

            for (int i = 0; i < 4; i++)
            {
                int nx = centerIdx.x + xDir[i];
                int nz = centerIdx.z + zDir[i];

                // Revisamos niveles: -1 (Bajar), 0 (Plano), +1 (Subir)
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    int ny = centerIdx.y + yOffset;
                    
                    PathNode neighbor = GetNodeByIndex(nx, ny, nz);
                    
                    // Si el vecino es una superficie válida (Aire con suelo abajo)
                    if (neighbor != null && neighbor.IsWalkableSurface)
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }
            return neighbors;
        }

        public PathNode GetNode(int worldX, int worldY, int worldZ)
        {
            Vector3Int idx = WorldToArray(worldX, worldY, worldZ);
            return GetNodeByIndex(idx.x, idx.y, idx.z);
        }

        private PathNode GetNodeByIndex(int ix, int iy, int iz)
        {
            if (CheckIndexBounds(ix, iy, iz)) return _grid[ix, iy, iz];
            return null;
        }

        private bool CheckIndexBounds(int ix, int iy, int iz)
        {
            return ix >= 0 && ix < _width && iy >= 0 && iy < _height && iz >= 0 && iz < _depth;
        }
    }
}