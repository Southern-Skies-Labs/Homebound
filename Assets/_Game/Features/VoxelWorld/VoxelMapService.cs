using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.VoxelWorld
{
    public class VoxelMapService : MonoBehaviour, IVoxelMap
    {
        private Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();
        // Adjacency list for connections: StartNode -> List of EndNodes
        private Dictionary<Vector3Int, List<Vector3Int>> _connections = new Dictionary<Vector3Int, List<Vector3Int>>();


        // Tamaño del chunk asumiendo que todos son iguales.
        // Idealmente esto debería venir de configuración, pero por ahora lo detectaremos del primer chunk o usaremos constantes.
        private Vector3Int _chunkSize = new Vector3Int(50, 256, 50); // Default placeholder
        private bool _isInitialized = false;

        private void Awake()
        {
            ServiceLocator.Register<IVoxelMap>(this);
            // También registramos la clase concreta por si acaso
            ServiceLocator.Register<VoxelMapService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IVoxelMap>();
            ServiceLocator.Unregister<VoxelMapService>();
        }

        public void Configure(Vector3Int chunkSize)
        {
            _chunkSize = chunkSize;
            _isInitialized = true;
        }

        public void RegisterChunk(Vector3Int chunkCoord, Chunk chunk)
        {
            if (!_chunks.ContainsKey(chunkCoord))
            {
                _chunks.Add(chunkCoord, chunk);
            }
            else
            {
                _chunks[chunkCoord] = chunk;
            }
        }

        public void UnregisterChunk(Vector3Int chunkCoord)
        {
            if (_chunks.ContainsKey(chunkCoord))
            {
                _chunks.Remove(chunkCoord);
            }
        }

        public void RegisterConnection(Vector3Int start, Vector3Int end, float cost)
        {
            if (!_connections.ContainsKey(start))
            {
                _connections[start] = new List<Vector3Int>();
            }
            if (!_connections[start].Contains(end))
            {
                _connections[start].Add(end);
            }

            // Si es bidireccional, registramos el reverso también
            if (!_connections.ContainsKey(end))
            {
                _connections[end] = new List<Vector3Int>();
            }
            if (!_connections[end].Contains(start))
            {
                _connections[end].Add(start);
            }
        }

        public void UnregisterConnection(Vector3Int start, Vector3Int end)
        {
             if (_connections.ContainsKey(start))
             {
                 _connections[start].Remove(end);
             }
             if (_connections.ContainsKey(end))
             {
                 _connections[end].Remove(start);
             }
        }

        public List<Vector3Int> GetConnections(Vector3Int start)
        {
            if (_connections.ContainsKey(start))
            {
                return _connections[start];
            }
            return null;
        }

        public BlockType GetBlock(Vector3Int globalPos)
        {
            if (!_isInitialized) return BlockType.Air;

            Vector3Int chunkCoord = GetChunkCoordinate(globalPos);

            if (_chunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                // Convertir global a local
                Vector3Int localPos = GlobalToLocal(globalPos, chunkCoord);
                return chunk.GetBlockLocal(localPos.x, localPos.y, localPos.z);
            }

            return BlockType.Air; // Fuera de chunks cargados es aire (o vacío)
        }

        public bool IsWalkable(Vector3Int globalPos)
        {
            // Regla básica: El bloque debe ser Aire (o no solido) y el de abajo debe ser Sólido.
            // OJO: Esta es una regla simplificada. El pathfinder puede tener reglas más complejas.
            BlockType current = GetBlock(globalPos);
            BlockType below = GetBlock(globalPos + Vector3Int.down);

            // Se puede caminar si el bloque actual no es solido y hay suelo
            // Asumimos que todo lo que no sea Air es solido por ahora
            bool isCurrentPassable = (current == BlockType.Air);
            bool isGroundSolid = (below != BlockType.Air);

            return isCurrentPassable && isGroundSolid;
        }

        // --- Helpers de Coordenadas ---

        public Vector3Int WorldToBlock(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x),
                Mathf.FloorToInt(position.y),
                Mathf.FloorToInt(position.z)
            );
        }

        public Vector3 BlockToWorldCenter(Vector3Int blockPos)
        {
            return new Vector3(
                blockPos.x + 0.5f,
                blockPos.y, // Pivot suele estar abajo en agentes, pero el centro del cubo es +0.5.
                            // Sin embargo, para navegación, el "suelo" es y.
                blockPos.z + 0.5f
            );
        }

        private Vector3Int GetChunkCoordinate(Vector3Int globalPos)
        {
            // Nota: Esto asume coordenadas positivas y negativas correctamente con Floor
            int x = Mathf.FloorToInt((float)globalPos.x / _chunkSize.x);
            int y = 0; // Por ahora asumimos chunks columna, o ignoramos Y si son columnas infinitas
            int z = Mathf.FloorToInt((float)globalPos.z / _chunkSize.z);

            // Si soportamos chunks cúbicos:
            // int y = Mathf.FloorToInt((float)globalPos.y / _chunkSize.y);

            return new Vector3Int(x, y, z);
        }

        private Vector3Int GlobalToLocal(Vector3Int globalPos, Vector3Int chunkCoord)
        {
            int x = globalPos.x - (chunkCoord.x * _chunkSize.x);
            int y = globalPos.y; // Si columnas infinitas
            int z = globalPos.z - (chunkCoord.z * _chunkSize.z);

            return new Vector3Int(x, y, z);
        }
    }
}
