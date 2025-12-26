using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Homebound.Core;
using Homebound.Features.Economy;
using Homebound.Features.TaskSystem;
using Homebound.Features.VoxelWorld;
using Homebound.Features.Navigation; 

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

        [Header("Requisitos de Trabajo")]
        [Tooltip("Arrastra aquí el asset 'Villager_Data'")]
        [SerializeField] private UnitClassDefinition _requiredWorkerClass;

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
        private GridManager _gridManager; 

        private List<JobRequest> _activeJobs = new List<JobRequest>();

        //Metodos
        private void Awake()
        {
            if (_blockCosts == null) _blockCosts = new List<BlockCostMapping>();
        }

        private void Start()
        {
            _inventoryInstance = ServiceLocator.Get<CityInventory>();
            _jobManager = ServiceLocator.Get<JobManager>();
            _gridManager = ServiceLocator.Get<GridManager>();
            _scaffolding = GetComponent<ConstructionScaffolding>();

            if (_blueprint != null && _currentPhase == ConstructionPhase.NotStarted)
            {
                InitializeSite(_blueprint);
            }
        }

        private void OnDestroy()
        {
            ClearAllJobs();
            ReleaseGridReservations(); 
        }

        public void InitializeSite(BuildingBlueprint blueprint)
        {
            _blueprint = blueprint;
            _currentPhase = ConstructionPhase.NotStarted;

            if (_gridManager == null) _gridManager = ServiceLocator.Get<GridManager>();

            Vector3Int siteOrigin = Vector3Int.RoundToInt(transform.position);

            if (blueprint.StructureBlocks != null)
            {
                foreach (var block in blueprint.StructureBlocks)
                {
                    Vector3Int worldPos = siteOrigin + block.LocalPosition;
                    bool success = _gridManager.TryReserve(worldPos, this);
                    if (!success)
                    {
                        Debug.LogWarning($"[ConstructionSite] Conflicto de espacio en {worldPos}. No se pudo reservar.");
                    }
                }
            }
            // --------------------------------

            var structureList = blueprint.StructureBlocks ?? new List<BlueprintBlock>();
            var detailsList = blueprint.Props != null ? ConvertPropsToBlocks(blueprint.Props) : new List<BlueprintBlock>();

            var sortedStructure = structureList
                .OrderBy(b => b.LocalPosition.y)
                .ThenBy(b => b.LocalPosition.x)
                .ThenBy(b => b.LocalPosition.z)
                .ToList();

            _pendingStructure = new Queue<BlueprintBlock>(sortedStructure);

            _pendingDetails = new Queue<BlueprintBlock>(detailsList);

            Debug.Log($"[ConstructionSite] Inicializado '{blueprint.name}'. Estructura: {_pendingStructure.Count}");

            TryAdvancePhase();
        }

        private List<BlueprintBlock> ConvertPropsToBlocks(List<PropEntry> props)
        {
            List<BlueprintBlock> list = new List<BlueprintBlock>();
            foreach (var p in props)
            {
                list.Add(new BlueprintBlock { LocalPosition = Vector3Int.RoundToInt(p.LocalPosition), Type = BlockType.Air });
            }
            return list;
        }

        private void ReleaseGridReservations()
        {
            if (_gridManager == null || _blueprint == null || _blueprint.StructureBlocks == null) return;

            Vector3Int siteOrigin = Vector3Int.RoundToInt(transform.position);
            foreach (var block in _blueprint.StructureBlocks)
            {
                Vector3Int worldPos = siteOrigin + block.LocalPosition;
                _gridManager.ClearReservation(worldPos, this);
            }
        }

        private void TryAdvancePhase()
        {
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

            if (_requiredWorkerClass == null)
            {
                Debug.LogError($"[ConstructionSite] ¡Falta asignar la '_requiredWorkerClass' en el inspector de {name}!");
                return;
            }

            JobRequest newJob = new JobRequest(
                "Construir " + _blueprint.name,
                JobType.Build,
                transform.position,
                this.transform,
                1,
                _requiredWorkerClass
            );

            _activeJobs.Add(newJob);
            _jobManager.PostJob(newJob);
        }

        public void ClearAllJobs()
        {
            var jobsToCancel = new List<JobRequest>(_activeJobs);
            foreach (var job in jobsToCancel)
            {
                if (job != null) job.Cancel();
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
                ConfirmBlockBuilt(block.Value);
                return true;
            }

            return false;
        }

        public bool IsFinishedOrStalled()
        {
            return _currentPhase == ConstructionPhase.Completed;
        }

        // --- MÉTODOS AUXILIARES ---

        public ItemData GetNextRequiredItem()
        {
            BlueprintBlock? next = GetNextBlockToBuild();
            if (next == null) return null;
            return GetItemForBlock(next.Value.Type);
        }

        public bool ConstructBlock(UnitInventory workerInventory)
        {
            BlueprintBlock? block = GetNextBlockToBuild();
            if (block == null) return false;

            ItemData itemCost = GetItemForBlock(block.Value.Type);

            // 1. Validamos si el trabajador tiene el material
            if (itemCost != null)
            {
                if (!workerInventory.HasItem(itemCost, 1)) return false;

                // 2. Consumimos del trabajador
                workerInventory.Remove(itemCost, 1);
            }

            // 3. Ejecutamos la construcción
            ConfirmBlockBuilt(block.Value);
            return true;
        }

        private BlueprintBlock? GetNextBlockToBuild()
        {
            if (_currentPhase == ConstructionPhase.Phase1_Structure && _pendingStructure.Count > 0) return _pendingStructure.Peek();
            if (_currentPhase == ConstructionPhase.Phase2_Details && _pendingDetails.Count > 0) return _pendingDetails.Peek();
            return null;
        }

        private void ConfirmBlockBuilt(BlueprintBlock block)
        {
            if (_currentPhase == ConstructionPhase.Phase1_Structure && _pendingStructure.Count > 0) _pendingStructure.Dequeue();
            else if (_currentPhase == ConstructionPhase.Phase2_Details && _pendingDetails.Count > 0) _pendingDetails.Dequeue();

            if (_currentPhase == ConstructionPhase.Phase1_Structure)
            {
                Vector3Int siteOrigin = Vector3Int.RoundToInt(transform.position);
                Vector3Int worldPos = siteOrigin + block.LocalPosition;

                if (_gridManager != null)
                {
                    _gridManager.SetNode(worldPos.x, worldPos.y, worldPos.z, NodeType.Solid);

                }
            }
            // -------------------------------------

            TryAdvancePhase();
        }

        private void FinishConstruction()
        {
            _currentPhase = ConstructionPhase.Completed;
            ClearAllJobs();
            ReleaseGridReservations(); 

            if (_scaffolding != null) _scaffolding.ClearScaffolds();

            Debug.Log("✨ CONSTRUCCIÓN COMPLETADA ✨");
            if (_completionParticles != null) Instantiate(_completionParticles, transform.position, Quaternion.identity);
        }

        private ItemData GetItemForBlock(BlockType blockType)
        {
            var mapping = _blockCosts.FirstOrDefault(x => x.Block == blockType);
            return mapping.Item;
        }

        private void CreateScaffoldJob(Vector3 pos)
        {
            if (_jobManager == null) return;
            if (_requiredWorkerClass == null) return;

            JobRequest scaffoldJob = new JobRequest(
                "Construir Andamio",
                JobType.Build,
                pos, 
                this.transform,
                1,
                _requiredWorkerClass
            );
            _activeJobs.Add(scaffoldJob);
            _jobManager.PostJob(scaffoldJob);
        }

        private bool JobExistsAt(Vector3 pos)
        {
            return _activeJobs.Exists(j => Vector3.Distance(j.Position, pos) < 0.5f);
        }

        private CityInventory Inventory
        {
            get
            {
                if (_inventoryInstance == null) _inventoryInstance = ServiceLocator.Get<CityInventory>();
                return _inventoryInstance;
            }
        }
    }
}