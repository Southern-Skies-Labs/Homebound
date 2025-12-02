using UnityEngine;
using System.Collections.Generic;
using Homebound.Features.VoxelWorld;


namespace Homebound.Features.Construction
{
    [CreateAssetMenu(fileName = "New Blueprint", menuName = "Homebound/Construction/Blueprint")]
    public class BuildingBlueprint : ScriptableObject
    {
        public string BuildingName;

        public List<BlueprintBlock> StructureBlocks;
        
        public List<BlueprintBlock> DetailBlocks;

    }
    
    [System.Serializable]
    public struct BlueprintBlock
    {
        public Vector3Int LocalPosition;
        public BlockType Type;
    }
}