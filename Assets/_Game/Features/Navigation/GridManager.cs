using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class GridManager : MonoBehaviour
    {
        //Variables
        private PathNode[,,] _grid;
        private int _width;
        private int _height;
        private int _depth;
        
        
        //Metodos
        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GridManager>();
        }

        public void InitializeGrid(int width, int height, int depth)
        {
            _width = width;
            _height = height;
            _depth = depth;
            _grid = new PathNode[width, height, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        _grid[x, y, z] = new PathNode(x, y, z, false); 
                    }
                }
            }
            Debug.Log($"[GridManager] Grilla de navegación inicializada: {width}x{height}x{depth}");
        }

        public void UpdateNode(int x, int y, int z, bool isSolid)
        {
            if (!IsInsideGrid(x, y, z)) return;
            
            // Lógica Voxel Básica:
            // Un nodo es "Caminable" si el bloque en sí es AIRE (no sólido)
            // Y el bloque justo DEBAJO es SÓLIDO (suelo).
            // PERO: Para simplificar la actualización, aquí solo marcamos si este nodo está obstruido o no.
            // La lógica real de "puedo pararme aquí" se evalúa mejor chequeando el nodo y su vecino inferior.
            
            // En esta implementación: PathNode representa el ESPACIO, no el suelo.
            // Si isSolid es true, el nodo NO es caminable (hay una pared).
            // Si isSolid es false, podría ser caminable (si tiene suelo abajo).
            
            // Actualizamos el estado base
            // Nota: Esta lógica se refinará cuando integremos la gravedad, 
            // por ahora: Si es sólido = No caminable. Si es aire = Potencialmente caminable.
                
            _grid[x, y, z].IsWalkable = !isSolid;
        }

        public bool IsInsideGrid(int x, int y, int z)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height && z >= 0 && z < _depth;
        }

        public PathNode GetNode(int x, int y, int z)
        {
            if (IsInsideGrid(x, y, z)) return _grid[x, y, z];
            return null;
        }

        public List<PathNode> GetNeighbors(PathNode node)
        {
            List<PathNode> neighbors = new List<PathNode>();

            int[] xDir = { 0, 1, 0, -1 };
            int[] zDir = { 1, 0, -1, 0 };

            for (int i = 0; i < 4; i++)
            {
                int checkX = node.X + xDir[i];
                int checkZ = node.Z + zDir[i];
                
                //Vecino en mismo nivel
                CheckAndAddNeighbor(checkX, node.Y, checkZ, neighbors);
                
                //Vecino de abajo
                CheckAndAddNeighbor(checkX, node.Y -1, checkZ, neighbors);
                
                //Vecino de arriba
                CheckAndAddNeighbor(checkX, node.Y + 1, checkZ, neighbors);
            }

            return neighbors;
        }

        private void CheckAndAddNeighbor(int x, int y, int z, List<PathNode> list)
        {
            if (IsInsideGrid(x, y, z))
            {
                if (_grid[x, y, z].IsWalkable)
                {
                    list.Add(_grid[x, y, z]);
                }
            }
        }
    }
}
