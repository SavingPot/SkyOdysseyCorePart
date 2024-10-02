using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

namespace GameCore
{
    public class ChallengeRoomGeneration : IslandGeneration
    {
        public const int challengeRoomIndexY = 99;

        public override void GenerateDirectBlocks()
        {
            for (int y = regionGeneration.minPoint.y + 5; y <= 0; y++)
            {
                //每 4 格一个平台
                if (y % 4 != 0)
                    continue;

                var planeCount = regionGeneration.random.Next(8, 18);
                for (int i = 0; i < planeCount; i++)
                {
                    var start = regionGeneration.random.Next(regionGeneration.minPoint.x + 5, regionGeneration.maxPoint.x - 5);
                    var length = regionGeneration.random.Next(5, 8);
                    for (int xDelta = 0; xDelta < length; xDelta++)
                    {
                        AddBlock(BlockID.Stone, start + xDelta, y, false, BlockStatus.Platform);
                    }
                }
            }
        }

        public override bool IsValidDirectBlock(BiomeData_Block block) => false;
        public override bool IsValidPerlinBlock(BiomeData_Block block) => false;
        public override bool IsValidPostProcessBlock(BiomeData_Block block) => false;

        public ChallengeRoomGeneration(RegionGeneration regionGeneration, Vector2Int centerPoint) : base(regionGeneration, centerPoint)
        {
            Debug.Log(biome.id);
        }
    }
}