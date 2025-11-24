using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.Identity
{
    [CreateAssetMenu(fileName = "NameDatabase", menuName = "Homebound/Identity/Name Database")]
    public class NameDatabase : ScriptableObject
    {
        
        //Variables para asignacion de nombres por raza
        [Header("Aethian Names")] 
        public List<string> MaleNames = new List<string>();
        public List<string> FemaleNames = new List<string>();
        
        [Header("General")]
        public List<string> Surnames = new List<string>();
        
        [Header("Trodar Names")]
        public List<string> MaleTrodarNames = new List<string>();
        public List<string> FemaleTrodarNames = new List<string>();
        
        //Metodos para obtener listas aleatorias
        public string GetRandomMaleName() => MaleNames.Count > 0 ? MaleNames[Random.Range(0, MaleNames.Count)] : "SinNombre";
        public string GetRandomFemaleName() => FemaleNames.Count > 0 ? FemaleNames[Random.Range(0, FemaleNames.Count)] : "SinNombre";
        public string GetRandomSurname() => Surnames.Count > 0 ? Surnames[Random.Range(0, Surnames.Count)] : "Errante";
        public string GetRandomMaleTrodarName() => MaleTrodarNames.Count > 0 ? MaleTrodarNames[Random.Range(0, MaleTrodarNames.Count)] : "SinNombre";
        public string GetRandomFemaleTrodarName() => FemaleTrodarNames.Count > 0 ? FemaleTrodarNames[Random.Range(0, FemaleTrodarNames.Count)] : "SinNombre";

    }
}

