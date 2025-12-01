using UnityEngine;

namespace Homebound.Features.VoxelWorld
{
    public interface IVoxelMap
    {
        bool IsWalkable(Vector3Int globalPos);
        BlockType GetBlock(Vector3Int globalPos);
        void RegisterChunk(Vector3Int chunkCoord, Chunk chunk);
        void UnregisterChunk(Vector3Int chunkCoord);

        // Connections (Ladders, Teleporters)
        void RegisterConnection(Vector3Int start, Vector3Int end, float cost);
        void UnregisterConnection(Vector3Int start, Vector3Int end);
        List<Vector3Int> GetConnections(Vector3Int start);

        // Conversiones de coordenadas
        Vector3Int WorldToBlock(Vector3 position);
        Vector3 BlockToWorldCenter(Vector3Int blockPos);
    }
}
