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
        // Vértices y Triángulos 
        public static readonly Vector3[] VoxelVerts = new Vector3[8]
        {
            new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), 
            new Vector3(1.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), 
            new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f), 
            new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), 
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

        //SISTEMA DE TEXTURE ARRAY 
        //Formato: { Indice_Inicio, Cantidad_Variantes }

        private static readonly int[][] BlockTextureIndices = new int[][]
        {
            new int[] { 0, 0 },   // 0: Air (No usa texturas)
            new int[] { 0, 3 },   // 1: Grass (Índices 0, 1, 2) -> 3 variantes
            new int[] { 3, 2 },   // 2: Dirt  (Índices 3, 4)    -> 2 variantes
            new int[] { 5, 2 },   // 3: Stone (Índices 5, 6)    -> 2 variantes
            new int[] { 7, 1 },   // 4: Coal  (Índice 7)        -> 1 variante
            new int[] { 8, 2 },   // 5: Copper (Índices 8, 9)   -> 2 variantes
            new int[] { 10, 2 },  // 6: Gold   (Índices 10, 11) -> 2 variantes (CORREGIDO, antes pisaba Copper)
            // new int[] { 12, 2 },  // 7: Wood   (Índices 12, 13) -> 2 variantes (CORREGIDO)
            // new int[] { 14, 2 },  // 8: Leaves (Índices 14, 15) -> 2 variantes (CORREGIDO)
        };

        public static int GetTextureIndex(BlockType blockID, Vector3 position)
        {
            // Mapeo seguro para Bedrock u otros
            if (blockID == BlockType.Bedrock) return 5; // Usa piedra por defecto
            int id = (int)blockID;
            if (id >= BlockTextureIndices.Length) return 0;

            int[] info = BlockTextureIndices[id];
            int startIndex = info[0];
            int variantCount = info[1];

            if (variantCount <= 1) return startIndex;

            // --- ESTOCÁSTICO DETERMINISTA ---
            int seed = Mathf.FloorToInt(position.x * 3f + position.y * 7f + position.z * 13f);
            
            // Usamos System.Random o un hash simple
            int variant = Mathf.Abs(seed) % variantCount;

            return startIndex + variant;
        }
    }
}