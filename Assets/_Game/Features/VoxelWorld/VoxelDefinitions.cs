using UnityEngine;

namespace Homebound.Features.VoxelWorld
{
    // Mantenemos el Enum solo para evitar errores de compilación masivos inmediatos.
    // En la Fase 2, reemplazaremos su uso por strings o índices directos.
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
        // Dimensiones del Chunk (Estándar de Homebound)
        public static readonly int ChunkWidth = 16;
        public static readonly int ChunkHeight = 128; // Ampliado para soportar montañas

        // --- Geometría Estática (No cambia) ---
        public static readonly Vector3[] VoxelVerts = new Vector3[8]
        {
            new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f),
            new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f),
        };

        public static readonly int[,] VoxelTris = new int[6, 4]
        {
            {0, 3, 1, 2}, // Back Face
            {5, 6, 4, 7}, // Front Face
            {3, 7, 2, 6}, // Top Face
            {1, 5, 0, 4}, // Bottom Face
            {4, 7, 0, 3}, // Left Face
            {1, 2, 5, 6}  // Right Face
        };

        public static readonly Vector3Int[] FaceChecks = new Vector3Int[6]
        {
            new Vector3Int(0, 0, -1), // Back
            new Vector3Int(0, 0, 1),  // Front
            new Vector3Int(0, 1, 0),  // Top
            new Vector3Int(0, -1, 0), // Bottom
            new Vector3Int(-1, 0, 0), // Left
            new Vector3Int(1, 0, 0)   // Right
        };

        // NOTA: La lógica de BlockTextureIndices ha sido eliminada.
        // Ahora es responsabilidad de 'BlockDefinition.GetTextureIndex()'.

        // --- PARCHE TEMPORAL PARA COMPILACIÓN (Eliminar en Fase 2) ---
        // Esto permite que el viejo Chunk.cs compile mientras creamos los nuevos datos.
        public static int GetTextureIndex(BlockType blockID, Vector3 position)
        {
            // Retorna 0 (o el índice que quieras) temporalmente para evitar el error.
            // En la Fase 2, Chunk.cs leerá esto desde los BlockDefinitions.
            return 0;
        }
    }
}