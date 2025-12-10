using UnityEngine;
using Homebound.Core;
using Homebound.Features.Navigation;

namespace Homebound.Testing
{
    [RequireComponent(typeof(BoxCollider))]
    public class ObstacleOverride : MonoBehaviour
    {
        private void Start()
        {
            RegisterObstacle();
        }

        [ContextMenu("Forzar Registro Ahora")]
        public void RegisterObstacle()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;

            Bounds bounds = GetComponent<BoxCollider>().bounds;
            
            // Convertimos los límites físicos del cubo a coordenadas de la grilla
            Vector3Int min = gridManager.WorldToArray((int)bounds.min.x, (int)bounds.min.y, (int)bounds.min.z);
            Vector3Int max = gridManager.WorldToArray((int)bounds.max.x, (int)bounds.max.y, (int)bounds.max.z);

            int registeredNodes = 0;

            // Recorremos todo el volumen del cubo gigante
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        // Convertimos de vuelta a mundo para usar la API pública
                        // (OJO: GridManager.SetNode espera coordenadas MUNDO, no array)
                        // Así que necesitamos revertir o usar una API interna.
                        // Asumiremos que SetNode usa coordenadas de mundo.
                        
                        // Calculamos la posición mundo real de este voxel iterado
                        // Usamos bounds.min como base
                        float worldX = bounds.min.x + (x - min.x); 
                        float worldY = bounds.min.y + (y - min.y);
                        float worldZ = bounds.min.z + (z - min.z);

                        // Hack rápido para alinear al centro del voxel
                        int ix = Mathf.RoundToInt(worldX);
                        int iy = Mathf.RoundToInt(worldY);
                        int iz = Mathf.RoundToInt(worldZ);

                        gridManager.SetNode(ix, iy, iz, NodeType.Solid);
                        registeredNodes++;
                    }
                }
            }
            
            Debug.Log($"[ObstacleOverride] Muro '{name}' registró {registeredNodes} bloques sólidos en el Grid.");
        }
    }
}