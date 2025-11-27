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
        
        private NavMeshLink _link;

        //Metodos
        public void Initialize(Vector3 bottomPos, Vector3 topPos, LadderType type, float duration)
        {
            _link = GetComponent<NavMeshLink>();
            Type = type;
            ExpiryTime = duration;
            
            //Snapping al Navmesh
            NavMeshHit hit;
            
            //Ajustamos base
            if (NavMesh.SamplePosition(bottomPos, out hit, 1.0f, NavMesh.AllAreas))
            {
                bottomPos = hit.position;
            }
            
            //Ajustamos cima
            if (NavMesh.SamplePosition(topPos, out hit, 1.0f, NavMesh.AllAreas))
            {
                topPos = hit.position;
            }
            
            transform.position = bottomPos;
            transform.rotation = Quaternion.identity;
            
            _link.startPoint = Vector3.zero;
            _link.endPoint = transform.InverseTransformPoint(topPos);

            _link.width = 1.5f;
            _link.costModifier = -1;
            _link.bidirectional = true;
            _link.autoUpdate = true;

            StartCoroutine(UpdateLinkRoutine());

        }
        
        private IEnumerator UpdateLinkRoutine()
        {
            _link.enabled = false;
            yield return null;
            _link.enabled = true;
            
            _link.UpdateLink();

        }
        
        
    }
}
