using Homebound.Core;
using Homebound.Core.Inputs;
using Homebound.Features.AethianAI;
using Homebound.Features.Navigation;
using Homebound.Features.TaskSystem;
using Homebound.Features.PlayerInteraction.Tools;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Homebound.Features.PlayerInteraction
{
    public class InteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _selectionGhost;

        [Header("Debug Spawner")]
        [SerializeField] private GameObject _malePrefab;
        [SerializeField] private GameObject _femalePrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _resourceLayer;
        [SerializeField] private LayerMask _unitLayer;

        [Header("Requisitos de Trabajo")]
        [SerializeField] private UnitClassDefinition _requiredWorkerClass;

        // ESTADO INTERNO
        private IInteractionTool _currentTool;

        // Estado temporal para mantener referencias
        private RTSInputs _input;
        private Vector3 _currentGridPos;
        private bool _isValidHover;

        public event Action<AethianBot> OnUnitSelected;

        private void Awake()
        {
            _input = new RTSInputs();
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void Start()
        {
            // Default tool
            SetTool(new InspectionTool(this, _mainCamera, _unitLayer));
        }

        private void OnEnable()
        {
            _input.Enable();
            // Delegamos clicks a la herramienta activa en Update, o usamos eventos aquí si es necesario
            // Por ahora InspectionTool usa Input.GetMouseButtonDown, pero AreaTool usa eso + Drag.
            // Para consistencia y no reescribir todo el input handling, dejamos que Update llame a la Tool.
        }

        private void OnDisable()
        {
            _input.Disable();
        }

        private void Update()
        {
            // Hover logic global (para debug/info)
            HandleGlobalRaycast();

            // Tool Logic
            if (_currentTool != null)
            {
                _currentTool.UpdateTool();
            }
        }

        public void SetTool(IInteractionTool newTool)
        {
            if (_currentTool != null)
            {
                _currentTool.ExitTool();
            }

            _currentTool = newTool;

            if (_currentTool != null)
            {
                _currentTool.EnterTool();
            }
        }

        // --- API PÚBLICA (UI) ---

        public void SetMiningTool()
        {
            SetTool(new MiningAreaTool(this, _groundLayer, _requiredWorkerClass));
        }

        public void SetInspectionTool()
        {
            SetTool(new InspectionTool(this, _mainCamera, _unitLayer));
        }

        public void SelectUnit(AethianBot bot)
        {
            OnUnitSelected?.Invoke(bot);
            if (bot != null) Debug.Log($"[Interaction] Unidad seleccionada: {bot.name}");
        }

        // --- UTILIDADES ---

        private void HandleGlobalRaycast()
        {
            // Solo para mantener _currentGridPos actualizado por si alguna herramienta lo necesita
            // o para debug visual.
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                _currentGridPos = new Vector3(
                    Mathf.Floor(hit.point.x) + 0.5f,
                    Mathf.Floor(hit.point.y) + 0.5f,
                    Mathf.Floor(hit.point.z) + 0.5f
                );
                _isValidHover = true;
            }
            else
            {
                _isValidHover = false;
            }
        }

        public void SpawnDebugUnit()
        {
            if (!_isValidHover) return;

            GameObject prefabToSpawn = (UnityEngine.Random.value > 0.5f) ? _malePrefab : _femalePrefab;

            if (prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, _currentGridPos, Quaternion.identity);
            }
        }
    }
}
