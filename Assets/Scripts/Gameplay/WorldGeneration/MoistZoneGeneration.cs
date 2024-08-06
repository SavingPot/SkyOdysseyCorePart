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

        protected override bool CheckStructureConditions(int x, int y, BiomeData_Structure structure) =>
            structure?.structure?.id switch
            {
                StructureID.GhostShip => x == 0 && y == -6,
                StructureID.BowlStone => (y >= regionGeneration.maxPoint.y * 0.15f || y <= regionGeneration.minPoint.y * 0.15f) && //碗石只生成在上、下部分 85%
                                         Tools.Prob100(structure.structure.probability, regionGeneration.random),
                _ => false,
            };

        protected override void GenerateStructure(int x, int y, BiomeData_Structure structure)
        {
            base.GenerateStructure(x, y, structure);

            if (structure?.structure?.id == StructureID.GhostShip)
            {
                WriteBlockCustomDataInStructure(BlockID.WoodenChest, x, y, structure,
                (_, _, _) => GenerateLootCustomData(40, item => item.HasTag("ori:loot.ghost_ship"), spell => spell.HasTag("ori:loot.ghost_ship")));
            }
        }

        public MoistZoneGeneration(RegionGeneration regionGeneration, Vector2Int centerPoint) : base(regionGeneration, centerPoint)
        {
            var structureTemp = structures.ToList();
            structureTemp.Add(new(ModFactory.CompareStructure(StructureID.GhostShip)));
            structureTemp.Add(new(ModFactory.CompareStructure(StructureID.BowlStone)));
            structures = structureTemp.ToArray();
        }
    }
}