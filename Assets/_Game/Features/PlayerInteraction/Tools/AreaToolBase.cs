using UnityEngine;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Navigation;

namespace Homebound.Features.PlayerInteraction.Tools
{
    public abstract class AreaToolBase : IInteractionTool
    {
        protected InteractionController _controller;
        protected bool _isDragging;
        protected Vector3 _startDragPos;
        protected Vector3 _endDragPos;
        protected GameObject _ghostBox; // Visual feedback

        protected LayerMask _groundLayer;

        public AreaToolBase(InteractionController controller, LayerMask groundLayer)
        {
            _controller = controller;
            _groundLayer = groundLayer;
            InitializeGhost();
        }

        private void InitializeGhost()
        {
            // Creamos un cubo simple transparente para feedback
            _ghostBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghostBox.name = "AreaTool_Ghost";
            Object.Destroy(_ghostBox.GetComponent<Collider>());
            var mr = _ghostBox.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default")); // Material simple temporal
            mr.material.color = new Color(0, 1, 0, 0.3f); // Verde semitransparente
            _ghostBox.SetActive(false);
        }

        public virtual void EnterTool()
        {
            _isDragging = false;
            _ghostBox.SetActive(false);
        }

        public virtual void ExitTool()
        {
            _isDragging = false;
            _ghostBox.SetActive(false);
            // Si quisiéramos destruir el ghost al cambiar de herramienta:
            // Object.Destroy(_ghostBox);
        }

        public virtual void UpdateTool()
        {
            // 1. Raycast Mouse
            if (!GetMouseGridPosition(out Vector3 currentPos)) return;

            // 2. Input Logic
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _startDragPos = currentPos;
                _ghostBox.SetActive(true);
            }

            if (_isDragging)
            {
                _endDragPos = currentPos;
                UpdateGhostVisuals();

                if (Input.GetMouseButtonUp(0))
                {
                    _isDragging = false;
                    _ghostBox.SetActive(false);
                    ExecuteSelection(_startDragPos, _endDragPos);
                }
            }
        }

        public virtual void OnDrawGizmos() { }

        // Lógica abstracta a implementar por herramientas específicas
        protected abstract void ExecuteSelection(Vector3 start, Vector3 end);

        protected void UpdateGhostVisuals()
        {
            Vector3 center = (_startDragPos + _endDragPos) / 2f;
            // Añadimos 1 unidad al tamaño porque seleccionamos celdas enteras
            // Si start=0 y end=0, tamaño=1. Si start=0 y end=1, tamaño=2.
            Vector3 size = new Vector3(
                Mathf.Abs(_startDragPos.x - _endDragPos.x) + 1,
                Mathf.Abs(_startDragPos.y - _endDragPos.y) + 1,
                Mathf.Abs(_startDragPos.z - _endDragPos.z) + 1
            );

            // Ajuste visual para que el cubo encaje en el grid
            // Los cubos de Unity tienen pivote en el centro
            _ghostBox.transform.position = center; // center ya está en coords 0.5, 1.5, etc si el grid lo devuelve
            _ghostBox.transform.localScale = size;
        }

        private bool GetMouseGridPosition(out Vector3 pos)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                // Queremos el centro del bloque
                Vector3 p = hit.point + (ray.direction * 0.1f);
                pos = new Vector3(
                    Mathf.Floor(p.x) + 0.5f,
                    Mathf.Floor(p.y) + 0.5f,
                    Mathf.Floor(p.z) + 0.5f
                );
                return true;
            }
            pos = Vector3.zero;
            return false;
        }
    }
}
