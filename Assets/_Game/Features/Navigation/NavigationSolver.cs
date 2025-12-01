using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class NavigationSolver : MonoBehaviour
    {
        private void Awake()
        {
            ServiceLocator.Register<NavigationSolver>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<NavigationSolver>();
        }

        public LadderConstructionRequest GetRecoverySolution(Vector3 currentPos, Vector3 targetDestination)
        {
            // --- PARÁMETROS ---
            float sphereRadius = 0.4f;      // Un poco más delgado para precisión
            float maxClimbHeight = 30f;
            
            // TRUCO DEL RETROCESO:
            // Empezamos el cast 1 metro DETRÁS del bot. 
            // Así aseguramos que si el bot está pegado al muro, el rayo lo detecte al entrar.
            float backOffset = 1.0f; 
            float checkDistance = 4.0f; // Aumentamos distancia para compensar el retroceso
            
            // --- 1. MÁSCARAS ---
            int layerIndex = LayerMask.NameToLayer("Obstacle");
            if (layerIndex == -1) { Debug.LogError("ERROR CRÍTICO: No existe Layer 'Obstacle'"); return LadderConstructionRequest.Invalid; }
            
            LayerMask detectionMask = LayerMask.GetMask("Obstacle", "Default", "Ground");

            // --- 2. ORIGEN RETRASADO ---
            // Usamos transform.forward para saber el frente, pero movemos el origen hacia atrás.
            Vector3 castDirection = transform.forward;
            Vector3 origin = currentPos + Vector3.up * 1.0f - (castDirection * backOffset); 

            // DIBUJO VISUAL (Amarillo = El rayo que busca el muro)
            Debug.DrawRay(origin, castDirection * checkDistance, Color.yellow, 1.0f);

            // --- 3. LANZAR SPHERECAST ---
            RaycastHit hitInfo;
            // Importante: QueryTriggerInteraction.Ignore para que no choque con triggers invisibles
            if (Physics.SphereCast(origin, sphereRadius, castDirection, out hitInfo, checkDistance, detectionMask, QueryTriggerInteraction.Ignore))
            {
                // FILTRO DE "AUTO-DETECCIÓN":
                // Si por error el rayo choca con el propio bot (si tiene collider en ese layer), lo ignoramos.
                if (hitInfo.collider.gameObject == this.gameObject) 
                {
                    Debug.LogWarning("Me he detectado a mí mismo. Ignorando.");
                    // (Aquí podrías lanzar otro raycast excluyéndote, pero con el backOffset no suele pasar)
                }

                Debug.Log($"[1/2] IMPACTO CON: {hitInfo.collider.name} (Layer: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)})");

                // --- 4. VALIDACIÓN DE ALTURA ---
                Vector3 pushInDirection = -hitInfo.normal; // Entramos perpendicular al muro
                Vector3 pointOnWall = hitInfo.point; 
                
                // Ajustamos bien adentro del muro para lanzar el rayo desde el cielo
                Vector3 skyOrigin = pointOnWall + (pushInDirection * 0.6f) + (Vector3.up * maxClimbHeight);

                // DIBUJO VISUAL (Cian = El rayo que mide la altura)
                Debug.DrawRay(skyOrigin, Vector3.down * maxClimbHeight, Color.cyan, 2.0f);

                if (Physics.Raycast(skyOrigin, Vector3.down, out RaycastHit topHit, maxClimbHeight, detectionMask))
                {
                    float heightDiff = topHit.point.y - currentPos.y;
                    Debug.Log($"[2/2] ALTURA DETECTADA: {heightDiff}m");

                    if (heightDiff > 0.5f && heightDiff <= maxClimbHeight)
                    {
                        Debug.Log(">>> SOLUCIÓN ENCONTRADA <<<");
                        
                        // Calculamos base y tope
                        Vector3 ladderBottom = pointOnWall + (hitInfo.normal * 0.6f); 
                        ladderBottom.y = currentPos.y; 

                        return LadderConstructionRequest.Create(ladderBottom, topHit.point, LadderType.Emergency, 15f);
                    }
                    else
                    {
                        Debug.LogWarning($"FALLO ALTURA: La altura {heightDiff} no está en rango (0.5 - {maxClimbHeight})");
                    }
                }
                else
                {
                    Debug.LogWarning("FALLO CIELO: El rayo desde el cielo no tocó nada (¿Muro muy fino o hueco?)");
                }
            }
            else
            {
                Debug.LogWarning("FALLO IMPACTO: El SphereCast no tocó NADA. Revisa Layers o Distancia.");
            }

            return LadderConstructionRequest.Invalid;
        }
    }
}

