using UnityEngine;
// using Unity.AI.Navigation; // Removing NavMesh dependency

namespace Homebound.Features.VoxelWorld
{
    public class WorldGenerator : MonoBehaviour
    {
        //Variables
        [Header("Settings")]
        [SerializeField] private int _mapSize = 50;
        [SerializeField] private int _mapHeight = 5;
        [SerializeField] private Material _voxelMaterial;

        // [Header("Navigation")]
        // [SerializeField] private NavMeshSurface _navMeshSurface;

        
        //Metodos
        private void Awake()
        {
            // Inicializamos el servicio de mapa si no existe
            if (GetComponent<VoxelMapService>() == null)
            {
                var service = gameObject.AddComponent<VoxelMapService>();
                service.Configure(new Vector3Int(_mapSize, 256, _mapSize));
            }
        }

        private void Start()
        {
            CreateChunk();
            // BakeNavigation(); // Ya no usamos NavMesh
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
            // Enviamos coord (0,0,0) como base
            chunk.Initialize(_mapSize, _mapHeight, _mapSize, Vector3Int.zero);
        }

        /*
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
        */
    }
}