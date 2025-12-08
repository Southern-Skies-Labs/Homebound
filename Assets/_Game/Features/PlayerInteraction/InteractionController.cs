using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Homebound.Core.Inputs;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy;
using Homebound.Features.AethianAI;
using Homebound.Features.Navigation;

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

        [Header("Layers")] 
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _resourceLayer;
        [SerializeField] private LayerMask _unitLayer;

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

            // Validar objetivo según el tipo de trabajo
            Transform targetTransform = null;
            Vector3 targetPos = _currentGridPos;
            string jobName = $"{_pendingJobType} Order";

            // Lógica específica por tipo de trabajo
            if (_pendingJobType == JobType.Chop)
            {
                // Para TALAR, necesitamos un Recurso válido bajo el mouse
                if (TryGetResourceUnderMouse(out var resource))
                {
                    targetTransform = resource.Transform;
                    targetPos = resource.Position;
                    jobName = $"Talar {resource.Name}";
                }
                else
                {
                    Debug.LogWarning("[Interaction] Debes hacer clic en un recurso para Talar.");
                    return; // No consumimos el click si fue inválido
                }
            }
            else if (_pendingJobType == JobType.Move)
            {
                // Para MOVER, solo necesitamos suelo (ya validado por HandleRaycast)
                jobName = "Moverse";
            }

            // Crear y postear la tarea
            var job = new JobRequest(
                jobName, 
                _pendingJobType, 
                targetPos, 
                targetTransform, 
                50, // Prioridad estándar (usuario)
                UnitClass.Villager
            );
            
            _jobManager.PostJob(job);
            
            // Consumir el modo (volver a normal) tras dar la orden
            // (En RTS clásicos a veces se mantiene con Shift, por ahora simple: 1 click = 1 orden)
            _currentMode = InputMode.Normal;
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
    }
}