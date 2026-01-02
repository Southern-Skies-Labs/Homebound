using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.Visuals
{
    [CreateAssetMenu(fileName = "StyleDatabase", menuName = "Homebound/Visuals/Style Database")]
    public class StyleDatabase : ScriptableObject
    {
        [Header("Geometría (Modelos)")]
        public List<GameObject> Hairstyles;
        public List<GameObject> Eyes;

        [Header("Paletas (Materiales)")]
        [Tooltip("Materiales con diferentes texturas de paleta para la piel")]
        public List<Material> SkinMaterials;

        [Tooltip("Materiales para teñir el cabello")]
        public List<Material> HairMaterials;

        [Tooltip("Materiales para el color de ojos")]
        public List<Material> EyeMaterials;

        // Métodos de ayuda para randomización
        public GameObject GetRandomHair() => GetRandom(Hairstyles);
        public GameObject GetRandomEyes() => GetRandom(Eyes);
        public Material GetRandomSkin() => GetRandom(SkinMaterials);
        public Material GetRandomHairColor() => GetRandom(HairMaterials);
        public Material GetRandomEyeColor() => GetRandom(EyeMaterials);

        private T GetRandom<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[Random.Range(0, list.Count)];
        }
    }
}