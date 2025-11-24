using System;
using UnityEngine;
using Homebound.Core;


namespace Homebound.Features.Identity
{
    public enum Gender {Male, Female}
    public enum Race {Aethian, Trodar}
    
    public class NameGeneratorService : MonoBehaviour
    {
        [Header("Database")] 
        [SerializeField] private NameDatabase _database;

        private void Awake()
        {
            if (_database == null)
            {
                Debug.Log("[NameGenerator] Falta asignar la Name Database");
            }
            ServiceLocator.Register<NameGeneratorService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<NameGeneratorService>();
        }

        public string GetRandomName(Race race, Gender gender)
        {
            if (_database == null) return "Error Data";

            string firstName = "Unit";

            if (race == Race.Aethian)
            {
                if (gender == Gender.Male) firstName = _database.GetRandomMaleName();
                else firstName = _database.GetRandomFemaleName();
            }
            else if (race == Race.Trodar)
            {
                if (gender == Gender.Male) firstName = _database.GetRandomMaleTrodarName();
                else firstName = _database.GetRandomFemaleTrodarName();
            }
            
            string surname = _database.GetRandomSurname();
            return $"{firstName} {surname}";

        }
    }
}
