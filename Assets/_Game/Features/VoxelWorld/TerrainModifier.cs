using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.VoxelWorld
{
    public class TerrainModifier : MonoBehaviour
    {
        private void Awake() => ServiceLocator.Register(this);
        // Sin OnDestroy/Unregister por brevedad, pero deberías añadirlo.

        public void DestroyBlockAt(Vector3 worldPos)
        {
            // Lanzamos un rayo pequeño o usamos Physics.Overlap para encontrar el Chunk
            // O, si tus Chunks tienen un sistema de coordenadas predecible, calculamos el índice.
            // Método robusto por física (ya que los chunks tienen MeshCollider):
            
            // Buscamos el centro del bloque
            Vector3 center = new Vector3(Mathf.Round(worldPos.x), Mathf.Round(worldPos.y), Mathf.Round(worldPos.z));
            
            // Raycast minúsculo para detectar el Chunk
            if (Physics.Raycast(center + Vector3.up, Vector3.down, out RaycastHit hit, 2f, LayerMask.GetMask("Terrain")))
            {
                Chunk chunk = hit.collider.GetComponent<Chunk>();
                if (chunk != null)
                {
                    // Convertir Mundo -> Local del Chunk
                    // Asumiendo que el chunk pivote está alineado correctamente o usamos transform.InverseTransformPoint
                    Vector3 localPos = chunk.transform.InverseTransformPoint(center);
                    
                    int lx = Mathf.RoundToInt(localPos.x); // + Offset si tu chunk empieza en negativo
                    int ly = Mathf.RoundToInt(localPos.y);
                    int lz = Mathf.RoundToInt(localPos.z);
                    
                    // Como tu Chunk.cs usa _xOffset para centrar (width/2), debemos compensar al buscar el índice del array
                    // Mirando tu Chunk.cs: worldX = transform.x + (x - offset). 
                    // Entonces: x = worldX - transform.x + offset
                    
                    // REVISIÓN DE TU CHUNK.CS:
                    // private int _xOffset = _width / 2;
                    // float worldX = transform.position.x + (x - _xOffset);
                    
                    // FÓRMULA INVERSA:
                    // arrayIndexX = (int)(center.x - chunk.transform.position.x) + (chunkWidth / 2);
                    // arrayIndexZ = (int)(center.z - chunk.transform.position.z) + (chunkWidth / 2);
                    
                    // Necesitamos exponer _width o hacer un método público en Chunk "GetLocalCoords".
                    // Por ahora, usaremos una aproximación basada en tu código:
                    
                    // Mejor opción: Añade este método a Chunk.cs para no adivinar offsets fuera
                    // public void DestroyBlockGlobal(Vector3 worldPos) { ... logica inversa ... }
                    
                    // Pero para no modificar tanto Chunk ahora, usaremos el método ModifyVoxel directo
                    // asumiendo que calculamos bien los índices.
                    
                    // Solución rápida: Modifica Chunk.cs para que acepte WorldPos.
                    // (Ver abajo)
                }
            }
        }
    }
}