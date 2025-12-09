using UnityEngine;

namespace Homebound.Features.Economy
{
    public enum ItemType
    {
        Resource,
        Food,
        Tool,
        Building
    }
    
    
    
    [CreateAssetMenu(fileName = "NewItem", menuName = "Homebound/Economy/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("General Info")] 
        public string ID;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("Visuals")] 
        public Sprite Icon;
        public GameObject WorldPrefab;
        
        [Header("Settings")]
        public ItemType Type;
        public int MaxStack = 99;
        public bool IsStackeable = true;

        [Header("Consumable")] 
        public float NutritionValue = 10f;

        [Tooltip("Multiplicador de saciedad")] public float SaturationValue = 1.0f;
    }

}

