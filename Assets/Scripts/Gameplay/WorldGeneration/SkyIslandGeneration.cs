using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

namespace GameCore
{
    public class SkyIslandGeneration : IslandGeneration
    {
        protected override bool CheckStructureConditions(int x, int y, BiomeData_Structure structure)
        {
            if (structure.structure.id == StructureID.UndergroundRelics)
                return x == -9 && y == -3;
            else
                return base.CheckStructureConditions(x, y, structure);
        }

        protected override void GenerateStructure(int x, int y, BiomeData_Structure structure)
        {
            base.GenerateStructure(x, y, structure);

            if (structure.structure.id == StructureID.UndergroundRelics)
                WriteBlockCustomDataInStructure(BlockID.WoodenChest, x, y, structure, (_, _, _) =>
                {
                    var result = GenerateLootCustomData(40);
                    var array = result["ori:container"]["items"]["array"];

                    //随机填充 1 个战利品袋
                    var index = regionGeneration.random.Next(array.Count());
                    array[index] = ModFactory.CompareItem(ItemID.LootBag)
                                        .DataToItem()
                                        .SetCustomData(LootBagBehaviour.NewCustomData(
                                            ModFactory.CompareItem(ItemID.SportsVest).DataToItem(),
                                            ModFactory.CompareItem(ItemID.SportsShorts).DataToItem(),
                                            ModFactory.CompareItem(ItemID.Sneakers).DataToItem(),
                                            ModFactory.CompareItem(BlockID.OnionCrop).DataToItem().SetCount(6))).
                                        ToJToken();

                    return result;
                });
        }

        public SkyIslandGeneration(RegionGeneration regionGeneration, Vector2Int centerPoint) : base(regionGeneration, centerPoint)
        {
            //如果是中心空岛就添加地下遗迹
            if (regionGeneration.index == Vector2Int.zero)
            {
                var structureTemp = structures.ToList();
                structureTemp.Add(new(ModFactory.CompareStructure(StructureID.UndergroundRelics)));
                structures = structureTemp.ToArray();
            }
        }
    }
}