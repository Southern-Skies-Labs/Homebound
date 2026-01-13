using System;
using UnityEngine;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.VoxelWorld;
using Homebound.Features.AethianAI; // Necesario para referenciar AethianBot
using UnityEngine.EventSystems;

namespace Homebound.Features.PlayerInteraction
{
    // Definimos los modos de interacción posibles
    public enum CommandMode
    {
        Select = 0,
        MineSingle = 1,
        MineArea = 2,
        Build = 3 // Reservado para futuro
    }

    public class InteractionController : MonoBehaviour
    {
        // --- EVENTOS QUE ESCUCHA LA UI ---
        // Esto soluciona el error en UnitDetailsPanel
        public event Action<AethianBot> OnUnitSelected;

        [Header("Tools & Visuals")]
        [SerializeField] private GameObject _ghostCursorPrefab;
        [SerializeField] private LayerMask _terrainLayer;
        [SerializeField] private LayerMask _unitLayer; // <--- NUEVO: Para detectar NPCs

        [Header("Visual Adjustments")]
        [SerializeField] private bool _useCenterPivot = true;
        [SerializeField] private Vector3 _cursorOffset = Vector3.zero;

        [Header("Settings")]
        [SerializeField] private float _rayDistance = 100f;

        // Estado Interno
        private CommandMode _currentMode = CommandMode.Select;
        private GameObject _ghostCursorInstance;
        private Camera _mainCamera;

        // Dragging Data (Para Área)
        private bool _isDragging = false;
        private Vector3Int _startDragPos;
        private Vector3Int _currentDragPos;

        private void Awake()
        {
            // Registrar en ServiceLocator si es necesario para que otros lo encuentren
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<InteractionController>();
        }

        private void Start()
        {
            _mainCamera = Camera.main;

            if (_ghostCursorPrefab != null)
            {
                _ghostCursorInstance = Instantiate(_ghostCursorPrefab);
                _ghostCursorInstance.SetActive(false);
                if (_ghostCursorInstance.TryGetComponent(out Collider c)) Destroy(c);
            }
        }

        private void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            HandleInput();
            UpdateVisuals();
        }

        // --- API PÚBLICA (Soluciona errores de CommandHUD) ---

        public void SetCommandMode(int modeIndex)
        {
            // Conversión segura de int a Enum
            _currentMode = (CommandMode)Mathf.Clamp(modeIndex, 0, 3);
            Debug.Log($"[Interaction] Modo cambiado a: {_currentMode}");

            // Resetear estados al cambiar de modo
            _isDragging = false;
            if (_ghostCursorInstance) _ghostCursorInstance.SetActive(false);
        }

        // Sobrecarga para usar con Enum directamente si se prefiere
        public void SetCommandMode(CommandMode mode)
        {
            _currentMode = mode;
        }

        // Métodos específicos para tus botones anteriores (Backwards Compatibility)
        public void SetMiningModeSingle() => SetCommandMode(CommandMode.MineSingle);
        public void SetMiningModeArea() => SetCommandMode(CommandMode.MineArea);

        // ----------------------------------------------------

        private void HandleInput()
        {
            // 1. PRIORIDAD: Selección de Unidades (Solo en modo Select o cualquier modo si es click simple)
            if (Input.GetMouseButtonDown(0) && !_isDragging)
            {
                if (TrySelectUnit()) return; // Si seleccionamos un NPC, no hacemos nada más (no minamos)
            }

            // 2. Obtener posición del terreno
            if (!GetMouseWorldPosition(out Vector3Int gridPos))
            {
                if (!_isDragging && _ghostCursorInstance) _ghostCursorInstance.SetActive(false);
                if (Input.GetMouseButtonUp(0)) _isDragging = false;
                return;
            }

            _currentDragPos = gridPos;

            // 3. Lógica según Modo
            if (Input.GetMouseButtonDown(0))
            {
                switch (_currentMode)
                {
                    case CommandMode.MineSingle:
                        RequestMiningJob(gridPos);
                        break;
                    case CommandMode.MineArea:
                        _isDragging = true;
                        _startDragPos = gridPos;
                        break;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_currentMode == CommandMode.MineArea && _isDragging)
                {
                    _isDragging = false;
                    RequestAreaMining(_startDragPos, _currentDragPos);
                }
            }
        }

