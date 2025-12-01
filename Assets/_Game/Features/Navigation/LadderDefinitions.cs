using UnityEngine;

namespace Homebound.Features.Navigation
{
    public enum LadderType
    {
        Standard,
        Emergency,
        Siege,
        Scaffolding
    }

    public struct LadderConstructionRequest
    {
        public Vector3 BottomPosition;
        public Vector3 TopPosition;
        public LadderType Type;
        public float Duration;
        public bool IsValid;

        public static LadderConstructionRequest Invalid => new LadderConstructionRequest { IsValid = false };

        public static LadderConstructionRequest Create(Vector3 bottom, Vector3 top, LadderType type, float duration)
        {
            return new LadderConstructionRequest
            {
                BottomPosition = bottom,
                TopPosition = top,
                Type = type,
                Duration = duration,
                IsValid = true
            };
        }
        
        
        
        
        
        
        
    }
    
    
}
