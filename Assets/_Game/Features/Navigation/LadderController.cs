using UnityEngine;
using Unity.AI.Navigation;
using System.Collections;

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
            
            //Posicionamos en la base
            transform.position = bottomPos;
            
            //Configuramos el enlace relativo
            _link.startPoint = Vector3.zero;
            _link.endPoint = transform.InverseTransformPoint(topPos);
            
            //Ancho del enlace de 1 voxel
            _link.width = 2f;
            _link.area = 0;
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
