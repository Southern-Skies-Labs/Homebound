using UnityEngine;
using Homebound.Core;
// using TMPro; // Opcional si usas texto en pantalla, pero usaremos Gizmos y Console

namespace Homebound.Features.Navigation
{
    public class GridDebugger : MonoBehaviour
    {
        private GridManager _gridManager;
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _walkableColor = Color.green;
        [SerializeField] private Color _blockedColor = Color.red;

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos || Application.isPlaying == false) return;
            if (_gridManager == null) _gridManager = ServiceLocator.Get<GridManager>();
            if (_gridManager == null) return;

            // Rayo desde la cámara del editor o juego
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                // 1. Dibujar dónde pegó el rayo físico
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(hit.point, 0.1f);

                // 2. Calcular qué nodo es ese
                int x = Mathf.RoundToInt(hit.point.x);
                int z = Mathf.RoundToInt(hit.point.z);
                // Importante: Chequeamos varias alturas para ver qué hay
                int yBase = Mathf.RoundToInt(hit.point.y);

                for (int y = yBase - 2; y <= yBase + 2; y++)
                {
                    PathNode node = _gridManager.GetNode(x, y, z);
                    if (node != null)
                    {
                        // Dibujar el nodo
                        Gizmos.color = node.IsWalkableSurface ? _walkableColor : _blockedColor;
                        if (node.Type == NodeType.Solid) Gizmos.color = Color.black; // Muro
                        
                        // Cubo alámbrico
                        Vector3 center = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        Gizmos.DrawWireCube(center, Vector3.one * 0.9f);

                        // Si es el nodo bajo el mouse exacto, dibujarlo relleno
                        if (y == yBase)
                        {
                            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                            Gizmos.DrawCube(center, Vector3.one * 0.9f);
                        }
                    }
                }
            }
        }
        
        // Método para imprimir info en consola al hacer clic con rueda central (por ejemplo)
        private void Update()
        {
            if (Input.GetMouseButtonDown(2)) // Click Rueda
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    int x = Mathf.RoundToInt(hit.point.x);
                    int y = Mathf.RoundToInt(hit.point.y); // Ojo aquí, puede ser Ceil
                    int z = Mathf.RoundToInt(hit.point.z);
                    
                    // Chequeamos el nodo exacto y el de arriba
                    PathNode n = _gridManager?.GetNode(x, y, z);
                    PathNode nUp = _gridManager?.GetNode(x, y + 1, z);
                    PathNode nDown = _gridManager?.GetNode(x, y - 1, z);

                    Debug.Log($"--- DEBUG NODO ({x}, {y}, {z}) ---");
                    LogNode(n, "TARGET");
                    LogNode(nDown, "ABAJO");
                    LogNode(nUp, "ARRIBA");
                    Debug.Log("--------------------------------");
                }
            }
        }

        private void LogNode(PathNode n, string label)
        {
            if (n == null) Debug.Log($"{label}: NULL (Fuera de rango)");
            else Debug.Log($"{label}: Tipo={n.Type}, WalkableSurface={n.IsWalkableSurface}, Cost={n.MovementPenalty}");
        }
    }
}