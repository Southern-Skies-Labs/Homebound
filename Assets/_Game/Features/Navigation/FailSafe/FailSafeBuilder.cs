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
                // Calculamos la posición DEBAJO del camino (donde va la escalera)
                int targetY = Mathf.RoundToInt(pos.y) - 1; 
                PathNode floorNode = _gridManager.GetNode((int)pos.x, targetY, (int)pos.z);
                
                if (floorNode != null && floorNode.Type == NodeType.Air)
                {
                    nodesToBuild.Add(new Vector3Int((int)pos.x, targetY, (int)pos.z));
                }
            }

            foreach (Vector3Int buildPos in nodesToBuild)
            {
                Vector3 worldPos = new Vector3(buildPos.x + 0.5f, buildPos.y, buildPos.z + 0.5f); 

                // 1. Instanciar y Registrar
                Instantiate(_ladderPrefab, worldPos, Quaternion.identity, _ladderContainer);
                _gridManager.SetNode(buildPos.x, buildPos.y, buildPos.z, NodeType.Solid);

                // 2. [CRÍTICO] EFECTO POP-UP
                // Buscamos si hay algún bot atrapado justo en esta coordenada
                // Usamos un cubo pequeño en el centro del bloque recién creado
                Collider[] victims = Physics.OverlapBox(worldPos + Vector3.up * 0.5f, Vector3.one * 0.4f);
                
                foreach (var col in victims)
                {
                    // Buscamos el controlador de movimiento
                    var botController = col.GetComponentInParent<UnitMovementController>();
                    if (botController != null)
                    {
                        // ¡ELEVAMOS AL BOT!
                        // Lo subimos 1 metro para que quede SOBRE la escalera, no dentro.
                        botController.transform.position += Vector3.up * 1.05f; 
                        Debug.Log($"[FailSafe] Bot {botController.name} elevado para evitar quedar enterrado.");
                    }
                }

                yield return new WaitForSeconds(_buildDelayPerUnit);
            }
        }
    }
}