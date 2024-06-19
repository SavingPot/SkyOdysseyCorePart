using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

namespace GameCore
{
    public class MoistZoneGeneration : IslandGeneration
    {
        public override bool IsValidDirectBlock(BiomeData_Block block) => false;
        public override bool IsValidPerlinBlock(BiomeData_Block block) => false;
        public override bool IsValidPostProcessBlock(BiomeData_Block block) => false;
        public override bool IsValidStructure(BiomeData_Structure structure) => false;

        protected override bool CheckStructureConditions(int x, int y, BiomeData_Structure structure)
        {
            return x == 0 && y == 0;
        }

        protected override void GenerateStructure(int x, int y, BiomeData_Structure structure)
        {
            base.GenerateStructure(x, y, structure);

            WriteBlockCustomDataInStructure(BlockID.WoodenChest, x, y, structure, (_, _, _) => GenerateLootCustomData(40));
        }

        public MoistZoneGeneration(RegionGeneration regionGeneration, Vector2Int centerPoint) : base(regionGeneration, centerPoint)
        {
            var structureTemp = structures.ToList();
            structureTemp.Add(new(ModFactory.CompareStructure(StructureID.GhostShip)));
            structures = structureTemp.ToArray();
        }
    }
}