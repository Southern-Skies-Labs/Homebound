using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Homebound.Core.Inputs;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy;
using Homebound.Features.AethianAI;

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
        private Vector3 _currentGridPos; // Ahora guardará la Y real
        private bool _isValidHover;
        
        private void Awake()
        {
            _input = new RTSInputs();
            if (_mainCamera == null) _mainCamera = Camera.main;
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
                // --- FIX: Altura Dinámica ---
                // Redondeamos X y Z para la grilla, pero tomamos la Y del impacto
                int x = Mathf.RoundToInt(hit.point.x);
                int z = Mathf.RoundToInt(hit.point.z);
                
                // Para la altura, usamos Mathf.Ceil para asegurarnos de estar "encima" del bloque
                // o hit.point.y si queremos precisión decimal.
                // Usaremos Ceil para que el ghost se pose sobre el vóxel.
                float y = Mathf.Ceil(hit.point.y);

                _currentGridPos = new Vector3(x, y, z);
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
            if (jobManager == null) return;
            
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
                    var job = new JobRequest($"Talar {gatherable.Name}", JobType.Chop, gatherable.GetPosition(), gatherable.Transform, 50);
                    jobManager.PostJob(job);
                    OnUnitSelected?.Invoke(null);
                    Debug.Log($"[Interaction] Árbol marcado: {gatherable.Name}");
                    return; 
                }
            }

            // PRIORIDAD 2: SUELO (MOVIMIENTO)
            if (_isValidHover)
            {
                OnUnitSelected?.Invoke(null);
                
                // --- FIX: Usar la posición calculada en HandleRaycast ---
                // Ya contiene la altura correcta (Y) gracias al fix de arriba.
                Vector3 targetPos = _currentGridPos; 

                var job = new JobRequest("Ir a Posición", JobType.Haul, targetPos, null, 10);
                jobManager.PostJob(job);
                
                Debug.Log($"[Interaction] Tarea de movimiento a {targetPos}");
            }
        }

        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            if (!_isValidHover || _aethianPrefab == null) return;

            // --- FIX: Spawn con altura correcta ---
            // Usamos la posición del ghost que ya está ajustada a la altura del terreno
            Vector3 spawnPos = _currentGridPos;
            
            // Opcional: +0.5f o +1.0f en Y para asegurar que no nazca atascado si el pivote está en los pies
            // Si el pivote es pies, _currentGridPos (que usa Ceil) debería ser seguro.
            
            Instantiate(_aethianPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"[Interaction] Aethian creado en {spawnPos}");
        }
    }
}