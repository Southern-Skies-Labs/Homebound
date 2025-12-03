using UnityEngine;

namespace Homebound.Features.VoxelWorld
{
    public enum BlockType : byte
    {
        Air = 0,
        Grass = 1,
        Dirt = 2,
        Stone = 3,
        Coal = 4,
        Copper = 5,
        Gold = 6,
        Wood = 7,
        Leaves = 8,
        Bedrock = 255
    }
    
    public static class VoxelData
    {
        //CONFIGURACIÓN DEL ATLAS
        
        public static readonly float TextureAtlasSizeInBlocks = 4.0f; 
        
        public static float NormalizedBlockTextureSize => 1.0f / TextureAtlasSizeInBlocks;

        
        public static readonly Vector3[] VoxelVerts = new Vector3[8]
        {
            new Vector3(0.0f, 0.0f, 0.0f), 
            new Vector3(1.0f, 0.0f, 0.0f), 
            new Vector3(1.0f, 1.0f, 0.0f), 
            new Vector3(0.0f, 1.0f, 0.0f), 
            new Vector3(0.0f, 0.0f, 1.0f), 
            new Vector3(1.0f, 0.0f, 1.0f), 
            new Vector3(1.0f, 1.0f, 1.0f), 
            new Vector3(0.0f, 1.0f, 1.0f), 
        };
        
        public static readonly int[,] VoxelTris = new int[6, 4]
        {
            {0, 3, 1, 2}, {5, 6, 4, 7}, {3, 7, 2, 6}, 
            {1, 5, 0, 4}, {4, 7, 0, 3}, {1, 2, 5, 6} 
        };
        
        public static readonly Vector3Int[] FaceChecks = new Vector3Int[6]
        {
            new Vector3Int(0, 0, -1), new Vector3Int(0, 0, 1), new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0)
        };

        //LÓGICA DE UVS
        public static Vector2 GetTextureCoordinates(BlockType blockID)
        {
            int index = (int)blockID - 1; 
            if (index < 0) return Vector2.zero;

            
            int x = index % (int)TextureAtlasSizeInBlocks;
            int y = index / (int)TextureAtlasSizeInBlocks;
            
            return new Vector2(x, y);
        }
        
        // Define las 4 esquinas UV para una cara
        public static Vector2[] GetUVs(BlockType blockID)
        {
            Vector2 texturePos = GetTextureCoordinates(blockID);
            float size = NormalizedBlockTextureSize;
            float eps = 0.06f; 

            return new Vector2[]
            {
                new Vector2(texturePos.x * size + eps, texturePos.y * size + eps),
                new Vector2(texturePos.x * size + eps, (texturePos.y + 1) * size - eps),
                new Vector2((texturePos.x + 1) * size - eps, (texturePos.y + 1) * size - eps),
                new Vector2((texturePos.x + 1) * size - eps, texturePos.y * size + eps)
            };
        }
    }
}
