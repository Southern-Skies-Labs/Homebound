using UnityEngine;
using System.Collections.Generic;
using Homebound.Features.VoxelWorld;
using Homebound.Features.Economy;


namespace Homebound.Features.Construction
{
    [CreateAssetMenu(fileName = "New Blueprint", menuName = "Homebound/Construction/Blueprint")]
    public class BuildingBlueprint : ScriptableObject
    {
        [Header("Identity")]
        public string BuildingName;
        [TextArea] public string Description;
        public Sprite Icon;


        [Header("Structure (Voxel Grid)")]
        [Tooltip("Bloques que forman la estructura fisica (Paredes, suelo, etc) del edificio.")]
        public List<BlueprintBlock> StructureBlocks;

        [Header("Decoration (Props)")]
        [Tooltip("Objetos manufacturados colocados en el edificio (Camas, mesas, etc)")]
        public List<PropEntry> Props;

        [Header("Economy")]
        [Tooltip("Recursos totales necesarios para iniciar o completar la construcción")]
        public List<CostEntry> ConstructionCosts;

    }

    //Representa un Bloque Voxel en la grilla del edificio
    [System.Serializable]
    public struct BlueprintBlock
    {
        public Vector3Int LocalPosition;
        public BlockType Type;
    }

    [System.Serializable]
    public struct PropEntry
    {
        [Tooltip("Referencia al ItemData que reprenta este mueble (Requiere un World Prefab para funcionar)")]
        public ItemData ItemRef;

        public Vector3 LocalPosition;
        public Vector3 LotalRotation;
    }

    [System.Serializable]
    public struct CostEntry
    {
        public ItemData Resource;
        public int Amount;
    }



}