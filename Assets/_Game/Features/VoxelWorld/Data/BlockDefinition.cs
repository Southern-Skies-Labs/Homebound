using UnityEngine;

namespace Homebound.Features.VoxelWorld
{
    [CreateAssetMenu(fileName = "NewBlock", menuName = "Homebound/Voxel/Block Definition")]
    public class BlockDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string blockID;
        public string displayName;

        [Header("Rendering (Texture Array Indices)")]
        [Tooltip("Índice inicial para la cara SUPERIOR")]
        public int topTextureIndex;
        [Tooltip("Cuántas texturas consecutivas usar para variaciones aleatorias")]
        [Min(1)] public int topVariants = 1;

        [Space(10)]
        [Tooltip("Índice inicial para los LADOS")]
        public int sideTextureIndex;
        [Min(1)] public int sideVariants = 1;

        [Space(10)]
        [Tooltip("Índice inicial para la cara INFERIOR")]
        public int bottomTextureIndex;
        [Min(1)] public int bottomVariants = 1;

        [Header("Physics & Nav")]
        public bool isSolid = true;
        public bool isLiquid = false;
        public bool isTransparent = false;
        [Range(1, 255)] public int navigationCost = 1;

        /// <summary>
        /// Calcula el índice final de la textura basándose en la posición del bloque
        /// para mantener la consistencia visual (ruido determinista).
        /// </summary>
        public int GetTextureIndex(Vector3Int normal, Vector3 position)
        {
            int startIndex;
            int variantCount;

            // 1. Determinar qué cara estamos pintando
            if (normal.y > 0) // Arriba
            {
                startIndex = topTextureIndex;
                variantCount = topVariants;
            }
            else if (normal.y < 0) // Abajo
            {
                startIndex = bottomTextureIndex;
                variantCount = bottomVariants;
            }
            else // Lados
            {
                startIndex = sideTextureIndex;
                variantCount = sideVariants;
            }

            // 2. Si no hay variaciones, devolver directo
            if (variantCount <= 1) return startIndex;

            // 3. Lógica Estocástica (La misma de tu VoxelDefinitions original)
            // Usamos la posición para que el mismo bloque siempre tenga la misma textura
            int seed = Mathf.FloorToInt(position.x * 3f + position.y * 7f + position.z * 13f);
            int variant = Mathf.Abs(seed) % variantCount;

            return startIndex + variant;
        }
    }
}