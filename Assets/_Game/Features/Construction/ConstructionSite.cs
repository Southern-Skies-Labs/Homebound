using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Homebound.Core;
using Homebound.Features.Economy;

namespace Homebound.Features.Construction
{
    public enum ConstructionPhase
    {
        NotStarted,
        Phase1_Structure,
        Phase2_Details,
        Completed
    }
    
    public class ConstruictionSite : MonoBehaviour
    {
        //Variables
        [Header("Configuración")] 
        [SerializeField] private BuildingBlueprint _blueprint;
        
        [Header("Estado (Solo lectura)")]
        [SerializeField] private ConstructionPhase _currentPhase = ConstructionPhase.NotStarted;
        
        //Colas de construcción
        private Queue<BlueprintBlock> _pendingStructure;
        private Queue<BlueprintBlock> _pendingDetails;

        private CityInventory _inventory;
        
        //Metodos

        private void Start()
        {
            _inventory = ServiceLocator.Get<CityInventory>();

            if (_blueprint != null)
            {
                InitializeSite(_blueprint);
            }
        }
        


        public void InitializeSite(BuildingBlueprint blueprint)
        {
            _blueprint = blueprint;
            _currentPhase = ConstructionPhase.NotStarted;

            var sortedStructure = blueprint.StructureBlocks
                .OrderBy(b => b.LocalPosition.y)
                .ThenBy(b => b.LocalPosition.x)
                .ThenBy(b => b.LocalPosition.z)
                .ToList();
            
            _pendingStructure = new Queue<BlueprintBlock>(sortedStructure);

            var sortedDetails = blueprint.DetailBlocks
                .OrderBy(b => b.LocalPosition.y)
                .ToList();

            _pendingDetails = new Queue<BlueprintBlock>(sortedDetails);

            TryAdvancePhase();
        }

        private void TryAdvancePhase()
        {
            switch (_currentPhase)
            {
                case ConstructionPhase.NotStarted:
                    if (CheckResourcesForNextBlock(_pendingStructure))
                    {
                        _currentPhase = ConstructionPhase.Phase1_Structure;
                        Debug.Log($"[ConstructionSite] Iniciando Fase 1: Estructura");
                    }

                    break;
                case ConstructionPhase.Phase1_Structure:
                    if (_pendingStructure.Count == 0)
                    {
                        _currentPhase = ConstructionPhase.Phase2_Details;
                        Debug.Log("[ConstrucionSite] Fase 1 Completa. Iniciando Fase 2: Detalles");

                        TryAdvancePhase();
                    }
                    break;
                case ConstructionPhase.Phase2_Details:
                    if (_pendingDetails.Count == 0)
                    {
                        _currentPhase = ConstructionPhase.Completed;
                        Debug.Log("[ConstrucionSite] Fase 2 Completa. Construcción Completada");
                    }
                    break;
            }
        }

        public BlueprintBlock? GetNextBlockToBuild()
        {
            if (_currentPhase == ConstructionPhase.Phase1_Structure && _pendingStructure.Count > 0)
            {
                return _pendingStructure.Peek();
            }
            else if (_currentPhase == ConstructionPhase.Phase2_Details && _pendingDetails.Count > 0)
            {
                return _pendingDetails.Peek();
            }

            return null;
        }

        public void ConfirmBlockBuilt()
        {
            if (_currentPhase == ConstructionPhase.Phase1_Structure && _pendingStructure.Count > 0)
            {
                _pendingStructure.Dequeue(); 
            }
            else if (_currentPhase == ConstructionPhase.Phase2_Details && _pendingDetails.Count > 0)
            {
                _pendingDetails.Dequeue();
            }

            TryAdvancePhase();
        }

        private bool CheckResourcesForNextBlock(Queue<BlueprintBlock> queue)
        {
            if (queue.Count == 0) return true;
            return true;
        }
    }
}