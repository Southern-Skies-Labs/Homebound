using UnityEngine;
using System;
using UnityEditor.Embree;

namespace Homebound.Features.AethianAI
{
    [Serializable]    
    public class Need
    {
        //Variables
        public string Name;
        public float Value = 100f;
        public float MaxValue = 100f;
        public float DecayRate = 1f;
        
        //Umbral critico para activar modo supervivencia
        public float CriticalThreshold = 20f;
        
        //Metodos
        public Need(string name, float decayRate)
        {
            Name = name;
            DecayRate = decayRate;
            Value = 100f;
        }

        public void Decay(float deltaTimeHours)
        {
            Value -= DecayRate * deltaTimeHours;
            Value = Mathf.Clamp(Value, 0, MaxValue);
        }

        public void Restore(float amount)
        {
            Value += amount;
            Value = Mathf.Clamp(Value, 0, MaxValue);
        }
        
        public bool IsCritical() => Value <= CriticalThreshold;

    }

}