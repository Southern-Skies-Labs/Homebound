using UnityEngine;
using Unity.AI.Navigation;
using System.Collections;
using UnityEngine.AI;

namespace Homebound.Features.Navigation
{
    [RequireComponent(typeof(NavMeshLink))]
    public class LadderController : MonoBehaviour
    {
        //Variables
        [Header("Settings")] 
        public LadderType Type;

        public float ExpiryTime = -1f;
        
        [SerializeField] private NavMeshLink _link;
        [SerializeField] private Transform visualMesh;

        //Metodos
        public void Initialize(Vector3 bottomPos, Vector3 topPos, LadderType type, float duration)
        {
            _link = GetComponent<NavMeshLink>();
            Type = type;
            ExpiryTime = duration;

            //Snapping al Navmesh
            NavMeshHit hit;
            float snapDistance = 2.0f;

            //Ajustamos base
            if (NavMesh.SamplePosition(bottomPos, out hit, snapDistance, NavMesh.AllAreas))
            {
                bottomPos = hit.position;
            }
            else
            {
                Debug.LogWarning($"[LadderController] No se pudo ajustar la base de la escalera.");
            }

            //Ajustamos cima
            if (NavMesh.SamplePosition(topPos, out hit, snapDistance, NavMesh.AllAreas))
            {
                topPos = hit.position;
            }
            else
            {
                Debug.LogWarning($"[LadderController] No se pudo ajustar la cima de la escalera.");
            }

            transform.position = (bottomPos + topPos) * 0.5f;

            Vector3 direction = topPos - bottomPos;
            float height = direction.magnitude;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            //Configuración del NavMesh 

            // transform.position = bottomPos;
            // transform.rotation = Quaternion.identity;

            _link.startPoint = transform.InverseTransformPoint(bottomPos);
            _link.endPoint = transform.InverseTransformPoint(topPos);
            _link.width = 1.5f;
            _link.costModifier = -1;
            _link.bidirectional = true;
            _link.autoUpdate = true;
            _link.area = NavMesh.GetAreaFromName("Walkable");

            StartCoroutine(UpdateLinkRoutine());
            
            Debug.Log($"[Ladder] Escalera creada desde: {bottomPos} hasta {topPos}, con altura de {height:F2}m");

        }
        
        
        
        private IEnumerator UpdateLinkRoutine()
        {
            _link.enabled = false;
            yield return null;
            _link.enabled = true;
            
            _link.UpdateLink();

            if (!_link.activated)
            {
                Debug.LogWarning("No se pudo activas el NavMeshLink. Verifica las configuraciones y posiciones");
            }
        }
    }
}