        private bool TrySelectUnit()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _unitLayer))
            {
                AethianBot bot = hit.collider.GetComponentInParent<AethianBot>();
                if (bot != null)
                {
                    Debug.Log($"[Interaction] Unidad Seleccionada: {bot.name}");
                    OnUnitSelected?.Invoke(bot); // Notificar a la UI
                    return true;
                }
            }
            return false;
        }

        private void UpdateVisuals()
        {
            // El cursor fantasma solo se muestra en modos de construcción/minería
            bool showCursor = _currentMode == CommandMode.MineSingle || _currentMode == CommandMode.MineArea;

            if (_ghostCursorInstance == null) return;

            if (!showCursor)
            {
                _ghostCursorInstance.SetActive(false);
                return;
            }

            // Si el mouse no toca terreno, ocultar
            if (!_isDragging && !GetMouseWorldPosition(out Vector3Int _))
            {
                _ghostCursorInstance.SetActive(false);
                return;
            }

            _ghostCursorInstance.SetActive(true);

            if (_currentMode == CommandMode.MineArea && _isDragging)
            {
                // Visualización de Área
                Vector3Int min = Vector3Int.Min(_startDragPos, _currentDragPos);
                Vector3Int max = Vector3Int.Max(_startDragPos, _currentDragPos);
                Vector3 size = (Vector3)(max - min + Vector3Int.one);
                Vector3 center = min + (size * 0.5f);

                _ghostCursorInstance.transform.position = center + _cursorOffset;
                _ghostCursorInstance.transform.localScale = size;
            }
            else
            {
                // Visualización Simple
                Vector3 targetPos = _currentDragPos;
                if (_useCenterPivot) targetPos += new Vector3(0.5f, 0.5f, 0.5f);

                _ghostCursorInstance.transform.position = targetPos + _cursorOffset;
                _ghostCursorInstance.transform.localScale = Vector3.one;
            }
        }

        private void RequestAreaMining(Vector3Int start, Vector3Int end)
        {
            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);
            int minY = Mathf.Min(start.y, end.y);
            int maxY = Mathf.Max(start.y, end.y);
            int minZ = Mathf.Min(start.z, end.z);
            int maxZ = Mathf.Max(start.z, end.z);

            var worldData = ServiceLocator.Get<IWorldDataProvider>();
            int count = 0;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);

                        if (worldData != null)
                        {
                            int id = worldData.GetBlockIDAt(pos);
                            if (id == 0 || id == 4) continue; // No minar aire ni agua
                        }
                        RequestMiningJob(pos);
                        count++;
                    }
                }
            }
            Debug.Log($"[Interaction] Área minada: {count} bloques.");
        }

        private void RequestMiningJob(Vector3Int gridPos)
        {
            var jobManager = ServiceLocator.Get<JobManager>();
            if (jobManager != null)
            {
                Vector3 worldPos = gridPos + new Vector3(0.5f, 0.5f, 0.5f);
                jobManager.CreateMiningRequest(worldPos);
            }
        }

        private bool GetMouseWorldPosition(out Vector3Int gridPos)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _terrainLayer))
            {
                Vector3 pointInBlock = hit.point - (hit.normal * 0.01f);
                gridPos = new Vector3Int(
                    Mathf.FloorToInt(pointInBlock.x),
                    Mathf.FloorToInt(pointInBlock.y),
                    Mathf.FloorToInt(pointInBlock.z)
                );
                return true;
            }
            gridPos = Vector3Int.zero;
            return false;
        }
    }
}