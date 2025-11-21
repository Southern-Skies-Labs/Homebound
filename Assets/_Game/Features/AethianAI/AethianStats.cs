using UnityEngine;

namespace Homebound.Features.AethianAI
{
    [System.Serializable]
    public class AethianStats
    {
        //Variables
        [Header("Life Points")] 
        public float Health = 100f;
        public float MaxHealth = 100f;

        [Header("Survival")] 
        public float Hunger = 100f;
        public float maxHunger = 100f;
        
        [Header("Energy")]
        public float Energy = 100f;
        public float maxEnergy = 100f;
        
        //Umbrales de configuración
        public const float HUNGER_CRITICAL_THRESHOLD = 20f;
        
        

        // Reduce el hambre con el tiempo, retorna a true si murió de hambre.
        public bool DecayHunger(float amount)
        {
            Hunger -= amount;
            Hunger = Mathf.Clamp(Hunger, 0, maxHunger);
            return Hunger <= 0;
        }
    }
}


