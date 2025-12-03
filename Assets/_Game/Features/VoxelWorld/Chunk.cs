using UnityEngine;
using System.Collections.Generic;
using Homebound.Features.VoxelWorld;

namespace Homebound.Features.VoxelWorld
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        [Header("Configuración de Generación")]
        [SerializeField] private int _width = 50;
        [SerializeField] private int _height = 30;
        [SerializeField] private float _noiseScale = 0.03f;
        [SerializeField] private float _heightMultiplier = 15f;

        private BlockType[,,] _blocks;
        private Mesh _mesh;
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Color> _colors = new List<Color>();

        private int _xOffset;
        private int _zOffset;

        public void Initialize(int width, int height, int depth)
        {
            _width = width;
            _height = height;
            
            _xOffset = _width / 2;
            _zOffset = width / 2;

            _blocks = new BlockType[_width, _height, width];
            
            _mesh = new Mesh();
            // Permite mapas grandes (>65k vértices)
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
            
            GetComponent<MeshFilter>().mesh = _mesh;
            gameObject.layer = LayerMask.NameToLayer("Terrain");

            GenerateMapData();
            CreateMeshData();
        }

        private void GenerateMapData()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _width; z++)
                {
                    float worldX = transform.position.x + (x - _xOffset); 
                    float worldZ = transform.position.z + (z - _zOffset);

                    int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(worldX * _noiseScale, worldZ * _noiseScale) * _heightMultiplier) + 5;

                    for (int y = 0; y < _height; y++)
                    {
                        if (y < terrainHeight)
                        {
                            if (y == terrainHeight - 1) _blocks[x, y, z] = BlockType.Stone; // Pasto
                            else _blocks[x, y, z] = BlockType.Dirt; // Tierra
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
            _colors.Clear();

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
            _mesh.colors = _colors.ToArray();
            
            _mesh.RecalculateNormals();
            
            GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        private void AddVoxelDataToChunk(int x, int y, int z)
        {
            for (int p = 0; p < 6; p++)
            {
                if (!CheckVoxel(x + VoxelData.FaceChecks[p].x, y + VoxelData.FaceChecks[p].y, z + VoxelData.FaceChecks[p].z))
                {
                    Color voxelColor = Color.white;
                    // Definición de colores (Ajusta los números si quieres tonos distintos)
                    if (_blocks[x,y,z] == BlockType.Dirt) voxelColor = new Color(0.54f, 0.27f, 0.07f); // Marrón
                    else if (_blocks[x,y,z] == BlockType.Stone) voxelColor = new Color(0.13f, 0.54f, 0.13f); // Verde

                    // Añadimos los 4 vértices de la cara
                    for (int i = 0; i < 4; i++)
                    {
                        int triangleIndex = VoxelData.VoxelTris[p, i];
                        Vector3 pos = VoxelData.VoxelVerts[triangleIndex] + new Vector3(x - _xOffset, y, z - _zOffset);
                        _vertices.Add(pos);
                        _colors.Add(voxelColor);
                    }

                    int vertCount = _vertices.Count;

                    // --- CORRECCIÓN DE TRIÁNGULOS (TOPOLOGÍA QUAD) ---
                    // Triángulo 1: (0, 1, 2) -> Esquina, Arriba, Derecha
                    _triangles.Add(vertCount - 4);
                    _triangles.Add(vertCount - 3);
                    _triangles.Add(vertCount - 2);

                    // Triángulo 2: (2, 1, 3) -> Derecha, Arriba, Opuesto
                    // Esta es la combinación que cierra el cuadrado perfectamente sin invertir la normal
                    _triangles.Add(vertCount - 2);
                    _triangles.Add(vertCount - 3);
                    _triangles.Add(vertCount - 1);
                    // ------------------------------------------------
                }
            }
        }

        private bool CheckVoxel(int x, int y, int z)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height || z < 0 || z >= _width)
                return false;

            return _blocks[x, y, z] != BlockType.Air;
        }
    }
}