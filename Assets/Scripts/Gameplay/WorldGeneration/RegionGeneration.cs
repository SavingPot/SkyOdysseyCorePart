using System;
using System.Collections.Generic;
using System.Linq;
using SP.Tools;
using UnityEngine;
using Random = System.Random;

namespace GameCore
{
    public class RegionGeneration
    {
        public Random random;
        public int originalSeed;
        public int actualSeed;
        public Vector2Int index;
        public Vector2Int size;
        public Vector2Int maxPoint;
        public Vector2Int minPoint;
        public Region region;

        public static readonly Dictionary<int, string> IslandGenerationTable = new()
        {
            { 0, typeof(SkyIslandGeneration).FullName },
            { -1, typeof(MoistZoneGeneration).FullName },
        };

        public IslandGeneration NewIsland(Vector2Int centerPoint)
        {
            IslandGeneration islandGeneration = index.y switch
            {
                0 => new SkyIslandGeneration(this, centerPoint),
                -1 => new MoistZoneGeneration(this, centerPoint),
                _ => throw new NotImplementedException(),
            };
            return islandGeneration;
        }

        public void GeneratePortal()
        {
            int portalMiddleX = region.spawnPoint.x;
            int portalMiddleY = region.spawnPoint.y + 10;

            region.AddPos(BlockID.Portal, portalMiddleX, portalMiddleY, false, true);
            region.AddPos(BlockID.PortalBase, portalMiddleX, portalMiddleY - 1, false, true);
            region.AddPos(BlockID.PortalBase, portalMiddleX - 2, portalMiddleY - 1, false, true);
            region.AddPos(BlockID.PortalBase, portalMiddleX - 1, portalMiddleY - 1, false, true);
            region.AddPos(BlockID.PortalBase, portalMiddleX + 1, portalMiddleY - 1, false, true);
            region.AddPos(BlockID.PortalBase, portalMiddleX + 2, portalMiddleY - 1, false, true);
        }

        public void GenerateBoundaries()
        {
            //? 为什么是 "<=" 最高点, 而不是 "<" 最高点呢?
            //? 我们假设 min=(-3,-3), max=(3,3)
            //? 那么我们会循环 -3, -2, -1, 0, 1, 2
            //? 发现了吗, 3 没有被遍历到, 以此要使用 "<="
            for (int x = minPoint.x; x <= maxPoint.x; x++)
            {
                for (int y = minPoint.y; y <= maxPoint.y; y++)
                {
                    //检测点位是否在边界上
                    if (x == minPoint.x || y == minPoint.y || x == maxPoint.x || y == maxPoint.y)
                    {
                        //删除边界上的任何方块
                        region.RemovePos(x, y, false);
                        region.RemovePos(x, y, true);

                        //添加边界方块
                        region.AddPos(BlockID.Boundary, x, y, false, true);
                    }
                }
            }
        }

        public void Finish()
        {
            region.generatedAlready = true;

            lock (GFiles.world.regionData)
                GFiles.world.AddRegion(region);
        }





        public RegionGeneration(int seed, Vector2Int index, string specificBiome = null)
        {
            this.index = index;

            //乘数是为了增加 index 差, 避免比较靠近的区域生成一致
            actualSeed = seed + index.x * 2 + index.y * 4;

            //改变随机数种子, 以确保同一种子的地形一致, 不同区域地形不一致
            random = new(actualSeed);

            /* --------------------------------- 群系 -------------------------------- */
            BiomeData biome;
            if (specificBiome == null)
            {
                biome = index.x == 0 ? ModFactory.CompareBiome(BiomeID.Center) : ModFactory.globalBiomes.Extract(random);
            }
            else
            {
                biome = ModFactory.CompareBiome(specificBiome);
            }
            if (biome == null)
            {
                Debug.LogError($"群系为空, 将生成 {BiomeID.Desert}");
                biome = ModFactory.CompareBiome(BiomeID.Desert);
            }

            /* ------------------------------------------------------------------------ */

            //确定大小
            size = new(Region.chunkCount * Chunk.blockCountPerAxis, Region.chunkCount * Chunk.blockCountPerAxis);

            //边际 (左下右上)
            maxPoint = new((int)Math.Floor(size.x / 2f), (int)Math.Floor(size.y / 2f));
            minPoint = -maxPoint;



            region = new()
            {
                size = size,
                index = index,
                biomeId = biome.id,
                maxPoint = maxPoint,
                minPoint = minPoint
            };
        }
    }
}