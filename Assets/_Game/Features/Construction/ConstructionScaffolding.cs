using System;
using UnityEngine;
using System.Collections.Generic;
using Homebound.Core;
using Homebound.Features.Navigation;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.Construction
{
    public class ConstructionScaffolding : MonoBehaviour
    {
        //Variables
        [Header("Configuración")] 
        [SerializeField] private GameObject _scaffoldPrefab;
        [SerializeField] private Transform _scaffoldContainer;

        private List<GameObject> _activeScaffolds = new List<GameObject>();

        private ConstructionSite _site;
        private GridManager _gridManager;
        
        
        //Metodoos

        private void Awake()
        {
            _site = GetComponent<ConstructionSite>();
            _gridManager = ServiceLocator.Get<GridManager>();

            if (_scaffoldContainer == null)
            {
                GameObject container = new GameObject("Scaffolds");
                container.transform.SetParent(transform);
                _scaffoldContainer = container.transform;
            }
        }
        
        //API PUBLICA
        public void ClearScaffolds()
        {
            foreach (var scaffold in _activeScaffolds)
            {
                if (scaffold != null)
                {
                    Vector3Int pos = Vector3Int.RoundToInt(scaffold.transform.position);
                    

                    if (_gridManager != null) 
                        _gridManager.SetNode(pos.x, pos.y, pos.z, Homebound.Features.Navigation.NodeType.Air);
                    
                    Destroy(scaffold);
                }
            }
            _activeScaffolds.Clear();
            Debug.Log("[Scaffolding] Andamios retirados y Grid actualizado.");
        }

        public Vector3? GetRequiredScaffoldPosition(Vector3 blockWorldPos)
        {
            Vector3Int targetPos = Vector3Int.RoundToInt(blockWorldPos);

            if (targetPos.y <= 1) return null;

            Vector3Int standPos = targetPos + Vector3Int.down;

            Vector3Int[] neighbors =
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
            };

            foreach (var offset in neighbors)
            {
                Vector3Int checkPos = standPos + offset;

                if (IsScaffoldAt(checkPos)) return null;

                if (NeedsScaffoldBase(checkPos))
                {
                    return checkPos + Vector3Int.down;
                }
                return checkPos;
            }
            return null;    
        }

        public void BuildScaffold(Vector3 position)
        {
            GameObject newScaffold = Instantiate(_scaffoldPrefab, position, Quaternion.identity, _scaffoldContainer);
            _activeScaffolds.Add(newScaffold);
            
            Vector3Int pos = Vector3Int.RoundToInt(position);
            if (_gridManager != null) 
                _gridManager.SetNode(pos.x, pos.y, pos.z, Homebound.Features.Navigation.NodeType.Ground);
        }

        private bool IsScaffoldAt(Vector3Int pos)
        {
            foreach (var s in _activeScaffolds)
            {
                if (Vector3Int.RoundToInt(s.transform.position) == pos) return true;
            }

            return false;
        }

        private bool NeedsScaffoldBase(Vector3Int pos)
        {
            if (pos.y <= 0) return false;
            if(IsScaffoldAt(pos + Vector3Int.down)) return false;

            return true;
        }
        
        
        
        
        
        
        
    }
}

