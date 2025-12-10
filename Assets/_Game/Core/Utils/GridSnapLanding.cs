using System.Collections;
using UnityEngine;

namespace Homebound.Core
{
    /// <summary>
    /// Componente independiente que fuerza al objeto a aterrizar en coordenadas Y enteras (Grilla).
    /// Útil para inicializar Bots y objetos de escena antes de que el mundo termine de generarse.
    /// </summary>
    [DefaultExecutionOrder(-10)] 
    public class GridSnapLanding : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Altura desde la cual se busca el suelo.")]
        [SerializeField] private float _raycastOriginHeight = 50f;
        
        [Tooltip("Capas consideradas 'Suelo' (Default, Terrain, etc).")]
        [SerializeField] private LayerMask _groundLayer = 1; // Default por defecto (capa 0)

        [Tooltip("Si es true, se destruye este componente tras aterrizar para ahorrar memoria.")]
        [SerializeField] private bool _autoRemoveAfterLanding = true;

        [Header("Estado")]
        [SerializeField] private bool _hasLanded = false;

        private IEnumerator Start()
        {
            yield return null;
            
            int attempts = 0;
            while (!_hasLanded && attempts < 60) 
            {
                TryLand();
                if (!_hasLanded)
                {
                    yield return new WaitForSeconds(0.1f); 
                    attempts++;
                }
                else
                {
                    yield break; // Éxito
                }
            }

            if (!_hasLanded)
            {
                Debug.LogError($"[GridSnapLanding] {name} no encontró suelo tras varios intentos. Se queda flotando.");
            }
        }

        [ContextMenu("Forzar Aterrizaje Ahora")]
        public void TryLand()
        {
            
            Vector3 castOrigin = new Vector3(transform.position.x, transform.position.y + _raycastOriginHeight, transform.position.z);
            
            RaycastHit[] hits = Physics.RaycastAll(castOrigin, Vector3.down, 200f, _groundLayer);

            RaycastHit validHit = new RaycastHit();
            bool found = false;

            
            float maxY = -9999f;

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == this.gameObject) continue; 
                if (hit.collider.isTrigger) continue; 

                if (hit.point.y > maxY)
                {
                    maxY = hit.point.y;
                    validHit = hit;
                    found = true;
                }
            }

            if (found)
            {
                float exactY = validHit.point.y;
                int roundedY = Mathf.RoundToInt(exactY);

                Vector3 finalPos = new Vector3(transform.position.x, roundedY, transform.position.z);
                transform.position = finalPos;

                _hasLanded = true;
                // Debug.Log($"[GridSnapLanding] {name} aterrizó en Y={roundedY} (Detectado: {exactY:F2})");

                if (_autoRemoveAfterLanding)
                {
                    Destroy(this);
                }
            }
        }
    }
}