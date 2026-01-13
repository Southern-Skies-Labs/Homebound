using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class GridManager : MonoBehaviour
    {
        private PathNode[,,] _grid;
        private int _width, _height, _depth;
        private Vector3Int _gridOriginOffset;

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister<GridManager>();

        private void Start()
        {
            // Inicialización automática al arrancar (o llamar manualmente desde GameController)
            // Aseguramos que el mapa sea lo suficientemente grande para cubrir los chunks
            InitializeGrid(128, 64, 128); // Ajusta según el tamaño de tu WorldSizeChunks

            // IMPORTANTE: Esperar un frame o llamar esto después de que WorldGenerator termine
            Invoke(nameof(ScanWorld), 0.1f);
        }

        public void InitializeGrid(int width, int height, int depth)
        {
            _width = width; _height = height; _depth = depth;
            _grid = new PathNode[width, height, depth];

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

            Debug.Log($"[GridManager] Grilla inicializada: {width}x{height}x{depth}");
        }


        [ContextMenu("Force Scan World")]
        public void ScanWorld()
        {
            // 1. Obtener el proveedor desde el Core (Sin dependencia directa a VoxelWorld)
            var worldProvider = ServiceLocator.Get<IWorldDataProvider>();

            if (worldProvider == null)
            {
                Debug.LogError("[GridManager] No se encontró un IWorldDataProvider registrado.");
                return;
            }

            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        Vector3Int worldPos = new Vector3Int(x + _gridOriginOffset.x, y, z + _gridOriginOffset.z);

                        // 2. Usar la interfaz para consultar
                        int blockID = worldProvider.GetBlockIDAt(worldPos);

                        // 3. Interpretar ID
                        NodeType type = NodeType.Air;
                        if (blockID == 1) type = NodeType.Solid;
                        else if (blockID == 2) type = NodeType.Water;

                        SetNodeInternal(x, y, z, type);
                    }
                }
            }

            RefreshAllWalkability();
            Debug.Log("[GridManager] Mundo escaneado vía Interfaz.");
        }

        public void SetNode(int worldX, int worldY, int worldZ, NodeType type)
        {
            Vector3Int idx = WorldToArray(worldX, worldY, worldZ);
            SetNodeInternal(idx.x, idx.y, idx.z, type);

            // Actualizar vecindad local inmediata
            UpdateWalkability(idx.x, idx.y, idx.z);
            UpdateWalkability(idx.x, idx.y + 1, idx.z);
        }

        private void SetNodeInternal(int x, int y, int z, NodeType type)
        {
            if (!CheckIndexBounds(x, y, z)) return;
            _grid[x, y, z].Type = type;
        }

        private void RefreshAllWalkability()
        {
            for (int x = 0; x < _width; x++)
                for (int z = 0; z < _depth; z++)
                    for (int y = 0; y < _height; y++)
                        UpdateWalkability(x, y, z);
        }

        private void UpdateWalkability(int ix, int iy, int iz)
        {
            if (!CheckIndexBounds(ix, iy, iz)) return;

            PathNode node = _grid[ix, iy, iz];
            PathNode below = GetNodeByIndex(ix, iy - 1, iz);
            PathNode above = GetNodeByIndex(ix, iy + 1, iz);

            // --- REGLAS DE NAVEGACIÓN v0.3.5 ---

            bool walkable = false;
            float penalty = 0;

            // CASO 1: Tierra Firme
            // Yo soy Aire, Abajo Sólido, Arriba Aire
            if (node.Type == NodeType.Air && below != null && below.Type == NodeType.Solid && (above == null || above.Type == NodeType.Air))
            {
                walkable = true;
                penalty = 0;
            }
            // CASO 2: Agua Poco Profunda (Riachuelo)
            // Yo soy Agua, Abajo es Sólido (Profundidad 1)
            else if (node.Type == NodeType.Water && below != null && below.Type == NodeType.Solid)
            {
                walkable = true;
                penalty = 10; // Costo alto por caminar en agua
            }
            // CASO 3: Agua Profunda
            // Yo soy Agua, Abajo es Agua -> NO CAMINABLE
            else if (node.Type == NodeType.Water && below != null && below.Type == NodeType.Water)
            {
                walkable = false;
            }

            node.IsWalkableSurface = walkable;
            node.MovementPenalty = penalty;
        }

        // ... (Resto de métodos: WorldToArray, TryReserve, GetNeighbors mantienen igual) ...

        public Vector3Int WorldToArray(int x, int y, int z)
        {
            return new Vector3Int(x - _gridOriginOffset.x, y - _gridOriginOffset.y, z - _gridOriginOffset.z);
        }

        public bool TryReserve(Vector3Int worldPos, object owner)
        {
            PathNode node = GetNode(worldPos.x, worldPos.y, worldPos.z);
            if (node == null || !node.IsWalkableSurface) return false; // Solo reservar caminables
            if (node.IsReserved() && node.ReservedBy != owner) return false;
            node.Reserve(owner);
            return true;
        }

        public void ClearReservation(Vector3Int worldPos, object owner)
        {
            PathNode node = GetNode(worldPos.x, worldPos.y, worldPos.z);
            if (node != null && node.ReservedBy == owner) node.ClearReservation();
        }

        public List<PathNode> GetNeighbors(PathNode node, bool useEmergencyRules = false)
        {
            List<PathNode> neighbors = new List<PathNode>();
            int[] xDir = { 0, 1, 0, -1 };
            int[] zDir = { 1, 0, -1, 0 };

            Vector3Int centerIdx = WorldToArray(node.X, node.Y, node.Z);

            for (int i = 0; i < 4; i++)
            {
                int nx = centerIdx.x + xDir[i];
                int nz = centerIdx.z + zDir[i];

                // Revisamos nivel actual, abajo y arriba (para escaleras/saltos)
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    int ny = centerIdx.y + yOffset;
                    PathNode neighbor = GetNodeByIndex(nx, ny, nz);

                    if (neighbor == null) continue;

                    // Si es caminable (ya calculado en UpdateWalkability)
                    if (neighbor.IsWalkableSurface)
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