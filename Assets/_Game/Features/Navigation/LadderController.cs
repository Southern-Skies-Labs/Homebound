using UnityEngine;
using Unity.AI.Navigation;

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
            
            //Punto final relativo
            _link.endPoint = topPos - bottomPos;
            
            //Ancho del enlace de 1 voxel
            _link.width = 1f;
            
            //Es para que suban y bajen
            _link.bidirectional = true;
            
            //Actualizamos el enlace para que el NavMesh lo reconozca
            _link.UpdateLink();

        }
    }
}
