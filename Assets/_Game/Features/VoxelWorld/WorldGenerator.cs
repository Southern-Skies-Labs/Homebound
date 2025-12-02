using UnityEngine;
// using Unity.AI.Navigation;
using Homebound.Core;
using Homebound.Features.Navigation;

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
            InitializeNavigationGrid();
            CreateChunk();
        }
        
        private void InitializeNavigationGrid()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager != null)
            {
                gridManager.InitializeGrid(_mapSize, 20, _mapSize);
            }
            else
            {
                Debug.LogError("[WorldGenerator] GridManager no encontrado.");
            }
        }

        private void CreateChunk()
        {
            // 1. Crear objeto vacío
            GameObject chunkObj = new GameObject("World_Chunk_0_0");
            chunkObj.transform.parent = this.transform;
            
            // 2. Añadir script Chunk. 
            Chunk chunk = chunkObj.AddComponent<Chunk>();
            
            // 3. OBTENER (No añadir) el renderer que se creó automáticamente
            var renderer = chunkObj.GetComponent<MeshRenderer>();
            
            // 4. Verificación de seguridad antes de asignar material
            if (_voxelMaterial != null)
            {
                renderer.material = _voxelMaterial;
            }
            else
            {
                Debug.LogError("[WorldGenerator] ¡Falta asignar el Material Voxel en el Inspector!");
            }

            // 5. Inicializar lógica
            chunk.Initialize(_mapSize, 5, _mapSize);
        }
    }
}