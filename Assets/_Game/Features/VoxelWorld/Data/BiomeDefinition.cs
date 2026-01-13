using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.VoxelWorld
{
    // Estructura para configurar cada mineral individualmente
    [System.Serializable]
    public struct OreSetting
    {
        public string name; // Solo para organizar en el inspector
        public BlockDefinition oreBlock;
        [Tooltip("ID interno para el Chunk (Ej: 5=Carbón, 6=Cobre)")]
        public int internalID;

        [Range(0f, 1f)] public float rarity; // Probabilidad de aparición
        public float veinSize; // Escala del ruido (más bajo = vetas más grandes)
        public int minDepth; // Altura mínima (ej: 0)
        public int maxDepth; // Altura máxima (ej: 40)
    }

    [CreateAssetMenu(fileName = "NewBiome", menuName = "Homebound/Voxel/Biome Definition")]
    public class BiomeDefinition : ScriptableObject
    {
        [Header("Terrain Blocks")]
        public BlockDefinition surfaceBlock;
        public BlockDefinition subSurfaceBlock;
        public BlockDefinition deepBlock;
        public BlockDefinition liquidBlock;

        [Header("Underground Resources")]
        public List<OreSetting> ores; 

        [Header("Terrain Shape")]
        public float terrainScale = 20f;
        public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Vegetation")]
        [Range(0f, 1f)] public float treeProbability = 0.05f;
        public List<GameObject> treePrefabs;
    }
}