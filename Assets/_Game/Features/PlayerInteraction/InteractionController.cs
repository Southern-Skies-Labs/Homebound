using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Homebound.Core.Inputs;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy;
using Homebound.Features.AethianAI;
using Homebound.Features.Navigation;
using UnityEngine.Serialization;

namespace Homebound.Features.PlayerInteraction
{
    public class InteractionController : MonoBehaviour
    {
        // ESTADOS DE INPUT
        private enum InputMode
        {
            Normal,         
            CommandPending 
        }

        [Header("References")] 
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _selectionGhost;
        [SerializeField] private GameObject _aethianPrefab;

        [FormerlySerializedAs("_terrainLayer")]
        [Header("Layers")] 
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _resourceLayer;
        [SerializeField] private LayerMask _unitLayer;

        [Header("Requisitos de Trabajo")]
        [Tooltip("Arrastra aquí el asset 'Villager_Data'")]
        [SerializeField] private UnitClassDefinition _requiredWorkerClass;

        [Header("Tools")] 
        [SerializeField] private bool _isMiningMode = false;

        // ESTADO INTERNO
        private RTSInputs _input;
        private Vector3 _currentGridPos; 
        private bool _isValidHover;
        
        private InputMode _currentMode = InputMode.Normal;
        private JobType _pendingJobType; 

        private GridManager _gridManager;
        private JobManager _jobManager;

        public event Action<AethianBot> OnUnitSelected; 

        private void Awake()
        {
            _input = new RTSInputs();
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _jobManager = ServiceLocator.Get<JobManager>();
        }

        private void OnEnable()
        {
            _input.Enable();
            _input.Gameplay.Select.performed += OnLeftClick; 
            _input.Gameplay.Spawn.performed += OnRightClick; 
        }

        private void OnDisable()
        {
            _input.Disable();
            _input.Gameplay.Select.performed -= OnLeftClick;
            _input.Gameplay.Spawn.performed -= OnRightClick;
        }

        private void Update()
        {
            HandleRaycast();
            UpdateVisuals();
            
            if (Input.GetMouseButtonDown(0) && _isMiningMode) // Click Izquierdo
            {
                HandleMiningClick();
            }
        }

        // --- API PÚBLICA (Llamado desde UI) ---
        public void SetCommandMode(JobType jobType)
        {
            _currentMode = InputMode.CommandPending;
            _pendingJobType = jobType;
            
            // Feedback visual opcional: Cambiar cursor, color del ghost, etc.
            if (_selectionGhost != null) 
            {
                // Ejemplo: Podrías cambiar el material del ghost aquí según el jobType
            }
        }

        public void CancelCommandMode()
        {
            _currentMode = InputMode.Normal;
            Debug.Log("[Interaction] Modo comando cancelado.");
        }
        

        // --- LÓGICA DE INPUT ---

        private void OnLeftClick(InputAction.CallbackContext context)
        {
            if (!_isValidHover) return;

            switch (_currentMode)
            {
                case InputMode.Normal:
                    HandleNormalSelection();
                    break;

                case InputMode.CommandPending:
                    ExecutePendingCommand();
                    break;
            }
        }

        private void OnRightClick(InputAction.CallbackContext context)
        {
            // CLICK DERECHO: Lógica de Cancelación o Acción Secundaria
            
            if (_currentMode == InputMode.CommandPending)
            {
                // Si estamos preparando una orden, el click derecho CANCELA
                CancelCommandMode();
            }
            else
            {
                // Si estamos en modo normal, mantenemos tu lógica de Debug (Spawnear)
                // Esto se eliminará en producción, pero es útil ahora.
                SpawnDebugUnit();
            }
        }

        // --- MÉTODOS DE ACCIÓN ---

        private void HandleNormalSelection()
        {
            Vector2 mouseScreenPos = _input.Gameplay.Point.ReadValue<Vector2>();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            // 1. Intentar seleccionar Unidad
            if (Physics.Raycast(ray, out RaycastHit unitHit, 1000f, _unitLayer))
            {
                var bot = unitHit.collider.GetComponentInParent<AethianBot>();
                if (bot != null)
                {
                    Debug.Log($"[Interaction] Unidad seleccionada: {bot.Stats.CharacterName}");
                    OnUnitSelected?.Invoke(bot);
                    return;
                }
            }

            // 2. Si no, deseleccionar
            OnUnitSelected?.Invoke(null);
        }

