using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Homebound.Core.Inputs;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy;
using Homebound.Features.AethianAI;
using Homebound.Features.Navigation; // Necesario

namespace Homebound.Features.PlayerInteraction
{
    public class InteractionController : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _selectionGhost;
        [SerializeField] private GameObject _aethianPrefab;

        [Header("Layers")] 
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _resourceLayer;
        [SerializeField] private LayerMask _unitLayer;

        public event Action<AethianBot> OnUnitSelected; 
        private RTSInputs _input;
        private Vector3 _currentGridPos; 
        private bool _isValidHover;
        
        private GridManager _gridManager; // Referencia al Grid

        private void Awake()
        {
            _input = new RTSInputs();
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
        }

        private void OnEnable()
        {
            _input.Enable();
            _input.Gameplay.Select.performed += OnSelectPerformed;
            _input.Gameplay.Spawn.performed += OnSpawnPerformed;
        }

        private void OnDisable()
        {
            _input.Disable();
            _input.Gameplay.Select.performed -= OnSelectPerformed;
            _input.Gameplay.Spawn.performed -= OnSpawnPerformed;
        }

        private void Update()
        {
            HandleRaycast();
        }
        
        private void HandleRaycast()
        {
            Vector2 mouseScreenPos = _input.Gameplay.Point.ReadValue<Vector2>();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                // 1. Coordenada Base (El bloque que tocamos)
                int x = Mathf.RoundToInt(hit.point.x);
                int z = Mathf.RoundToInt(hit.point.z);
                int yRaw = Mathf.RoundToInt(hit.point.y); // Altura aproximada del impacto

                // 2. Corrección de Altura Inteligente
                // Buscamos cuál es la superficie caminable real en esta columna (x, z)
                // Probamos desde un poco abajo hasta un poco arriba del impacto
                float finalY = yRaw + 1; // Por defecto asumimos "Encima del bloque"

                if (_gridManager != null)
                {
                    // Buscamos el nodo verde (WalkableSurface) más cercano verticalmente
                    for (int yOffset = -2; yOffset <= 2; yOffset++)
                    {
                        int checkY = yRaw + yOffset;
                        PathNode node = _gridManager.GetNode(x, checkY, z);
                        
                        if (node != null && node.IsWalkableSurface)
                        {
                            finalY = checkY; // ¡Encontramos el nodo verde!
                            break;
                        }
                    }
                }

                _currentGridPos = new Vector3(x, finalY, z);
                _isValidHover = true;
                
                if (_selectionGhost != null)
                {
                    _selectionGhost.gameObject.SetActive(true);
                    _selectionGhost.position = _currentGridPos;
                }
            }
            else
            {
                _isValidHover = false;
                if (_selectionGhost != null) _selectionGhost.gameObject.SetActive(false);
            }
        }
        
        private void OnSelectPerformed(InputAction.CallbackContext context)
        {
            
            var jobManager = ServiceLocator.Get<JobManager>();
            if (jobManager == null) 
            {
                // Si no hay sistema de tareas, no hacemos nada (evita el error rojo)
                // Debug.LogWarning("JobManager no encontrado al hacer clic.");
                return; 
            }
            
            Vector2 mouseScreenPos = _input.Gameplay.Point.ReadValue<Vector2>();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            // PRIORIDAD 0: UNIDADES
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
            
            // PRIORIDAD 1: RECURSOS
            if (Physics.Raycast(ray, out RaycastHit resourceHit, 1000f, _resourceLayer))
            {
                var gatherable = resourceHit.collider.GetComponentInParent<IGatherable>();
                if (gatherable != null)
                {
                    var job = new JobRequest(
                        $"Talar {gatherable.Name}", 
                        JobType.Chop, 
                        gatherable.GetPosition(), 
                        gatherable.Transform, 
                        50, 
                        UnitClass.Villager // <--- NUEVO
                    );
                    jobManager.PostJob(job);
                    OnUnitSelected?.Invoke(null);
                    return; 
                }
            }

            // PRIORIDAD 2: SUELO (MOVIMIENTO)
            if (_isValidHover)
            {
                var gatherable = resourceHit.collider.GetComponentInParent<IGatherable>();
                OnUnitSelected?.Invoke(null);
                
                // Usamos la posición corregida (Grid)
                Vector3 targetPos = _currentGridPos; 

                var job = new JobRequest(
                    $"Talar {gatherable.Name}", 
                    JobType.Chop, 
                    gatherable.GetPosition(), 
                    gatherable.Transform, 
                    50, 
                    UnitClass.Villager // <--- NUEVO
                );
                jobManager.PostJob(job);
                
                Debug.Log($"[Interaction] Orden de movimiento a {targetPos}");
            }
        }

        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            if (!_isValidHover || _aethianPrefab == null) return;
            Instantiate(_aethianPrefab, _currentGridPos, Quaternion.identity);
        }
    }
}