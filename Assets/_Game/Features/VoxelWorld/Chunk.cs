using UnityEngine;
using System.Collections.Generic;
using Homebound.Features.VoxelWorld;
using Homebound.Features.Navigation;
using Homebound.Core;

namespace Homebound.Features.VoxelWorld
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        [Header("Generación")]
        [SerializeField] private int _width = 50;
        [SerializeField] private int _height = 30;
        [SerializeField] private float _noiseScale = 0.03f;
        [SerializeField] private float _heightMultiplier = 15f;

        [Header("Probabilidad de Minerales (0-1)")]
        [SerializeField] private float _coalChance = 0.05f;   // 5%
        [SerializeField] private float _copperChance = 0.03f; // 3%
        [SerializeField] private float _goldChance = 0.01f;   // 1%

        // Datos
        private BlockType[,,] _blocks;
        private Mesh _mesh;
        
        // Listas de Malla
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector3> _uvs = new List<Vector3>(); 

        private int _xOffset;
        private int _zOffset;

        public void Initialize(int width, int height, int depth)
        {
            _width = width; _height = height;
            _xOffset = _width / 2; _zOffset = width / 2;
            _blocks = new BlockType[_width, _height, width];
            
            _mesh = new Mesh();
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            
            GetComponent<MeshFilter>().mesh = _mesh;
            gameObject.layer = LayerMask.NameToLayer("Terrain");

            GenerateMapData();
            CreateMeshData();
            
            RegisterTerrainToGrid(); 
        }

        private void GenerateMapData()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _width; z++)
                {
                    // Posición global para que el ruido sea coherente
                    float worldX = transform.position.x + (x - _xOffset); 
                    float worldZ = transform.position.z + (z - _zOffset);

                    // Altura del terreno base
                    int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(worldX * _noiseScale, worldZ * _noiseScale) * _heightMultiplier) + 5;
                    // float noiseVal = Mathf.PerlinNoise(worldX * _noiseScale, worldZ * _noiseScale);
                    // noiseVal = Mathf.Pow(noiseVal, 1.5f); // <--- Juega con este 2.0f (prueba 1.5f a 3.0f)
                    // int terrainHeight = Mathf.FloorToInt(noiseVal * _heightMultiplier) + 5;
                    
                    for (int y = 0; y < _height; y++)
                    {
                        if (y == 0) 
                        {
                            _blocks[x, y, z] = BlockType.Bedrock; // Fondo irrompible
                        }
                        else if (y < terrainHeight)
                        {
                            // Superficie
                            if (y == terrainHeight - 1) 
                            {
                                _blocks[x, y, z] = BlockType.Grass; 
                            }
                            // Subsuelo inmediato (Tierra)
                            else if (y > terrainHeight - 4) 
                            {
                                _blocks[x, y, z] = BlockType.Dirt;
                            }
                            // Profundidad (Piedra y Minerales)
                            else 
                            {
                                // Lógica Procedural de Minerales (Simple Probabilidad 3D)
                                // Usé un ruido 3D simple o Random determinista basado en coordenadas
                                float oreNoise = Mathf.PerlinNoise(worldX * 0.1f + y * 0.5f, worldZ * 0.1f);
                                
                                float rng = Random.value; 
                                
                                if (rng < _goldChance && y < 10) _blocks[x, y, z] = BlockType.Gold; // Oro solo profundo
                                else if (rng < _copperChance) _blocks[x, y, z] = BlockType.Copper;
                                else if (rng < _coalChance) _blocks[x, y, z] = BlockType.Coal;
                                else _blocks[x, y, z] = BlockType.Stone;
                            }
                        }
                        else
                        {
                            _blocks[x, y, z] = BlockType.Air;
                        }
                    }
                }
            }
        }

        private void CreateMeshData()
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear(); 

            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _width; z++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (_blocks[x, y, z] != BlockType.Air)
                        {
                            AddVoxelDataToChunk(x, y, z);
                        }
                    }
                }
            }

            _mesh.Clear();
            _mesh.vertices = _vertices.ToArray();
            _mesh.triangles = _triangles.ToArray();
            _mesh.SetUVs(0, _uvs); // Asignamos UVs
            
            _mesh.RecalculateNormals();
            GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        private void AddVoxelDataToChunk(int x, int y, int z)
        {
            BlockType type = _blocks[x, y, z];
            Vector3 worldPos = new Vector3(x, y ,z);

            float textureIndex = VoxelData.GetTextureIndex(type, worldPos);
            
            for (int p = 0; p < 6; p++)
            {
                if (!CheckVoxel(x + VoxelData.FaceChecks[p].x, y + VoxelData.FaceChecks[p].y, z + VoxelData.FaceChecks[p].z))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int triangleIndex = VoxelData.VoxelTris[p, i];
                        Vector3 pos = VoxelData.VoxelVerts[triangleIndex] + new Vector3(x - _xOffset, y, z - _zOffset);
                        _vertices.Add(pos);
                        
                        Vector3 uv = Vector3.zero;
                        uv.z = textureIndex;

                        // Mapeo simple de esquinas (0,0), (0,1), etc.
                        // Según el orden de VoxelTris: 0 (BL), 1 (BR), 2 (TL), 3 (TR)
                        // Verifica visualmente si las texturas salen rotadas
                        if(i == 0) { uv.x = 0; uv.y = 0; }
                        if(i == 1) { uv.x = 1; uv.y = 0; }
                        if(i == 2) { uv.x = 0; uv.y = 1; }
                        if(i == 3) { uv.x = 1; uv.y = 1; }
                        
                        _uvs.Add(uv);
                    }

                    int vertCount = _vertices.Count;

                    // Triángulo 1: (0, 1, 2) -> Esquina, Arriba, Derecha

                    _triangles.Add(vertCount - 4);

                    _triangles.Add(vertCount - 3);

                    _triangles.Add(vertCount - 2);


                    // Triángulo 2: (2, 1, 3) -> Derecha, Arriba, Opuesto

                    _triangles.Add(vertCount - 2);

                    _triangles.Add(vertCount - 3);

                    _triangles.Add(vertCount - 1);
                }
            }
        }

        private bool CheckVoxel(int x, int y, int z)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height || z < 0 || z >= _width) return false;
            return _blocks[x, y, z] != BlockType.Air;
        }


        private void RegisterTerrainToGrid()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("[Chunk] No se encontró GridManager para registrar el terreno.");
                return;
            }

            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _width; z++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        BlockType block = _blocks[x, y, z];
                        NodeType nodeType = NodeType.Air;

                        switch (block)
                        {
                            case BlockType.Air:
                                nodeType = NodeType.Air;
                                break;

                            case BlockType.Bedrock:
                                nodeType = NodeType.Solid; // Impasable
                                break;
                            
                            default: nodeType = NodeType.Solid; break;
                        }

                        int worldX = (int)transform.position.x + (x - _xOffset);
                        int worldZ = (int)transform.position.z + (z - _zOffset);
                        int worldY = (int)transform.position.y + y;

                        gridManager.SetNode(worldX, worldY, worldZ, nodeType);
                    }
                }
            }

            Debug.Log($"[Chunk] Terreno registrado en el Grid ({_width}x{_height}x{_width}).");
        }
        
        public void ModifyVoxel(int x, int y, int z, BlockType newType)
        {
            // 4. Verificación de Límites EXPLÍCITA
            if (x < 0 || x >= _width || y < 0 || y >= _height || z < 0 || z >= _width) // Nota: Usas _width para profundidad también
            {
                Debug.LogError($"[Chunk] ❌ ERROR DE LÍMITES: Intento modificar [{x},{y},{z}] pero el mapa es de {_width}x{_height}x{_width}.");
                return;
            }

            // 5. Verificación de Estado Actual
            if (_blocks[x, y, z] == newType)
            {
                Debug.LogWarning($"[Chunk] ⚠️ Aviso: El bloque en [{x},{y},{z}] ya es de tipo {newType}. No hubo cambios.");
                return;
            }

            Debug.Log($"[Chunk] ✅ ÉXITO: Cambiando bloque [{x},{y},{z}] de {_blocks[x,y,z]} a {newType}. Regenerando malla...");

            _blocks[x, y, z] = newType;
            
            // Regeneración
            CreateMeshData(); 
            
            // Actualización lógica
            UpdateGridLogic(x, y, z, newType);
        }

        private void UpdateGridLogic(int x, int y, int z, BlockType type)
        {
            var grid = ServiceLocator.Get<GridManager>();
            if (grid == null) return;

            int worldX = (int)transform.position.x + (x - _xOffset);
            int worldZ = (int)transform.position.z + (z - _zOffset);
            int worldY = (int)transform.position.y + y;

            NodeType nodeType = (type == BlockType.Air) ? NodeType.Air : NodeType.Solid;
            
            grid.SetNode(worldX, worldY, worldZ, nodeType);
        }
        
        public void DestroyBlockAtWorldPos(Vector3 worldPos)
        {
            // 1. Log de entrada
            // Debug.Log($"[Chunk] Intento de destruir en WorldPos: {worldPos}");

            // 2. Conversión de coordenadas (La matemática sospechosa)
            // Recordamos: float worldX = transform.position.x + (x - _xOffset);
            // Por tanto: x = (worldX - transform.position.x) + _xOffset;
            
            // Usamos FloorToInt o RoundToInt dependiendo de cómo generaste la malla. 
            // Tu generación usa enteros directos, así que RoundToInt es lo correcto para el centro del bloque.
            
            int arrayX = Mathf.RoundToInt(worldPos.x - transform.position.x) + _xOffset;
            int arrayZ = Mathf.RoundToInt(worldPos.z - transform.position.z) + _zOffset;
            int arrayY = Mathf.RoundToInt(worldPos.y - transform.position.y);

            Debug.Log($"[Chunk] Conversión: World {worldPos} -> Local Array [{arrayX}, {arrayY}, {arrayZ}] (Offset: {_xOffset}, {_zOffset})");

            // 3. Llamada segura
            ModifyVoxel(arrayX, arrayY, arrayZ, BlockType.Air);
        }
        
        
    }
}