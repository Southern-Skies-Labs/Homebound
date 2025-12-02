using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Homebound.Core;
using Homebound.Features.Economy;
using Homebound.Features.TaskSystem;
using Homebound.Features.VoxelWorld; 

namespace Homebound.Features.Construction
{
    public enum ConstructionPhase
    {
        NotStarted,
        Phase1_Structure,
        Phase2_Details,
        Completed
    }
    
    public class ConstructionSite : MonoBehaviour
    {
        //Variables
        [Header("Configuración")] 
        [SerializeField] private BuildingBlueprint _blueprint;
        [SerializeField] private int _maxWorkers = 4;
        [SerializeField] private ParticleSystem _completionParticles;

        
        [Header("Costes de Materiales")]
        [SerializeField] private List<BlockCostMapping> _blockCosts;

        [System.Serializable]
        public struct BlockCostMapping
        {
            public BlockType Block;
            public ItemData Item;
        }
        
        //Inyecciones
        [Header("Estado")]
        [SerializeField] private ConstructionPhase _currentPhase = ConstructionPhase.NotStarted;
        
        private Queue<BlueprintBlock> _pendingStructure;
        private Queue<BlueprintBlock> _pendingDetails;

        private CityInventory _inventory;
        private JobManager _jobManager;
        private List<JobRequest> _activeJobs = new List<JobRequest>();

        
        //Metodos
        private void Start()
        {
            _inventory = ServiceLocator.Get<CityInventory>();
            _jobManager = ServiceLocator.Get<JobManager>();

            if (_blueprint != null)
            {
                InitializeSite(_blueprint);
            }
        }

        private void OnDestroy()
        {
            ClearAllJobs();
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
            // Lógica de transición de fases
            if (_currentPhase == ConstructionPhase.NotStarted) _currentPhase = ConstructionPhase.Phase1_Structure;

            if (_currentPhase == ConstructionPhase.Phase1_Structure && _pendingStructure.Count == 0)
            {
                _currentPhase = ConstructionPhase.Phase2_Details;
                Debug.Log("[ConstructionSite] Fase 1 Completa. Iniciando Detalles.");
            }
            
            if (_currentPhase == ConstructionPhase.Phase2_Details && _pendingDetails.Count == 0)
            {
                FinishConstruction();
                return;
            }

            ManageJobs();
        }

        private void ManageJobs()
        {
            BlueprintBlock? nextBlock = GetNextBlockToBuild();
            if (nextBlock == null) return;

            ItemData requiredItem = GetItemForBlock(nextBlock.Value.Type);
            
            if (requiredItem != null && !_inventory.HasItem(requiredItem, 1))
            {
                // Debug.Log($"[ConstructionSite] Faltan recursos: {requiredItem.DisplayName}");
                ClearAllJobs();
                return;
            }

            int neededWorkers = _maxWorkers - _activeJobs.Count;
            if (neededWorkers > 0)
            {
                for (int i = 0; i < neededWorkers; i++) CreateJob();
            }
        }

        private void CreateJob()
        {
            if (_jobManager == null) return;

                JobRequest newJob = new JobRequest(
                "Construir " + _blueprint.name, // Nombre
                JobType.Build,                  // Tipo
                transform.position,             // Posición
                this.transform,                 // Target (Transform)
                1,                              // Prioridad
                OnJobCompleted                  // Callback
            );

            _activeJobs.Add(newJob);
            _jobManager.PostJob(newJob);
        }

        private void OnJobCompleted(JobRequest job)
        {
            if (_activeJobs.Contains(job)) _activeJobs.Remove(job);
            ManageJobs(); 
        }

        public void ClearAllJobs()
        {
            foreach (var job in _activeJobs)
            {
                job.ForceCancel(); // Ahora sí existe este método
            }
            _activeJobs.Clear();
        }


        public bool ConstructBlock()
        {
            BlueprintBlock? block = GetNextBlockToBuild();
            if (block == null) return false;

            
            ItemData itemCost = GetItemForBlock(block.Value.Type);

            
            if (itemCost == null || _inventory.TryConsume(itemCost, 1))
            {
                ConfirmBlockBuilt();
                return true; 
            }
            
            return false; 
        }

        public bool IsFinishedOrStalled()
        {
            return _currentPhase == ConstructionPhase.Completed;
        }

       //Metodos Auxiliares

        private BlueprintBlock? GetNextBlockToBuild()
        {
            if (_currentPhase == ConstructionPhase.Phase1_Structure && _pendingStructure.Count > 0) return _pendingStructure.Peek();
            if (_currentPhase == ConstructionPhase.Phase2_Details && _pendingDetails.Count > 0) return _pendingDetails.Peek();
            return null;
        }

        private void ConfirmBlockBuilt()
        {
            if (_currentPhase == ConstructionPhase.Phase1_Structure && _pendingStructure.Count > 0) _pendingStructure.Dequeue();
            else if (_currentPhase == ConstructionPhase.Phase2_Details && _pendingDetails.Count > 0) _pendingDetails.Dequeue();

            TryAdvancePhase();
        }

        private void FinishConstruction()
        {
            _currentPhase = ConstructionPhase.Completed;
            ClearAllJobs();
            Debug.Log("✨ CONSTRUCCIÓN COMPLETADA ✨");
            if (_completionParticles != null) Instantiate(_completionParticles, transform.position, Quaternion.identity);
        }

        
        private ItemData GetItemForBlock(BlockType blockType)
        {
            // Busca en la lista configurada en el inspector
            var mapping = _blockCosts.FirstOrDefault(x => x.Block == blockType);
            return mapping.Item; // Devuelve null si no se encuentra o el item si existe
        }
    }
}