using System;
using System.Collections.Generic;
using Homebound.Features.Navigation;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.VoxelWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        //Variables
        
        //Tamaño del chunk
        private int _width;
        private int _height;
        private int _depth;

        private BlockType[,,] _blocks;
        private Mesh _mesh;
        private GridManager _gridManager;
        
        //Listas para generar la Mesh
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Color> _colors = new List<Color>();
        
        
        //Metodos
        public void Initialize(int width, int height, int depth)
        {
            _width = width;
            _height = height;
            _depth = depth;
            
            _blocks = new BlockType[width, height, depth];
            _mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = _mesh;
            
            _gridManager = ServiceLocator.Get<GridManager>();

            GenerateMapData();
            UpdateMesh();
        }
        
        //MapGenerator
        private void GenerateMapData()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        // Generación básica
                        if (y == 0)
                        {
                            _blocks[x, y, z] = BlockType.Dirt;
  
                            NotifyGridUpdate(x, y, z, true); 
                        }
                        else
                        {
                            _blocks[x, y, z] = BlockType.Air;
 
                            NotifyGridUpdate(x, y, z, false);
                        }
                    }
                }
            }
        }
        
        private void NotifyGridUpdate(int x, int y, int z, bool isSolid)
        {
            if (_gridManager != null)
            {
                _gridManager.UpdateNode(x, y, z, isSolid);
            }
        }
        
    //MeshGenerator que convierte los datos en 3D
    private void UpdateMesh()
    {
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();
        
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    if (_blocks[x, y, z] != BlockType.Air)
                    {
                        AddVoxelDataToChunk(new Vector3Int(x, y, z));
                    } 
                }
            }
        } 
        
        _mesh.Clear();
        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();
        _mesh.colors = _colors.ToArray();
        
        _mesh.RecalculateNormals();
        
        //Asignamos el colisionador
        GetComponent<MeshCollider>().sharedMesh = _mesh;
    }
    
    private void AddVoxelDataToChunk(Vector3Int pos)
    {
        //Primero revisamos las 6 caras
        for (int p = 0; p < 6; p++)
        {
            if (!IsVoxelHidden(pos + VoxelData.FaceChecks[p]))
            {
                //Si la cara está visible, añadimos los 4 vertices
                int vertCount = _vertices.Count;
                
                _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 0]]);
                _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 1]]);
                _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 2]]);
                _vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 3]]);
                
                //Añadimos un color 
                Color voxelColor = new Color(0.5f, 0.3f, 0.1f);
                _colors.Add(voxelColor);
                _colors.Add(voxelColor);
                _colors.Add(voxelColor);
                _colors.Add(voxelColor);
                
                //Creamos los 2 triangulos
                _triangles.Add(vertCount);
                _triangles.Add(vertCount + 1);
                _triangles.Add(vertCount + 2);
                _triangles.Add(vertCount + 2);
                _triangles.Add(vertCount + 1);
                _triangles.Add(vertCount + 3);
            }
        }
    } 
    
        private bool IsVoxelHidden(Vector3Int pos)
        {
            //Si está fuera de los límites del chunk, asumimos que no está oculto y dibujamos el borde
            if (pos.x < 0 || pos.x >= _width || pos.y < 0 || pos.y >= _height || pos.z < 0 || pos.z >= _depth)
                return false;
            
            //Si el vecino es solido, la cara estará oculta
            return _blocks[pos.x, pos.y, pos.z] != BlockType.Air;
        }
    }
}

