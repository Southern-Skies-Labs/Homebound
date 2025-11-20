using UnityEngine;
using Unity.AI.Navigation;

namespace Homebound.Features.VoxelWorld
{
    public class WorldGenerator : MonoBehaviour
    {
        //Variables
        [Header("Settings")]
        [SerializeField] private int _mapSize = 50;
        [SerializeField] private Material _voxelMaterial;

        [Header("Navigation")]
        [SerializeField] private NavMeshSurface _navMeshSurface;

        
        //Metodos
        private void Start()
        {
            CreateChunk();
            BakeNavigation();
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

        private void BakeNavigation()
        {
            if (_navMeshSurface != null)
            {
                // Reconstruir NavMesh en tiempo de ejecución
                _navMeshSurface.BuildNavMesh();
                Debug.Log("[WorldGenerator] NavMesh construido exitosamente.");
            }
            else
            {
                Debug.LogError("[WorldGenerator] ¡Falta asignar NavMeshSurface en el Inspector!");
            }
        }
    }
}