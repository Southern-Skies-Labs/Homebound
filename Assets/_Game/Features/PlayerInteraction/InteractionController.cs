using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Homebound.Core.Inputs;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy;


namespace Homebound.Features.PlayerInteraction
{
    
    public class InteractionController : MonoBehaviour
    {
        //Variables
        [Header("References")] 
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _selectionGhost;
        [SerializeField] private GameObject _aethianPrefab;

        [Header("Layers")] 
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _resourceLayer;
        
        
        private RTSInputs _input;
        private Vector3 _currentGridPos;
        private bool _isValidHover;
        
        
        //Metodos
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
            //Obtenemos posición dle mouse
            Vector2 mouseScreenPos = _input.Gameplay.Point.ReadValue<Vector2>();
            
            //Lanzamos el rayo
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                //Calculamos la posición del gird
                int x = Mathf.RoundToInt(hit.point.x);
                int z = Mathf.RoundToInt(hit.point.z);
                
                //Se asume que y=0 o y=1
                _currentGridPos = new Vector3(x, 1.0f, z);
                _isValidHover = true;
                
                //Movemos el ghost
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

            // PRIORIDAD 1
            if (Physics.Raycast(ray, out RaycastHit resourceHit, 1000f, _resourceLayer))
            {
                // Intentamos obtener el componente del padre o del objeto golpeado
                var gatherable = resourceHit.collider.GetComponentInParent<IGatherable>();
                
                if (gatherable != null)
                {
                    // Crear tarea de recolección
                    var job = new JobRequest($"Talar {gatherable.Name}", JobType.Chop, gatherable.GetPosition(), gatherable.Transform, 50);
                    jobManager.PostJob(job);
                    
                    Debug.Log($"[Interaction] Árbol marcado para talar: {gatherable.Name}");
                    return; 
                }
            }

            // PRIORIDAD 2
            if (_isValidHover)
            {
                Vector3 targetPos = new Vector3(_currentGridPos.x, 1.2f, _currentGridPos.z);
                var job = new JobRequest("Ir a Posición", JobType.Haul, targetPos, null, 10); // Bajamos prioridad de mover a 10
                jobManager.PostJob(job);
                Debug.Log($"[Interaction] Tarea de movimiento creada.");
            }
        }
  

        

        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            if (!_isValidHover || _aethianPrefab == null) return;

            Vector3 spawnPos = new Vector3(_currentGridPos.x, 2.0f, _currentGridPos.z);
            Instantiate(_aethianPrefab, spawnPos, Quaternion.identity);
            
            Debug.Log($"[Interaction] Aethian creado en {spawnPos}");
        }
        
    }

}