        private void ExecutePendingCommand()
        {
            if (_jobManager == null) return;

            Transform targetTransform = null;
            Vector3 targetPos = _currentGridPos;
            string jobName = $"{_pendingJobType} Order";
            bool validCommand = true; // Flag para saber si procedemos

            // --- LÓGICA POR TIPO ---
            
            // CASO 1: TALAR (Busca entidad IGatherable)
            if (_pendingJobType == JobType.Chop)
            {
                if (TryGetResourceUnderMouse(out var resource))
                {
                    targetTransform = resource.Transform;
                    targetPos = resource.Position;
                    jobName = $"Talar {resource.Name}";
                }
                else
                {
                    Debug.LogWarning("[Interaction] Debes hacer clic en un recurso.");
                    validCommand = false; 
                }
            }
            // CASO 2: MINAR (Busca el Voxel exacto)
            else if (_pendingJobType == JobType.Mine)
            {
                Ray ray = _mainCamera.ScreenPointToRay(_input.Gameplay.Point.ReadValue<Vector2>());
                
                // CAMBIO 1: Usamos RaycastAll para atravesar al bot si se interpone
                RaycastHit[] hits = Physics.RaycastAll(ray, 100f, _groundLayer);
                
                // Buscamos el hit más cercano que NO sea una unidad
                RaycastHit validHit = new RaycastHit();
                bool found = false;
                float minDistance = float.MaxValue;

                foreach (var hit in hits)
                {
                    // Filtro de seguridad: Si golpeamos algo que tiene UnitMovementController o AethianBot, lo ignoramos
                    if (hit.collider.GetComponentInParent<UnitMovementController>() != null) continue;
                    if (hit.collider.isTrigger) continue;

                    if (hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        validHit = hit;
                        found = true;
                    }
                }

                if (found)
                {
                    // CAMBIO 2: Empujamos un poco más fuerte hacia adentro (0.2f)
                    Vector3 pointInBlock = validHit.point + (ray.direction * 0.2f);
                    
                    targetPos = new Vector3(
                        Mathf.Floor(pointInBlock.x),
                        Mathf.Floor(pointInBlock.y),
                        Mathf.Floor(pointInBlock.z)
                    );
                    
                    // DEBUG CRÍTICO: ¿Qué golpeamos y dónde quedó el target?
                    Debug.Log($"[Interaction] Raycast golpeó: {validHit.collider.name} en {validHit.point}. Target Calculado: {targetPos}");

                    jobName = "Minar Piedra";
                }
                else
                {
                    Debug.LogWarning("[Interaction] Raycast de minería no encontró terreno válido (¿Bloqueado por el bot?).");
                    validCommand = false;
                }
            }

            // --- EJECUCIÓN ---
            
            if (validCommand)
            {
                var job = new JobRequest(
                    jobName, 
                    _pendingJobType, 
                    targetPos, 
                    targetTransform, 
                    50,
                    _requiredWorkerClass
                );
                
                _jobManager.PostJob(job);
                
                // Feedback
                Debug.Log($"[Interaction] Comando '{jobName}' enviado en {targetPos}");
                
                // Salir del modo comando
                _currentMode = InputMode.Normal;
                CancelCommandMode(); // Limpia visuales si las hubiera
            }
        }

        private void SpawnDebugUnit()
        {
            if (!_isValidHover || _aethianPrefab == null) return;
            Instantiate(_aethianPrefab, _currentGridPos, Quaternion.identity);
            Debug.Log("[Interaction] Unidad de prueba spawneada.");
        }

        // --- UTILIDADES ---

        private bool TryGetResourceUnderMouse(out IGatherable resource)
        {
            Vector2 mouseScreenPos = _input.Gameplay.Point.ReadValue<Vector2>();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _resourceLayer))
            {
                resource = hit.collider.GetComponentInParent<IGatherable>();
                return resource != null;
            }
            resource = null;
            return false;
        }

        private void HandleRaycast()
        {
            Vector2 mouseScreenPos = _input.Gameplay.Point.ReadValue<Vector2>();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                int x = Mathf.RoundToInt(hit.point.x);
                int z = Mathf.RoundToInt(hit.point.z);
                int yRaw = Mathf.RoundToInt(hit.point.y);

                float finalY = yRaw + 1; 

                if (_gridManager != null)
                {
                    for (int yOffset = -2; yOffset <= 2; yOffset++)
                    {
                        int checkY = yRaw + yOffset;
                        PathNode node = _gridManager.GetNode(x, checkY, z);
                        if (node != null && node.IsWalkableSurface)
                        {
                            finalY = checkY;
                            break;
                        }
                    }
                }

                _currentGridPos = new Vector3(x, finalY, z);
                _isValidHover = true;
            }
            else
            {
                _isValidHover = false;
            }
        }

        private void UpdateVisuals()
        {
            if (_selectionGhost != null)
            {
                _selectionGhost.gameObject.SetActive(_isValidHover);
                if (_isValidHover)
                {
                    _selectionGhost.position = _currentGridPos;
                    // Aquí podrías cambiar el color del ghost si _currentMode == CommandPending
                }
            }
        }
        
        public void SetMiningMode(bool active)
        {
            _isMiningMode = active;
            // Desactivar otros modos si es necesario
        }
        private void HandleMiningClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayer))
            {
                // TRUCO MATEMÁTICO DE VOXELS:
                // Para saber qué bloque estamos mirando, nos movemos un poquito *dentro* del bloque
                // siguiendo la dirección del rayo.
                // Si golpeamos la cara Norte, queremos el bloque que está "dentro" de esa cara.
                Vector3 pointInBlock = hit.point + (ray.direction * 0.1f);

                int x = Mathf.RoundToInt(pointInBlock.x);
                int y = Mathf.RoundToInt(pointInBlock.y);
                int z = Mathf.RoundToInt(pointInBlock.z);

                // Crear el trabajo de minería en esa coordenada
                CreateMiningJob(new Vector3Int(x, y, z));
            }
        }
        
        private void CreateMiningJob(Vector3Int pos)
        {
            var jobManager = ServiceLocator.Get<JobManager>();
            
            // Creamos un Job en la posición del bloque
            // Nota: El bot debe pararse *al lado* o *arriba*, no dentro.
            // El JobDefinition se encargará de la distancia de interacción.
            
            JobRequest miningJob = new JobRequest(
                "Mine Stone",                      
                JobType.Mine,                      
                new Vector3(pos.x, pos.y, pos.z),  
                null,                              
                50,
                _requiredWorkerClass
            );

            jobManager.PostJob(miningJob);
            
            // Feedback Visual (Opcional): Instanciar un marcador rojo en 'pos'
            Debug.Log($"[Interaction] Orden de minar creada en {pos}");
        }
    }
}