using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.TaskSystem
{
    [CreateAssetMenu(fileName = "NewUnitClass", menuName = "Homebound/Unit Class Definition")]
    public class UnitClassDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string ClassName;
        [TextArea] public string Description;
        public Sprite Icon;

        [Header("Base Stats")]
        public float BaseHealth = 100f;
        public float BaseMetabolism = 1.0f;
        public float BaseGatheringPower = 1.0f;

        [Header("Capabilities")]
        [Tooltip("Lista de trabajos que esta clase puede realizar")]
        public List<JobType> SupportedJobs;

    }
}
    
