using UnityEngine;
using System.Collections.Generic;
using Homebound.Core;

namespace Homebound.Features.VoxelWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        // --- CONSTANTES ---
        private const int ID_AIR = 0;
        private const int ID_SURFACE = 1;
        private const int ID_SUBSURFACE = 2;
        private const int ID_DEEP = 3;
        private const int ID_LIQUID = 4;

        // --- DEPENDENCIAS ---
        private Vector2Int _coord;
        private INoiseGenerator _noise;
        private BiomeDefinition _biome;

        // --- CONFIGURACIÓN GLOBAL ---
        private float _globalNoiseScale;
        private int _globalBaseHeight;

        // --- DATOS ---
        private int[,,] _voxelMap = new int[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

        // DICCIONARIO DE DEFINICIONES (Optimización solicitada)
        private Dictionary<int, BlockDefinition> _blockLookup = new Dictionary<int, BlockDefinition>();

        // --- CACHE DE MESH ---
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private List<Vector3> _vertices = new();
        private List<int> _triangles = new();
        private List<Vector2> _uvs = new();
        private List<Vector3> _uvTextureIndices = new();

        private List<GameObject> _spawnedObjects = new List<GameObject>();

        public void Initialize(Vector2Int coord, INoiseGenerator noiseProvider, BiomeDefinition biome, Material mat, float noiseScale, int baseHeight)
        {
            _coord = coord;
            _noise = noiseProvider;
            _biome = biome;
            _globalNoiseScale = noiseScale;
            _globalBaseHeight = baseHeight;

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            _meshRenderer.sharedMaterial = mat;

            // CORRECCIÓN 1: Capa correcta
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            gameObject.layer = (terrainLayer != -1) ? terrainLayer : LayerMask.NameToLayer("Default");

            // OPTIMIZACIÓN: Construir el Diccionario de Bloques una sola vez
            BuildBlockLookup();

            PopulateVoxelMap();
            CreateMeshData();
            UpdateMesh();

            GenerateVegetation();
        }

        /// <summary>
        /// Construye el diccionario ID -> BlockDefinition para acceso rápido y seguro.
        /// </summary>
        private void BuildBlockLookup()
        {
            _blockLookup.Clear();

            // Bloques Estándar
            if (_biome.surfaceBlock != null) _blockLookup.Add(ID_SURFACE, _biome.surfaceBlock);
            if (_biome.subSurfaceBlock != null) _blockLookup.Add(ID_SUBSURFACE, _biome.subSurfaceBlock);
            if (_biome.deepBlock != null) _blockLookup.Add(ID_DEEP, _biome.deepBlock);
            if (_biome.liquidBlock != null) _blockLookup.Add(ID_LIQUID, _biome.liquidBlock);

            // Minerales (Ores)
            if (_biome.ores != null)
            {
                foreach (var ore in _biome.ores)
                {
                    if (ore.oreBlock != null && !_blockLookup.ContainsKey(ore.internalID))
                    {
                        _blockLookup.Add(ore.internalID, ore.oreBlock);
                    }
                }
            }
        }

        public int GetBlockAtLocalPos(int x, int y, int z)
        {
            if (!IsPosInChunk(new Vector3Int(x, y, z))) return ID_AIR;

            int internalID = _voxelMap[x, y, z];

            // Traducción para Navigation System
            if (internalID == ID_LIQUID) return 2; // Agua
            if (internalID > ID_AIR) return 1;     // Sólido
            return 0;
        }

        public void DestroyBlockAtWorldPos(Vector3 worldPos)
        {
            // Debug para verificar que la orden llega
            // Debug.Log($"[Chunk {_coord}] Intento de destruir en World: {worldPos}");

            Vector3Int localPos = new Vector3Int(
                Mathf.FloorToInt(worldPos.x) - (_coord.x * VoxelData.ChunkWidth),
                Mathf.FloorToInt(worldPos.y),
                Mathf.FloorToInt(worldPos.z) - (_coord.y * VoxelData.ChunkWidth)
            );

            if (IsPosInChunk(localPos))
            {
                int previousID = _voxelMap[localPos.x, localPos.y, localPos.z];
                if (previousID != ID_AIR)
                {
                    _voxelMap[localPos.x, localPos.y, localPos.z] = ID_AIR;

                    // IMPORTANTE: Regenerar malla y colisionador inmediatamente
                    CreateMeshData();
                    UpdateMesh();

                    // Debug.Log($"[Chunk {_coord}] Bloque {previousID} destruido en Local: {localPos}");
                }
            }
            else
            {
                Debug.LogWarning($"[Chunk {_coord}] Fallo al minar: Coordenada local {localPos} fuera de rango.");
            }
        }

        private bool IsPosInChunk(Vector3Int pos)
        {
            return pos.x >= 0 && pos.x < VoxelData.ChunkWidth &&
                   pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
                   pos.z >= 0 && pos.z < VoxelData.ChunkWidth;
        }

        void PopulateVoxelMap()
        {
            int worldXOffset = _coord.x * VoxelData.ChunkWidth;
            int worldZOffset = _coord.y * VoxelData.ChunkWidth;
            bool hasLiquidBlock = _blockLookup.ContainsKey(ID_LIQUID);

            // Offset de altura para permitir agua (centrado de ruido)
            int terrainOrigin = _globalBaseHeight - Mathf.FloorToInt(_biome.terrainScale * 0.5f);

            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    int terrainHeight = _noise.GetTerrainHeight(worldXOffset + x, worldZOffset + z, _globalNoiseScale, _biome.terrainScale, terrainOrigin);

                    for (int y = 0; y < VoxelData.ChunkHeight; y++)
                    {
                        if (y > terrainHeight)
                        {
                            if (y < _globalBaseHeight && hasLiquidBlock)
                                _voxelMap[x, y, z] = ID_LIQUID;
                            else
                                _voxelMap[x, y, z] = ID_AIR;
                        }
                        else if (y == terrainHeight)
                        {
                            _voxelMap[x, y, z] = ID_SURFACE;
                        }
                        else if (y > terrainHeight - 4)
                        {
                            _voxelMap[x, y, z] = ID_SUBSURFACE;
                        }
                        else
                        {
                            // Generación de Minerales
                            int blockToPlace = ID_DEEP; // Por defecto Piedra

                            if (_biome.ores != null)
                            {
                                foreach (var ore in _biome.ores)
                                {
                                    if (y >= ore.minDepth && y <= ore.maxDepth)
                                    {
                                        float oreNoise = Mathf.PerlinNoise(
                                            (worldXOffset + x) * ore.veinSize,
                                            (y * ore.veinSize) + (worldZOffset + z) * 0.1f
                                        );

                                        if (oreNoise > (1f - ore.rarity))
                                        {
                                            blockToPlace = ore.internalID;
                                            break;
                                        }
                                    }
                                }
                            }
                            _voxelMap[x, y, z] = blockToPlace;
                        }
                    }
                }
            }
        }

        void GenerateVegetation()
        {
            foreach (var obj in _spawnedObjects) { if (obj != null) Destroy(obj); }
            _spawnedObjects.Clear();

            if (_biome.treePrefabs == null || _biome.treePrefabs.Count == 0) return;

            int worldXOffset = _coord.x * VoxelData.ChunkWidth;
            int worldZOffset = _coord.y * VoxelData.ChunkWidth;
            System.Random prng = new System.Random(worldXOffset + worldZOffset + _globalBaseHeight);

            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--)
                    {
                        int blockID = _voxelMap[x, y, z];
                        if (blockID == ID_SURFACE)
                        {
                            if (prng.NextDouble() < _biome.treeProbability)
                            {
                                if (y + 1 < _globalBaseHeight) { } // No árboles bajo agua
                                else SpawnTree(x, y + 1, z, prng);
                            }
                            break;
                        }
                        else if (blockID == ID_LIQUID) break;
                    }
                }
            }
        }

        private void SpawnTree(int x, int y, int z, System.Random prng)
        {
            GameObject prefab = _biome.treePrefabs[prng.Next(_biome.treePrefabs.Count)];
            if (prefab == null) return;

            GameObject tree = Instantiate(prefab, transform);
            tree.transform.localPosition = new Vector3(x + 0.5f, y, z + 0.5f);
            float randomYRot = (float)(prng.NextDouble() * 360f);
            tree.transform.localRotation = Quaternion.Euler(0, randomYRot, 0);
            _spawnedObjects.Add(tree);
        }

        void CreateMeshData()
        {
            _vertices.Clear(); _triangles.Clear(); _uvs.Clear(); _uvTextureIndices.Clear();

            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int x = 0; x < VoxelData.ChunkWidth; x++)
                {
                    for (int z = 0; z < VoxelData.ChunkWidth; z++)
                    {
                        if (_voxelMap[x, y, z] != ID_AIR) AddVoxelDataToChunk(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        void AddVoxelDataToChunk(Vector3Int pos)
        {
            int blockID = _voxelMap[pos.x, pos.y, pos.z];

            // --- USO DEL DICCIONARIO ---
            if (!_blockLookup.TryGetValue(blockID, out BlockDefinition blockToUse))
            {
                // Fallback de seguridad: Si el ID existe pero no tiene definición, usar Piedra.
                // Esto arregla los "agujeros invisibles" si configuraste mal los minerales.
                if (_blockLookup.ContainsKey(ID_DEEP)) blockToUse = _blockLookup[ID_DEEP];
                else return; // Si ni siquiera hay piedra, abortar.
            }

            for (int p = 0; p < 6; p++)
            {
                Vector3Int neighborPos = pos + VoxelData.FaceChecks[p];
                int neighborID = IsPosInChunk(neighborPos) ? _voxelMap[neighborPos.x, neighborPos.y, neighborPos.z] : ID_AIR;

                bool drawFace = false;
                if (blockID == ID_LIQUID)
                {
                    if (neighborID == ID_AIR) drawFace = true;
                }
                else
                {
                    // Dibujar si vecino es Aire o Agua
                    if (neighborID == ID_AIR || neighborID == ID_LIQUID) drawFace = true;
                }

                if (drawFace)
                {
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 0]]);
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 1]]);
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 2]]);
                    _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 3]]);

                    Vector3Int normal = VoxelData.FaceChecks[p];
                    Vector3 worldPos = new Vector3(pos.x + _coord.x * VoxelData.ChunkWidth, pos.y, pos.z + _coord.y * VoxelData.ChunkWidth);

                    // Cálculo de textura usando la definición obtenida del diccionario
                    float textureIndex = blockToUse.GetTextureIndex(normal, worldPos);

                    Vector3 uvData = new Vector3(textureIndex, 0, 0);
                    _uvTextureIndices.Add(uvData); _uvTextureIndices.Add(uvData);
                    _uvTextureIndices.Add(uvData); _uvTextureIndices.Add(uvData);

                    _uvs.Add(new Vector2(0, 0)); _uvs.Add(new Vector2(0, 1));
                    _uvs.Add(new Vector2(1, 0)); _uvs.Add(new Vector2(1, 1));

                    int vertCount = _vertices.Count;
                    _triangles.Add(vertCount - 4); _triangles.Add(vertCount - 3); _triangles.Add(vertCount - 2);
                    _triangles.Add(vertCount - 2); _triangles.Add(vertCount - 3); _triangles.Add(vertCount - 1);
                }
            }
        }

        void UpdateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.SetUVs(2, _uvTextureIndices);
            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh; // Crucial para que el Raycast de minería funcione
        }
    }
}