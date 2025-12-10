using UnityEngine;
using Homebound.Core;
using Homebound.Features.Navigation;

namespace Homebound.Testing
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private bool _showWalkable = false;
        [SerializeField] private bool _showObstacles = true;
        [SerializeField] private int _radius = 10; // Radio alrededor de este objeto para dibujar

        private GridManager _grid;

        private void Start()
        {
            _grid = ServiceLocator.Get<GridManager>();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _grid == null) return;

            Vector3 center = transform.position;
            
            // Dibujamos solo un área cercana para no laggear el editor
            for (int x = -_radius; x <= _radius; x++)
            {
                for (int z = -_radius; z <= _radius; z++)
                {
                    for (int y = -5; y <= 10; y++) // Altura relativa
                    {
                        int checkX = (int)center.x + x;
                        int checkY = (int)center.y + y;
                        int checkZ = (int)center.z + z;

                        var node = _grid.GetNode(checkX, checkY, checkZ);
                        
                        if (node != null)
                        {
                            // Nodos Sólidos (Obstáculos) -> ROJO
                            if (_showObstacles && node.Type == NodeType.Solid)
                            {
                                Gizmos.color = new Color(1, 0, 0, 0.5f); // Rojo semitransparente
                                Gizmos.DrawCube(new Vector3(checkX, checkY, checkZ), Vector3.one * 0.9f);
                            }
                            // Superficies Caminables -> VERDE (Solo wireframe)
                            else if (_showWalkable && node.IsWalkableSurface)
                            {
                                Gizmos.color = Color.green;
                                Gizmos.DrawWireCube(new Vector3(checkX, checkY, checkZ), Vector3.one * 0.9f);
                            }
                        }
                    }
                }
            }
        }
    }
}