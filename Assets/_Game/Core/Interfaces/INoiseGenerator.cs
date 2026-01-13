using UnityEngine;

namespace Homebound.Core
{
    public interface INoiseGenerator
    {
        /// <summary>
        /// Obtiene un valor de ruido 2D entre 0 y 1.
        /// </summary>
        float GetNoise01(float x, float z, float scale, int octaves, float persistence, float lacunarity, Vector2 offset);

        /// <summary>
        /// Obtiene la altura del terreno en una coordenada global específica.
        /// </summary>
        int GetTerrainHeight(int x, int z, float globalScale, float heightMultiplier, int baseHeight);
    }
}