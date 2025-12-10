using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Homebound.Core; // Para ServiceLocator

namespace Homebound.Features.Navigation.FailSafe
{
    public class FailSafeBuilder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _ladderPrefab;
        [SerializeField] private Transform _ladderContainer; // Para no ensuciar la jerarquía
        [SerializeField] private float _buildDelayPerUnit = 0.5f; // Tiempo que tarda en "construir" cada una

        private GridManager _gridManager;

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            
            if (_ladderContainer == null)
            {
                GameObject container = new GameObject("--- Emergency Structures ---");
                _ladderContainer = container.transform;
            }
        }

        private void Awake()
        {
            ServiceLocator.Register<FailSafeBuilder>(this); 
        }

        /// <summary>
        /// Construye escaleras en las posiciones indicadas y actualiza el Grid.
        /// Devuelve una Corrutina para que el Bot pueda esperar a que termine.
        /// </summary>
        public IEnumerator BuildEmergencyRouteRoutine(List<Vector3> pathNodes)
        {
            List<Vector3Int> nodesToBuild = new List<Vector3Int>();

            foreach (Vector3 pos in pathNodes)
            {
                // CORRECCIÓN CRÍTICA:
                // El pathNode es donde están los PIES del bot (Aire).
                // La escalera debe ir DEBAJO de los pies (Suelo).
                int targetY = Mathf.RoundToInt(pos.y) - 1; 
                
                // Obtenemos el nodo donde iría la escalera
                PathNode floorNode = _gridManager.GetNode((int)pos.x, targetY, (int)pos.z);
                
                // Si ese suelo no existe o es aire, necesitamos construir ahí
                if (floorNode != null && floorNode.Type == NodeType.Air)
                {
                    nodesToBuild.Add(new Vector3Int((int)pos.x, targetY, (int)pos.z));
                }
            }

            foreach (Vector3Int buildPos in nodesToBuild)
            {
                // 1. Instanciar Visual
                // Usamos 0.5 en X/Z para centrar en el voxel, y Y normal (base del cubo)
                Vector3 worldPos = new Vector3(buildPos.x + 0.5f, buildPos.y, buildPos.z + 0.5f); 

                GameObject newLadder = Instantiate(_ladderPrefab, worldPos, Quaternion.identity, _ladderContainer);
                
                // 2. Actualizar Lógica
                // Marcamos el nodo DEBAJO como Sólido. 
                // Esto hace que el nodo ARRIBA (donde camina el bot) sea IsWalkableSurface = true.
                _gridManager.SetNode(buildPos.x, buildPos.y, buildPos.z, NodeType.Solid);

                yield return new WaitForSeconds(_buildDelayPerUnit);
            }
        }
    }
}