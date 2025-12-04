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

        private ConstructionScaffolding _scaffolding;
        
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

        private CityInventory _inventoryInstance;
        private JobManager _jobManager;
        private List<JobRequest> _activeJobs = new List<JobRequest>();

        
        //Metodos
        private void Start()
        {
            _inventoryInstance = ServiceLocator.Get<CityInventory>();
            _jobManager = ServiceLocator.Get<JobManager>();
            _scaffolding = GetComponent<ConstructionScaffolding>();

            if (_blueprint != null)
            {
                InitializeSite(_blueprint);
            }
        }

        private void OnDestroy()
        {
            ClearAllJobs();
        }

        private void Awake()
        {
            if (_blockCosts == null) _blockCosts = new List<BlockCostMapping>();
        }

        public void InitializeSite(BuildingBlueprint blueprint)
        {
            _blueprint = blueprint;
            _currentPhase = ConstructionPhase.NotStarted;

           
            var structureList = blueprint.StructureBlocks ?? new List<BlueprintBlock>();
            var detailsList = blueprint.DetailBlocks ?? new List<BlueprintBlock>();

            
            var sortedStructure = structureList
                .OrderBy(b => b.LocalPosition.y)
                .ThenBy(b => b.LocalPosition.x)
                .ThenBy(b => b.LocalPosition.z)
                .ToList();
            
            _pendingStructure = new Queue<BlueprintBlock>(sortedStructure);

            
            var sortedDetails = detailsList
                .OrderBy(b => b.LocalPosition.y)
                .ToList();

            _pendingDetails = new Queue<BlueprintBlock>(sortedDetails);

            Debug.Log($"[ConstructionSite] Inicializado '{blueprint.name}'. Estructura: {_pendingStructure.Count}, Detalles: {_pendingDetails.Count}");

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
            
            if (requiredItem != null && (Inventory == null || !Inventory.HasItem(requiredItem, 1)))
            {
                // Debug.Log($"[ConstructionSite] Faltan recursos: {requiredItem.DisplayName}");
                ClearAllJobs();
                return;
            }

            if (_scaffolding != null)
            {
                
                Vector3 blockWorldPos = transform.TransformPoint(nextBlock.Value.LocalPosition);
            
                
                Vector3? scaffoldPos = _scaffolding.GetRequiredScaffoldPosition(blockWorldPos);
            
                if (scaffoldPos.HasValue)
                {
                    if (!JobExistsAt(scaffoldPos.Value))
                    {
                        CreateScaffoldJob(scaffoldPos.Value);
                    }
                    return; 
                }
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
                "Construir " + _blueprint.name,
                JobType.Build,
                transform.position,
                this.transform,
                1,
                UnitClass.Villager 
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
                job.Cancel(); // Ahora sí existe este método
            }
            _activeJobs.Clear();
        }


        public bool ConstructBlock()
        {
            
            if (_scaffolding != null)
            {
                BlueprintBlock? next = GetNextBlockToBuild();
                if (next.HasValue)
                {
                    Vector3 worldPos = transform.TransformPoint(next.Value.LocalPosition);
                    Vector3? scaffoldNeeded = _scaffolding.GetRequiredScaffoldPosition(worldPos);
                    
                    if (scaffoldNeeded.HasValue)
                    {
                        
                        _scaffolding.BuildScaffold(scaffoldNeeded.Value);
                        
                        return true; 
                    }
                }
            }

            
            BlueprintBlock? block = GetNextBlockToBuild();
            if (block == null) return false;

            ItemData itemCost = GetItemForBlock(block.Value.Type);

            if (itemCost == null || (Inventory != null && Inventory.TryConsume(itemCost, 1)))
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
            
            if(_scaffolding != null) _scaffolding.ClearScaffolds();
            
            Debug.Log("✨ CONSTRUCCIÓN COMPLETADA ✨");
            if (_completionParticles != null) Instantiate(_completionParticles, transform.position, Quaternion.identity);
        }

        
        private ItemData GetItemForBlock(BlockType blockType)
        {
            // Busca en la lista configurada en el inspector
            var mapping = _blockCosts.FirstOrDefault(x => x.Block == blockType);
            return mapping.Item; // Devuelve null si no se encuentra o el item si existe
        }

        private void CreateScaffoldJob(Vector3 pos)
        {
            if(_jobManager == null) return;

            JobRequest scaffoldJob = new JobRequest(
                "Construir Andamio",
                JobType.Build,
                transform.position,
                this.transform,
                1,
                UnitClass.Villager
                );
            _activeJobs.Add(scaffoldJob);
            _jobManager.PostJob(scaffoldJob);
            Debug.Log($"[Construction] Solicitando andamio en {pos}");
        }

        private void OnScaffoldJobCompleted(JobRequest job)
        {
            if (_scaffolding != null) _scaffolding.BuildScaffold(job.Position);

            if (_activeJobs.Contains(job)) _activeJobs.Remove(job);
            ManageJobs();
        }

        private bool JobExistsAt(Vector3 pos)
        {
            return _activeJobs.Exists(j => Vector3.Distance(j.Position, pos) < 0.1f);
        }
        
        private CityInventory Inventory
        {
            get
            {
                // Si está vacía, la buscamos en el momento justo
                if (_inventoryInstance == null)
                {
                    _inventoryInstance = ServiceLocator.Get<CityInventory>();
                }
                return _inventoryInstance;
            }
        }
    }
}