using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.VoxelWorld
{
    public class FastNoiseGenerator : INoiseGenerator
    {
        // Semilla global para asegurar que el mundo sea siempre igual para el mismo ID
        private int _seed;

        public FastNoiseGenerator(int seed)
        {
            _seed = seed;
            Random.InitState(_seed);
        }

        public float GetNoise01(float x, float z, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;
            float maxValue = 0f; // Para normalizar al final

            // Algoritmo fBm (Fractal Brownian Motion)
            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x + offset.x + _seed) / scale * frequency;
                float sampleZ = (z + offset.y + _seed) / scale * frequency;

                // Mathf.PerlinNoise devuelve 0..1 (aprox)
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);

                noiseHeight += perlinValue * amplitude;

                maxValue += amplitude;

                amplitude *= persistence; // Reduce amplitud en cada octava
                frequency *= lacunarity;  // Aumenta frecuencia (detalle)
            }

            // Normalizar para devolver siempre 0..1 estricto
            return Mathf.Clamp01(noiseHeight / maxValue);
        }

        public int GetTerrainHeight(int x, int z, float globalScale, float heightMultiplier, int baseHeight)
        {
            // Configuración "Standard" para terreno base. 
            // En el futuro, estos valores vendrán del BiomeDefinition.
            float noise = GetNoise01(x, z, globalScale, 3, 0.5f, 2f, Vector2.zero);

            return Mathf.FloorToInt(baseHeight + (noise * heightMultiplier));
        }
    }
}