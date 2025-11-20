using UnityEngine;

namespace Homebound.Features.VoxelWorld
{
    public enum BlockType : byte
    {
        Air = 0,
        Dirt = 1,
        Stone = 2
    }
    
    /// <summary>
    /// Esto contendrá los datos estaticos para construir los cubos (Vertices y triangulos)
    /// </summary>
    
    public static class VoxelData
    {
     
        //Esto representa las 8 esquinas de un cubo unitario
        public static readonly Vector3[] VoxelVerts = new Vector3[8]
        {
            new Vector3(0.0f, 0.0f, 0.0f), //0
            new Vector3(1.0f, 0.0f, 0.0f), //1
            new Vector3(1.0f, 1.0f, 0.0f), //2
            new Vector3(0.0f, 1.0f, 0.0f), //3
            new Vector3(0.0f, 0.0f, 1.0f), //4
            new Vector3(1.0f, 0.0f, 1.0f), //5
            new Vector3(1.0f, 1.0f, 1.0f), //6
            new Vector3(0.0f, 1.0f, 1.0f), //7
        };
        
        //Esto representa las 6 caras de un cubo unitario (Orden de vertices para formar el triangulo)
        public static readonly int[,] VoxelTris = new int[6, 4]
        {
            {0, 3, 1, 2}, // Cara trasera
            {5, 6, 4, 7}, // Cara frontal
            {3, 7, 2, 6}, // Cara superior
            {1, 5, 0, 4}, // Cara Inferior
            {4, 7, 0, 3}, // Cara derecha
            {1, 2, 5, 6} // Cara izquierda
        };
        
        //Direcciones para confirmar si hay vecinos o Culling
        public static readonly Vector3Int[] FaceChecks = new Vector3Int[6]
        {
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };
        

    }
    
}

