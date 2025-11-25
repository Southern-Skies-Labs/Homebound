using UnityEngine;

namespace Homebound.Features.AethianAI
{
    [System.Serializable]
    public class AethianStats
    {
        //Variables
        [Header("Identity")] 
        public string CharacterName = "Aldeano";
        public string Title = "Errante";
        
        [Header("Vitality")] 
        public float Health = 100f;
        public float MaxHealth = 100f;
        
        [Header("Needs")]
        public Need Hunger = new Need("Hambre", 5f);
        public Need Thirst = new Need("Sed", 8f);
        public Need Energy = new Need("Energía", 3f);
        
        public string GetFullName() => $"{CharacterName} <{Title}>";

        // Reduce el hambre con el tiempo, retorna a true si murió de hambre.
        public void UpdateNeeds(float gameHoursPassed)
        {
            Hunger.Decay(gameHoursPassed);
            Thirst.Decay(gameHoursPassed);
            Energy.Decay(gameHoursPassed); 
        }
    }
}


