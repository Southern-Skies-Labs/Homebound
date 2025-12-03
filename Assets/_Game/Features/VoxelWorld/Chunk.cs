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
        private List<Vector2> _uvs = new List<Vector2>(); 

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
                                // Usamos un ruido 3D simple o Random determinista basado en coordenadas
                                float oreNoise = Mathf.PerlinNoise(worldX * 0.1f + y * 0.5f, worldZ * 0.1f);
                                
                                // Podríamos usar Random.value para dispersión pura, o ruido para vetas.
                                // Usaremos Random con Seed para determinismo local simple por ahora.
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
            _mesh.uv = _uvs.ToArray(); // Asignamos UVs
            
            _mesh.RecalculateNormals();
            GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        private void AddVoxelDataToChunk(int x, int y, int z)
        {
            BlockType type = _blocks[x, y, z];
            Vector2[] uvs = VoxelData.GetUVs(type); 

            for (int p = 0; p < 6; p++)
            {
                if (!CheckVoxel(x + VoxelData.FaceChecks[p].x, y + VoxelData.FaceChecks[p].y, z + VoxelData.FaceChecks[p].z))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int triangleIndex = VoxelData.VoxelTris[p, i];
                        Vector3 pos = VoxelData.VoxelVerts[triangleIndex] + new Vector3(x - _xOffset, y, z - _zOffset);
                        _vertices.Add(pos);
                        
                        // Mapeo UV estándar para cada cara (0,0 -> 0,1 -> 1,1 -> 1,0)
                        // Aquí necesitamos mapear los 4 vértices del Quad a las 4 esquinas del UV que nos devolvió VoxelData
                        // Orden de VoxelTris: 0, 1, 2, 3 (Bl, Br, Tr, Tl) -> Depende de tu definición exacta de Tris
                        
                        // Simplificación para Quad UVs:
                        // Asumimos que VoxelData.GetUVs devuelve [BL, TL, TR, BR] o similar.
                        // Ajustaremos esto visualmente:
                        if(i == 0) _uvs.Add(uvs[0]); // 0,0
                        if(i == 1) _uvs.Add(uvs[3]); // 1,0
                        if(i == 2) _uvs.Add(uvs[1]); // 0,1
                        if(i == 3) _uvs.Add(uvs[2]); // 1,1
                        // Nota: El orden exacto puede requerir prueba y error según como rote la textura, 
                        // pero esto asigna una esquina de la textura a cada vértice.
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

            // Recorremos todo el chunk para "traducir" Bloque -> Nodo
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
                                nodeType = NodeType.ObstacleNatural; // Impasable
                                break;

                            // Todos los bloques sólidos caminables
                            case BlockType.Grass:
                            case BlockType.Dirt:
                            case BlockType.Stone:
                            case BlockType.Coal:
                            case BlockType.Copper:
                            case BlockType.Gold:
                                nodeType = NodeType.Ground;
                                break;

                            // Futuro: Si añades agua, sería ObstacleNatural
                            // Futuro: Si añades caminos, sería Road
                        }

                        // Enviamos el dato al Grid
                        // Nota: Usamos las mismas coordenadas locales x,y,z porque asumimos
                        // que el Grid y el Chunk están alineados en (0,0,0) del mundo lógico.
                        gridManager.SetNode(x, y, z, nodeType);
                    }
                }
            }

            Debug.Log($"[Chunk] Terreno registrado en el Grid ({_width}x{_height}x{_width}).");
        }
        
        
    }
}