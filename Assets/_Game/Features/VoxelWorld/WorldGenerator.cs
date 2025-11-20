using System;
using UnityEngine;

namespace Homebound.Features.VoxelWorld
{
    public class WorldGenerator : MonoBehaviour
    {
        //Variables
        [Header("Settings")] 
        [SerializeField] private int _mapSize = 50;
        [SerializeField] private Material _voxelMaterial;
        
        //Metodos

        private void Start()
        {
            CreateChunk();
        }
        
        private void CreateChunk()
        {
            GameObject chunkObj = new GameObject("World_Chunk_0_0");
            chunkObj.transform.parent = this.transform;
            
            //Configuramos los componentes
            Chunk chunk = chunkObj.AddComponent<Chunk>();
            var renderer = chunkObj.AddComponent<MeshRenderer>();
            renderer.material = _voxelMaterial;
            
            //Inicializamos un mapa de 50x5x50
            chunk.Initialize(_mapSize, 5, _mapSize);
        }
    }
}

