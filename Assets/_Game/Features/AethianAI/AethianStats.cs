using UnityEngine;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.AethianAI
{
    public class AethianStats : MonoBehaviour
    {
        //Variables
        [Header("Identity")]
        public string CharacterName = "Aldeano";
        public string Title = "Errante";

        [Header("Class Configuration")]
        [Tooltip("Arrastra aquí el asset UnitClassDefinition (ej. Villager_Data)")]
        [SerializeField] private UnitClassDefinition _currentClassDef;
        public UnitClassDefinition CurrentClass => _currentClassDef;

        [Header("Vitality")]
        public float Health = 100f;
        public float MaxHealth = 100f;

        [Header("Traits")]
        [Range(0.1f, 3f)]
        public float MetabolismRate = 1.0f;

        [Header("Attributes")]
        public float GatheringPower = 1.0f;

        [Header("Needs")]
        public Need Hunger = new Need("Hambre", 10f);
        public Need Thirst = new Need("Sed", 15f);
        public Need Energy = new Need("Energía", 5f);

        public string GetFullName() => $"{CharacterName} <{Title}>";

        public void UpdateNeeds(float deltaGameHours)
        {
            Hunger.Decay(deltaGameHours * MetabolismRate);
            Thirst.Decay(deltaGameHours * MetabolismRate);
            Energy.Decay(deltaGameHours);
        }

        private void OnValidate()
        {
            if (_currentClassDef == null)
            {
                Debug.LogWarning($"[AethianStats] {_currentClassDef} no está asignado en {gameObject.name}. Por favor, asigna un UnitClassDefinition válido.");
            }
        }
    }
}