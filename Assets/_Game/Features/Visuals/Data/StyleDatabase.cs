using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.Visuals
{
    // Clase auxiliar para guardar el prefab Y sus ajustes
    [System.Serializable]
    public class AccessorySettings
    {
        public string idName; // Para que lo identifiques en la lista
        public GameObject prefab;

        [Header("Ajustes Forzados")]
        public Vector3 positionOffset = Vector3.zero; // ¿Cuánto bajarlo?
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 scaleOverride = new Vector3(0.1f, 0.1f, 0.1f); // Forzamos 0.1 por defecto
    }

    [CreateAssetMenu(fileName = "StyleDatabase", menuName = "Homebound/Visuals/Style Database")]
    public class StyleDatabase : ScriptableObject
    {
        [Header("Accesorios con Ajustes")]
        // Cambiamos List<GameObject> por List<AccessorySettings>
        public List<AccessorySettings> Hairstyles;
        public List<AccessorySettings> Eyes;
        [Header("Ropa Inicial")]
        public List<GameObject> DefaultOutfits; 

        [Header("Paletas (Materiales)")]
        public List<Material> SkinMaterials;
        public List<Material> HairMaterials;
        public List<Material> EyeMaterials;

        // Métodos actualizados para devolver la configuración completa
        public AccessorySettings GetRandomHairSettings() => GetRandom(Hairstyles);
        public AccessorySettings GetRandomEyeSettings() => GetRandom(Eyes);

        public GameObject GetRandomOutfit() => GetRandom(DefaultOutfits);

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