using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Homebound.Features.Visuals
{
    [CreateAssetMenu(fileName = "AnimDefinition_General", menuName = "Homebound/Visuals/Animation State Definition")]
    public class AnimationStateDefinition : ScriptableObject
    {
        [Header("Configuración General")]
        [Tooltip("Nombre del parámetro Float en el Animator para la velocidad de movimiento")]
        public string SpeedParameter = "Speed";
        [Tooltip("Nombre del parámetro Bool en el Animator para saber si se mueve")]
        public string IsMovingParameter = "IsMoving";

        [Header("Mapeo de Estados Lógicos")]
        [SerializeField] private List<StateMapping> _stateMappings;

        
        private Dictionary<string, StateMapping> _mappingLookup;

        public void Initialize()
        {
            if (_stateMappings == null) return;
            _mappingLookup = _stateMappings.ToDictionary(m => m.LogicStateName, m => m);
        }

        public bool TryGetMapping(string stateName, out StateMapping mapping)
        {
            if (_mappingLookup == null) Initialize();
            return _mappingLookup.TryGetValue(stateName, out mapping);
        }

        [System.Serializable]
        public class StateMapping
        {
            [Tooltip("El nombre exacto que envía AethianBot (ej: 'Idle', 'Working')")]
            public string LogicStateName;

            [Header("Animator Parameters")]
            [Tooltip("Nombre del Trigger a disparar al entrar al estado")]
            public string EnterTrigger;

            [Tooltip("Nombre del Bool a mantener en True mientras esté en este estado")]
            public string StateBool;

            [Header("Variantes (Randomness)")]
            [Tooltip("¿Este estado tiene múltiples animaciones? (ej: Idle 1, Idle 2)")]
            public bool HasVariants;
            [Tooltip("Nombre del parámetro Int/Float para elegir la variante")]
            public string VariantParameter;
            [Tooltip("Cantidad de variantes disponibles")]
            public int VariantCount;
        }
    }
}