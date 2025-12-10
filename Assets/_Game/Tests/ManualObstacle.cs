using System.Collections;
using UnityEngine;
using Homebound.Core;
using Homebound.Features.Navigation;

namespace Homebound.Testing
{
    [RequireComponent(typeof(BoxCollider))]
    public class ManualObstacle : MonoBehaviour
    {
        [Tooltip("Si es true, destruye el render para ver si el bot choca con lo invisible")]
        [SerializeField] private bool _hideOnStart = false;

        private IEnumerator Start()
        {
            // 1. Esperar a que el sistema arranque (Evita la Condición de Carrera)
            yield return new WaitForSeconds(0.5f); 

            // 2. Buscar el Grid
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError($"[ManualObstacle] {name}: No encontré el GridManager.");
                yield break;
            }

            // 3. Calcular límites basados en el Collider
            BoxCollider box = GetComponent<BoxCollider>();
            Bounds bounds = box.bounds;

            // Convertimos las esquinas del collider a coordenadas de la grilla
            // Nota: Asumimos que bounds.min y max nos dan la cobertura completa
            Vector3Int min = gridManager.WorldToArray((int)Mathf.Floor(bounds.min.x), (int)Mathf.Floor(bounds.min.y), (int)Mathf.Floor(bounds.min.z));
            Vector3Int max = gridManager.WorldToArray((int)Mathf.Ceil(bounds.max.x), (int)Mathf.Ceil(bounds.max.y), (int)Mathf.Ceil(bounds.max.z));

            int count = 0;

            // 4. Barrido volumétrico: Marcar todo lo que esté dentro del cubo como SÓLIDO
            // Usamos un bucle forzado en coordenadas de Mundo para asegurar precisión
            for (float x = bounds.min.x; x < bounds.max.x; x += 0.5f) 
            {
                for (float y = bounds.min.y; y < bounds.max.y; y += 0.5f)
                {
                    for (float z = bounds.min.z; z < bounds.max.z; z += 0.5f)
                    {
                        // Redondeamos para encontrar el centro del voxel más cercano
                        int ix = Mathf.RoundToInt(x);
                        int iy = Mathf.RoundToInt(y);
                        int iz = Mathf.RoundToInt(z);

                        gridManager.SetNode(ix, iy, iz, NodeType.Solid);
                        count++;
                    }
                }
            }

            // Debug.Log($"[ManualObstacle] {name}: Muro inyectado en la Matrix. {count} nodos afectados.");
            
            if (_hideOnStart) GetComponent<Renderer>().enabled = false;
        }
    }
}