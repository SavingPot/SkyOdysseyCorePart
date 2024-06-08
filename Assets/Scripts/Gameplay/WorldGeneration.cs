using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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



        public IslandGeneration NewIsland(Vector2Int centerPoint)
        {
            var islandGeneration = new IslandGeneration(this, centerPoint);
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
            BiomeData biome = null;
            if (specificBiome == null)
            {
                if (index.x == 0)
                {
                    biome = ModFactory.CompareBiome(BiomeID.Center);
                }
                else
                {
                    List<BiomeData> biomes = new();

                    foreach (var mod in ModFactory.mods)
                    {
                        foreach (var currentBiome in mod.biomes)
                        {
                            //找到正负相符的群系
                            if (Math.Sign(currentBiome.distribution) == Math.Sign(index.x))
                            {
                                biomes.Add(currentBiome);
                            }
                        }
                    }

                    if (biomes.Count != 0)
                    {
                        biomes = biomes.OrderBy(b => Math.Abs(b.distribution)).ToList();
                        biome = biomes[Math.Min(Math.Abs(index.x) - 1, biomes.Count - 1)];
                    }
                }
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

    public class IslandGeneration
    {
        public Vector2Int maxPoint;
        public Vector2Int minPoint;
        public Vector2Int size;
        public RegionGeneration regionGeneration;
        public BiomeData biome;
        public int yOffset;
        public int surface;
        public int surfaceExtra1;
        public Vector2Int centerPoint;
        public FormulaAlgebra directBlockComputationAlgebra;
        public List<(Vector2Int pos, bool isBackground)> blockAdded;

        //字典返回的是这个 x 值对应的 y 值最高的点 (x 为 Key   y 为 Value)
        public Dictionary<int, int> wallHighestPointFunction = new();
        public Dictionary<int, int> backgroundHighestPointFunction = new();

        BiomeData_Block[] directBlocks;
        BiomeData_Block[] perlinBlocks;
        BiomeData_Block[] postProcessBlocks;
        BiomeData_Block[] unexpectedBlocks;






        public static FormulaAlgebra GetIslandGenerationFormulaAlgebra(int bottom, int surface, int top)
        {
            return new()
            {
                {
                    "@bottom", bottom
                },
                {
                    "@surface", surface
                },
                {
                    "@top", top
                }
            };
        }

        public static FormulaAlgebra GetPerlinFormulaAlgebra(int lowestNoise, int highestNoise, int bottom, int surface, int top)
        {
            return new()
            {
                {
                    "@lowest_noise", lowestNoise
                },
                {
                    "@highest_noise", highestNoise
                },
                {
                    "@bottom", bottom
                },
                {
                    "@surface", surface
                },
                {
                    "@top", top
                }
            };
        }



        public void AddBlock(string blockId, int x, int y, bool isBackground, string customData = null)
        {
            //? 添加到区域生成，需要根据岛的中心位置做偏移，而在添加到岛屿生成则不需要
            regionGeneration.region.AddPos(blockId, x + centerPoint.x, y + centerPoint.y, isBackground, true, customData);

            blockAdded.Add((new(x, y), isBackground)); //* 如果已经存在过方块可能会出问题，但是目前来看无伤大雅
        }


        public void AddBlockForAreas(string blockId, int x, int y, Vector3Int[] areas)
        {
            regionGeneration.region.AddPos(blockId, x + centerPoint.x, y + centerPoint.y, areas, true); //* 如果已经存在过方块可能会出问题，但是目前来看无伤大雅

            foreach (var item in areas)
            {
                Vector2Int actualPos = new(x + item.x, y + item.y);
                bool actualIsBackground = item.z < 0;

                for (int c = 0; c < blockAdded.Count; c++)
                {
                    var (pos, isBackground) = blockAdded[c];
                    if (pos == actualPos && isBackground == actualIsBackground)
                        blockAdded.RemoveAt(c);
                }
                blockAdded.Add((actualPos, actualIsBackground));
            }
        }

        public void GenerateDirectBlocks()
        {
            //遍历每个点
            for (int x = minPoint.x; x < maxPoint.x; x++)
            {
                for (int y = minPoint.y; y < maxPoint.y; y++)
                {
                    foreach (var g in directBlocks)
                    {
                        //概率抽取
                        if (!Tools.Prob100(g.rules.probability, regionGeneration.random))
                            continue;

                        //检查依赖方块
                        if (!string.IsNullOrWhiteSpace(g.attached.blockId) && (regionGeneration.region.GetBlock(centerPoint.x + x + g.attached.offset.x, centerPoint.y + y + g.attached.offset.y, g.attached.isBackground).save?.blockId) != g.attached.blockId)
                            continue;

                        //遍历所有范围
                        foreach (var range in g.ranges)
                        {
                            if (string.IsNullOrWhiteSpace(range.minFormula) ||
                                string.IsNullOrWhiteSpace(range.maxFormula) ||
                                y.IInRange(
                                    range.minFormula.ComputeFormula(directBlockComputationAlgebra),
                                    range.maxFormula.ComputeFormula(directBlockComputationAlgebra)
                                ))
                            {
                                AddBlockForAreas(g.id, x, y, g.areas);
                            }
                        }
                    }
                }
            }
        }

        public void GeneratePerlinBlocks()
        {
            foreach (var perlinBlock in perlinBlocks)
            {
                var perlin = perlinBlock.perlin;

                //Key是x轴, Value是对应的噪声值
                List<Vector2Int> noises = new();
                int lowestNoise = int.MaxValue;
                int highestNoise = int.MaxValue;

                for (int x = minPoint.x; x < maxPoint.x; x++)
                {
                    var xSample = (x + regionGeneration.actualSeed / 1000f) / perlin.fluctuationFrequency;
                    var noise = (int)(Mathf.PerlinNoise1D(xSample) * perlin.fluctuationHeight);

                    noises.Add(new(x, noise));

                    //最低/最高值为 int.MaxValue 代表的是最低/最高值还未被赋值过     
                    if (lowestNoise == int.MaxValue || noise < lowestNoise)
                        lowestNoise = noise;
                    if (highestNoise == int.MaxValue || noise > highestNoise)
                        highestNoise = noise;
                }

                var startY = (int)perlin.startYFormula.ComputeFormula(directBlockComputationAlgebra);

                //遍历每一个噪声点
                foreach (var noise in noises)
                {
                    //如果 noise.y == 6, 那就要从 0 开始向上遍历, 把方块一个个放出来, 一共七个方块 (0123456)
                    for (int i = 0; i < noise.y + 1; i++)
                    {
                        /* -------------------------------- 接下来是添加方块 -------------------------------- */
                        foreach (var block in perlin.blocks)
                        {
                            var algebra = GetPerlinFormulaAlgebra(0, noise.y, minPoint.y, surface, maxPoint.y);
                            var min = (int)block.minFormula.ComputeFormula(algebra);
                            var max = (int)block.maxFormula.ComputeFormula(algebra);

                            if (i >= min && i <= max)
                            {
                                AddBlock(block.block, noise.x, startY + i, block.isBackground);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateHighestPointFunction()
        {
            foreach (var oneAdded in blockAdded)
            {
                var pos = oneAdded.pos;
                var isBackground = oneAdded.isBackground;

                var dic = isBackground ? backgroundHighestPointFunction : wallHighestPointFunction;

                //如果该 x 值没有被记录过, 那就记录该 x 值
                if (!dic.ContainsKey(pos.x))
                    dic.Add(pos.x, pos.y);
                else if (dic[pos.x] < pos.y)  //如果该 x 值已经被记录过, 但是记录的 y 值比当前 y 值要小, 那就更新记录的 y 值
                    dic[pos.x] = pos.y;
            }
        }

        public void GeneratePostProcessBlocks()
        {
            //先获取地面最高点
            UpdateHighestPointFunction();

            //遍历每个后处理方块
            foreach (var g in postProcessBlocks)
            {
                //遍历每个点
                for (int x = minPoint.x; x < maxPoint.x; x++)
                {
                    for (int y = minPoint.y; y < maxPoint.y; y++)
                    {
                        //概率抽取
                        if (!Tools.Prob100(g.rules.probability, regionGeneration.random))
                            continue;

                        //检查依赖方块
                        if (!string.IsNullOrWhiteSpace(g.attached.blockId) && (regionGeneration.region.GetBlock(centerPoint.x + x + g.attached.offset.x, centerPoint.y + y + g.attached.offset.y, g.attached.isBackground).save?.blockId) != g.attached.blockId)
                            continue;

                        //计算出代数
                        if (!wallHighestPointFunction.TryGetValue(x, out int highestY)) highestY = 0;
                        var formulaAlgebra = new FormulaAlgebra()
                                        {
                                            {
                                                "@bottom", minPoint.y
                                            },
                                            {
                                                "@surface", surface
                                            },
                                            {
                                                "@top", maxPoint.y
                                            },
                                            {
                                                "@highest_point", highestY
                                            }
                                        };


                        //遍历所有范围
                        foreach (var range in g.ranges)
                        {
                            if (string.IsNullOrWhiteSpace(range.minFormula) ||
                                string.IsNullOrWhiteSpace(range.maxFormula) ||
                                y.IInRange(
                                    range.minFormula.ComputeFormula(formulaAlgebra),
                                    range.maxFormula.ComputeFormula(formulaAlgebra)
                                ))
                            {
                                AddBlockForAreas(g.id, x, y, g.areas);
                            }
                        }
                    }
                }
            }
        }

        public void GenerateStructures()
        {
            if (biome.structures?.Length > 0)
            {
                //全部生成完后再生成结构
                for (int x = minPoint.x; x < maxPoint.x; x++)
                {
                    for (int y = minPoint.y; y < maxPoint.y; y++)
                    {
                        //当前遍历到的点
                        Vector2Int pos = new(x, y);

                        foreach (var l in biome.structures)
                        {
                            if (Tools.Prob100(l.structure.probability, regionGeneration.random))
                            {
                                //检查空间是否足够
                                if (l.structure.mustEnough)
                                {
                                    foreach (var fixedBlock in l.structure.fixedBlocks)
                                    {
                                        int tempPosX = pos.x + fixedBlock.offset.x + centerPoint.x;
                                        int tempPosY = pos.y + fixedBlock.offset.y + centerPoint.y;

                                        if (regionGeneration.region.GetBlock(tempPosX, tempPosY, fixedBlock.isBackground).location != null)
                                        {
                                            goto stopGeneration;
                                        }
                                    }
                                }

                                //检查是否满足所有需求
                                foreach (var require in l.structure.require)
                                {
                                    int tempPosX = pos.x + require.offset.x + centerPoint.x;
                                    int tempPosY = pos.y + require.offset.y + centerPoint.y;

                                    if (regionGeneration.region.GetBlock(tempPosX, tempPosY, require.isBackground).save?.blockId != require.blockId)
                                    {
                                        goto stopGeneration;
                                    }
                                }

                                //如果可以就继续
                                foreach (var fixedBlock in l.structure.fixedBlocks)
                                {
                                    var tempPosX = pos.x + fixedBlock.offset.x;
                                    var tempPosY = pos.y + fixedBlock.offset.y;

                                    AddBlock(fixedBlock.blockId, tempPosX, tempPosY, fixedBlock.isBackground);
                                }

                            stopGeneration:
                                continue;
                            }
                        }
                    };
                }
            }
        }




        public static Action<RegionGeneration> OreClumpsGeneration = (generation) =>
        {
            //* 这个算法实质是检测矿石的位置，把单个矿石变成矿石团块
            foreach (var save in generation.region.blocks)
            {
                var blockData = ModFactory.CompareBlockData(save.blockId);

                //筛选出有效方块
                if (blockData == null)
                    continue;

                //筛选出矿石
                if (!blockData.TryGetValueTagToInt("ori:ore", out var tagValue, 10))
                    continue;

                //缓存原先的 Locations
                var locationsTemp = save.locations.ToArray();

                //遍历所有 Locations
                foreach (var location in locationsTemp)
                {
                    List<Vector3Int> oreLocations = new() { Vector3Int.zero };

                    // tagValue 决定了团块有多大
                    var clumpSize = generation.random.Next(tagValue.tagValue / 3, tagValue.tagValue + 1);

                    //随机抽取 clumpSize 个位置
                    for (int i = 0; i < clumpSize; i++)
                    {
                        //在原有的方块周围随机抽取一个位置 (3x3 范围)
                        var randomLocation = oreLocations.Extract(generation.random);
                        var randomPos = new Vector3Int(
                                            randomLocation.x + generation.random.Next(-1, 2),
                                            randomLocation.y + generation.random.Next(-1, 2));
                        //防止重复添加
                        if (!oreLocations.Contains(randomPos))
                            oreLocations.Add(randomPos);
                    }

                    //把矿石正式添加到岛屿中
                    oreLocations.Remove(Vector3Int.zero);
                    foreach (var loc in oreLocations)
                    {
                        int newX = location.x + loc.x;
                        int newY = location.y + loc.y;

                        //只有石头会被替换
                        if (generation.region.TryGetBlock(newX, newY, save.isBg, out var stone) && stone.save.blockId == BlockID.Stone)
                        {
                            stone.save.RemoveLocation(newX, newY);
                            save.AddLocation(newX, newY);
                        }
                    }
                }
            }
        };

        public static Action<IslandGeneration> LootGeneration = (islandGeneration) =>
        {
            //遍历每个最高点
            foreach (var highest in islandGeneration.wallHighestPointFunction)
            {
                Vector2Int lootBlockPos = new(highest.Key, highest.Value + 1);

                /* ----------------------------------- 中心空岛特别战利品 ----------------------------------- */
                if (islandGeneration.regionGeneration.index == Vector2Int.zero)
                {
                    //远程市场
                    if (lootBlockPos.x == -6)
                    {
                        islandGeneration.AddBlock(BlockID.RemoteMarket, lootBlockPos.x, lootBlockPos.y, false, null);
                    }
                    //木桶
                    else if (lootBlockPos.x == 15)
                    {
                        JToken[] group = new JToken[28];

                        //填充每个栏位
                        for (int i = 0; i < group.Length; i++)
                        {
                            /* --------------------------------- 抽取一个物品 --------------------------------- */
                            Item item = null;

                            switch (i)
                            {
                                case 0:
                                    item = ModFactory.CompareItem(BlockID.Dirt).DataToItem();
                                    item.count = 30;
                                    break;

                                case 1:
                                    item = ModFactory.CompareItem(ItemID.SportsVest).DataToItem();
                                    break;

                                case 2:
                                    item = ModFactory.CompareItem(ItemID.SportsShorts).DataToItem();
                                    break;

                                case 3:
                                    item = ModFactory.CompareItem(ItemID.Sneakers).DataToItem();
                                    break;

                                case 4:
                                    item = ModFactory.CompareItem(BlockID.OnionCrop).DataToItem();
                                    item.count = 3;
                                    break;
                            }

                            /* ---------------------------------- 填充物品 ---------------------------------- */
                            if (item != null)  //如果获取失败了, 这个格子也会为空
                            {
                                group[i] = JToken.FromObject(item);
                            }
                        }

                        JObject jo = new();

                        jo.AddObject("ori:container");
                        jo["ori:container"].AddObject("items");
                        jo["ori:container"]["items"].AddArray("array", group);

                        islandGeneration.AddBlock(BlockID.Barrel, lootBlockPos.x, lootBlockPos.y, false, jo.ToString(Formatting.None));
                    }
                }

                //如果位置被占用就跳过
                if (islandGeneration.blockAdded.Any(added => added.pos == lootBlockPos && !added.isBackground))
                    continue;

                /* ----------------------------------- 木箱 ----------------------------------- */
                if (Tools.Prob100(1.5f, islandGeneration.regionGeneration.random))
                {
                    JToken[] group = new JToken[21];

                    Parallel.For(0, group.Length, i =>
                    {
                        //每个格子都有 >=65% 的概率为空
                        if (Tools.Prob100(65, islandGeneration.regionGeneration.random))
                            return;

                        Mod mod = null;
                        while (mod == null)
                        {
                            mod = ModFactory.mods.Extract(islandGeneration.regionGeneration.random);
                            if (mod.items.Count == 0)
                                mod = null;
                        }

                        //从模组中抽取一种魔咒
                        static string ExtractSpell(Random random)
                        {
                            Spell spell = null;
                            Mod mod = null;

                            //获取模组
                            while (mod == null)
                            {
                                mod = ModFactory.mods.Extract(random);
                                if (mod.spells.Count == 0)
                                    mod = null;
                            }

                            //从获得到的模组中抽取一种魔咒
                            for (var inner = 0; inner < mod.spells.Count / 5 + 1; inner++)  //最多尝试抽取 1/5 的魔咒
                            {
                                spell = mod.spells.Extract(random);

                                if (spell == null)
                                    continue;

                                //如果是木箱的战利品就通过
                                if (spell.GetTag("ori:loot.wooden_chest").hasTag)
                                    break;
                                else
                                    spell = null;
                            }

                            return spell?.id;
                        }

                        ItemData item = null;
                        for (var inner = 0; inner < mod.items.Count / 5 + 1; inner++)  //最多尝试抽取 1/5 的物品
                        {
                            item = mod.items.Extract(islandGeneration.regionGeneration.random);

                            if (item == null)
                                continue;

                            //如果是木箱的战利品就通过
                            if (item.GetTag("ori:loot.wooden_chest").hasTag)
                                break;
                            else
                                item = null;
                        }
                        if (item != null)  //如果获取失败了, 这个格子也会为空
                        {
                            var extendedItem = item.DataToItem();

                            if (item.id == ItemID.ManaStone)
                            {
                                var jo = new JObject();
                                jo.AddObject("ori:mana_container");
                                jo["ori:mana_container"].AddProperty("total_mana", islandGeneration.regionGeneration.random.Next(0, 100));
                                jo.AddObject("ori:spell_container");
                                jo["ori:spell_container"].AddProperty("spell", ExtractSpell(islandGeneration.regionGeneration.random));
                                extendedItem.customData = jo;
                            }
                            else if (item.id == ItemID.SpellManuscript)
                            {
                                var jo = new JObject();
                                jo.AddObject("ori:spell_container");
                                jo["ori:spell_container"].AddProperty("spell", ExtractSpell(islandGeneration.regionGeneration.random));
                                extendedItem.customData = jo;
                            }

                            group[i] = JToken.FromObject(extendedItem);
                        }
                    });

                    JObject jo = new();

                    jo.AddObject("ori:container");
                    jo["ori:container"].AddObject("items");
                    jo["ori:container"]["items"].AddArray("array", group);

                    islandGeneration.AddBlock(BlockID.WoodenChest, lootBlockPos.x, lootBlockPos.y, false, jo.ToString(Formatting.None));
                }
            }
        };

        public static Action<RegionGeneration> EntitiesGeneration = (generation) =>
        {
            if (generation.index == Vector2Int.zero)
            {
                //如是初始区域, 生成 Nick
                var nickSave = EntitySave.Create(EntityID.Nick);
                nickSave.pos = generation.region.spawnPoint + new Vector2Int(10, 0);
                nickSave.saveId = Tools.randomGUID;
                generation.region.AddEntity(nickSave);


                //如是初始区域, 生成商人
                var traderSave = EntitySave.Create(EntityID.Trader);
                traderSave.pos = generation.region.spawnPoint + new Vector2Int(0, 0);
                traderSave.saveId = Tools.randomGUID;
                generation.region.AddEntity(traderSave);
            }

            if (generation.region.biomeId == BiomeID.GrasslandFighting)
            {
                //如是初始区域, 生成 Nick
                var grasslandGuardSave = EntitySave.Create(EntityID.GrasslandGuard);
                grasslandGuardSave.pos = generation.region.spawnPoint + new Vector2Int(0, 8);
                grasslandGuardSave.saveId = Tools.randomGUID;
                generation.region.AddEntity(grasslandGuardSave);
            }
        };



        public IslandGeneration(RegionGeneration regionGeneration, Vector2Int centerPoint)
        {
            this.regionGeneration = regionGeneration;
            this.centerPoint = centerPoint;
            this.blockAdded = new();

            //决定群系
            biome = ModFactory.CompareBiome(regionGeneration.region.biomeId);

            //决定岛的大小
            size = new(regionGeneration.random.Next((int)(Region.placeVec.x * biome.minScale.x), (int)(Region.placeVec.x * biome.maxScale.x)).IRange(10, Region.place.x),
                                regionGeneration.random.Next((int)(Region.placeVec.y * biome.minScale.y), (int)(Region.placeVec.y * biome.maxScale.y)).IRange(10, Region.place.y));
            if (size.x % 2 != 0) size.x++;
            if (size.y % 2 != 0) size.y++;

            //边际 (左下右上)
            maxPoint = new(size.x / 2, size.y / 2);
            minPoint = -maxPoint;

            //使空岛有 y轴 偏移
            yOffset = regionGeneration.random.Next(15, 35);
            surface = maxPoint.y - yOffset;
            surfaceExtra1 = surface + 1;

            directBlockComputationAlgebra = GetIslandGenerationFormulaAlgebra(minPoint.y, surface, maxPoint.y);






            //获取方块生成规则
            List<BiomeData_Block> directBlocksTemp = new();
            List<BiomeData_Block> perlinBlocksTemp = new();
            List<BiomeData_Block> postProcessBlocksTemp = new();
            List<BiomeData_Block> unexpectedBlocksTemp = new();

            foreach (var g in biome.blocks)
            {
                if (g == null)
                {
                    Debug.Log($"生成岛屿时发现了一个空的方块生成规则");
                    continue;
                }
                if (!g.initialized)
                {
                    Debug.Log($"生成岛屿时发现了一个未初始化的方块生成规则 {g.id}");
                    continue;
                }

                if (g.type == "direct")
                    directBlocksTemp.Add(g);
                else if (g.type == "perlin")
                    perlinBlocksTemp.Add(g);
                else if (g.type == "post_process")
                    postProcessBlocksTemp.Add(g);
                else
                    unexpectedBlocksTemp.Add(g);
            }

            directBlocks = directBlocksTemp.ToArray();
            perlinBlocks = perlinBlocksTemp.ToArray();
            postProcessBlocks = postProcessBlocksTemp.ToArray();
            unexpectedBlocks = unexpectedBlocksTemp.ToArray();
        }
    }
}