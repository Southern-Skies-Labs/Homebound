using UnityEngine;
using System.Collections.Generic;
using Homebound.Core;

namespace Homebound.Features.VoxelWorld
{
    public class WorldGenerator : MonoBehaviour, IWorldDataProvider
    {
        [Header("World Settings")]
        [SerializeField] private int _worldSeed = 12345;
        [SerializeField] private int _worldSizeChunks = 8;

        [Header("Generation Parameters")]
        [SerializeField] private float _noiseScale = 50f;
        [SerializeField] private int _baseHeight = 10;

        [Header("Rendering & Biomes")]
        [SerializeField] private Material _voxelMaterial; // <--- NUEVO CAMPO
        [SerializeField] private BiomeDefinition _startingBiome;
        [SerializeField] private BiomeDefinition _defaultBiome;

        private Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();
        private INoiseGenerator _noiseGenerator;

        public static WorldGenerator Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            ServiceLocator.Register<IWorldDataProvider>(this);
            InitializeWorld();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IWorldDataProvider>();
        }

        private void Start()
        {
            GenerateWorld();
        }

        private void InitializeWorld()
        {
            _noiseGenerator = new FastNoiseGenerator(_worldSeed);

            // Validación de seguridad
            if (_voxelMaterial == null) Debug.LogError("CRITICAL: Voxel Material not assigned in WorldGenerator!");
            if (_defaultBiome == null) Debug.LogError("CRITICAL: Default Biome not assigned!");
        }

        public void GenerateWorld()
        {
            ClearWorld();

            int startRange = -(_worldSizeChunks / 2);
            int endRange = (_worldSizeChunks / 2);

            for (int x = startRange; x < endRange; x++)
            {
                for (int z = startRange; z < endRange; z++)
                {
                    CreateChunk(x, z);
                }
            }
        }

        private void CreateChunk(int x, int z)
        {
            Vector2Int coord = new Vector2Int(x, z);
            GameObject chunkObj = new GameObject($"Chunk_{x}_{z}");
            chunkObj.transform.parent = transform;
            chunkObj.transform.position = new Vector3(x * VoxelData.ChunkWidth, 0, z * VoxelData.ChunkWidth);

            Chunk newChunk = chunkObj.AddComponent<Chunk>();

            BiomeDefinition biomeToUse = (x == 0 && z == 0) ? _startingBiome : _defaultBiome;

            // Pasamos el material explícitamente
            newChunk.Initialize(coord, _noiseGenerator, biomeToUse, _voxelMaterial, _noiseScale, _baseHeight);

            _chunks.Add(coord, newChunk);
        }

        private void ClearWorld()
        {
            foreach (var chunk in _chunks.Values)
            {
                if (chunk != null && chunk.gameObject != null)
                    Destroy(chunk.gameObject);
            }
            _chunks.Clear();
        }

        public int GetBlockIDAt(Vector3Int globalPos)
        {
            int chunkX = Mathf.FloorToInt((float)globalPos.x / VoxelData.ChunkWidth);
            int chunkZ = Mathf.FloorToInt((float)globalPos.z / VoxelData.ChunkWidth);
            Vector2Int coord = new Vector2Int(chunkX, chunkZ);

            if (_chunks.TryGetValue(coord, out Chunk chunk))
            {
                int localX = globalPos.x - (chunkX * VoxelData.ChunkWidth);
                int localY = globalPos.y;
                int localZ = globalPos.z - (chunkZ * VoxelData.ChunkWidth);

                return chunk.GetBlockAtLocalPos(localX, localY, localZ);
            }

            return -1;
        }
    }
}