using UnityEngine;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.AethianAI
{
    [System.Serializable]
    public class AethianStats
    {
        //Variables
        [Header("Identity")] 
        public string CharacterName = "Aldeano";
        public string Title = "Errante";
        public UnitClass Class = UnitClass.Villager;
        
        [Header("Vitality")] 
        public float Health = 100f;
        public float MaxHealth = 100f;

        [Header("Traits")] [Range(0.1f, 3f)] public float MetabolismRate = 1.0f;
        
        [Header("Attributes")]
        public float GatheringPower = 1.0f;
        
        [Header("Needs")]
        public Need Hunger = new Need("Hambre", 10f);
        public Need Thirst = new Need("Sed", 15f);
        public Need Energy = new Need("Energía",  5f);
        
        public string GetFullName() => $"{CharacterName} <{Title}>";

       
        public void UpdateNeeds(float deltaGameHours)
        {
            Hunger.Decay(deltaGameHours * MetabolismRate);
            Thirst.Decay(deltaGameHours * MetabolismRate);
            Energy.Decay(deltaGameHours);
        }
    }
}


