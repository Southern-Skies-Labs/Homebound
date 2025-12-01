using UnityEngine;
using System.Collections;
using Homebound.Features.VoxelWorld;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class LadderController : MonoBehaviour
    {
        //Variables
        [Header("Settings")] 
        public LadderType Type;

        public float ExpiryTime = -1f;
        
        [SerializeField] private Transform visualMesh;

        private Vector3Int _startNode;
        private Vector3Int _endNode;
        private bool _isRegistered;

        private void OnDestroy()
        {
             if (_isRegistered)
             {
                 var map = ServiceLocator.Get<IVoxelMap>();
                 if (map != null)
                 {
                     map.UnregisterConnection(_startNode, _endNode);
                 }
             }
        }

        //Metodos
        public void Initialize(Vector3 bottomPos, Vector3 topPos, LadderType type, float duration)
        {
            Type = type;
            ExpiryTime = duration;
            
            // Ajuste simple: Las escaleras son conexiones entre centros de bloques
            // Pero visualmente pueden estar offseteadas.
            
            var map = ServiceLocator.Get<IVoxelMap>();
            if (map != null)
            {
                _startNode = map.WorldToBlock(bottomPos);
                _endNode = map.WorldToBlock(topPos);

                // Registramos conexión bidireccional en el mapa
                map.RegisterConnection(_startNode, _endNode, 1.0f);
                _isRegistered = true;

                // Ajustar posicion visual al centro
                transform.position = (bottomPos + topPos) * 0.5f;
                Vector3 direction = topPos - bottomPos;
                float height = direction.magnitude;

                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }

                Debug.Log($"[Ladder] Escalera grid registrada: {_startNode} <-> {_endNode}");
            }
        }
    }
}
