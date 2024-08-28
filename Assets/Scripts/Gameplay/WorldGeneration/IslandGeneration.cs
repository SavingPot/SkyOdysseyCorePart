using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SP.Tools;
using UnityEngine;

namespace GameCore
{
    public abstract class IslandGeneration
    {
        protected delegate JObject GetCustomDataDelegate(int blockX, int blockY, bool isBackground);



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

        protected BiomeData_Block[] directBlocks;
        protected BiomeData_Block[] perlinBlocks;
        protected BiomeData_Block[] postProcessBlocks;
        protected BiomeData_Block[] unexpectedBlocks;
        protected BiomeData_Structure[] structures;






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





        public virtual void GenerateDirectBlocks()
        {
            //遍历每个点
            for (int x = minPoint.x; x <= maxPoint.x; x++)
            {
                for (int y = minPoint.y; y <= maxPoint.y; y++)
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

        public virtual void GeneratePerlinBlocks()
        {
            foreach (var perlinBlock in perlinBlocks)
            {
                var perlin = perlinBlock.perlin;

                //Key是x轴, Value是对应的噪声值
                List<Vector2Int> noises = new();
                int lowestNoise = int.MaxValue;
                int highestNoise = int.MaxValue;

                for (int x = minPoint.x; x <= maxPoint.x; x++)
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

        public virtual void GeneratePostProcessBlocks()
        {
            //先获取地面最高点
            UpdateHighestPointFunction();

            //遍历每个后处理方块
            foreach (var g in postProcessBlocks)
            {
                //遍历每个点
                for (int x = minPoint.x; x <= maxPoint.x; x++)
                {
                    for (int y = minPoint.y; y <= maxPoint.y; y++)
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

        public virtual void GenerateStructures()
        {
            //检查是否有结构
            if (structures.Length == 0)
                return;

            for (int x = minPoint.x; x <= maxPoint.x; x++)
            {
                for (int y = minPoint.y; y <= maxPoint.y; y++)
                {
                    foreach (var structure in structures)
                    {
                        //检查条件
                        if (!CheckStructureConditions(x, y, structure))
                            continue;

                        //如果所有条件均满足，则正式开始生成结构
                        GenerateStructure(x, y, structure);
                    }
                }
            }
        }

        protected virtual bool CheckStructureConditions(int x, int y, BiomeData_Structure structure)
        {
            //概率抽取
            if (!Tools.Prob100(structure.structure.probability, regionGeneration.random))
                return false;

            //检查空间是否足够
            if (structure.structure.mustEnough)
            {
                foreach (var fixedBlock in structure.structure.fixedBlocks)
                {
                    int tempPosX = x + fixedBlock.offset.x + centerPoint.x;
                    int tempPosY = y + fixedBlock.offset.y + centerPoint.y;

                    //如果有方块就不能生成
                    if (regionGeneration.region.GetBlock(tempPosX, tempPosY, fixedBlock.isBackground).location != null)
                    {
                        return false;
                    }
                }
            }

            //检查是否满足所有特定需求
            foreach (var require in structure.structure.require)
            {
                int tempPosX = x + require.offset.x + centerPoint.x;
                int tempPosY = y + require.offset.y + centerPoint.y;

                //检测是否是要求的方块
                if (regionGeneration.region.GetBlock(tempPosX, tempPosY, require.isBackground).save?.blockId != require.blockId)
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void GenerateStructure(int x, int y, BiomeData_Structure structure)
        {
            //遍历每个方块
            foreach (var block in structure.structure.fixedBlocks)
            {
                var blockX = x + block.offset.x;
                var blockY = y + block.offset.y;

                AddBlock(block.blockId, blockX, blockY, block.isBackground);
            }

            WriteCustomDataForStructure(x, y, structure);
        }

        protected virtual void WriteCustomDataForStructure(int x, int y, BiomeData_Structure structure)
        {

        }

        //TODo
        protected virtual void WriteBlockCustomDataInStructure(string blockId, int structureX, int structureY, BiomeData_Structure structure, GetCustomDataDelegate GetCustomData)
        {
            foreach (var block in structure.structure.fixedBlocks)
            {
                if (block.blockId != blockId)
                    continue;

                //计算出方块的位置
                var blockX = structureX + block.offset.x;
                var blockY = structureY + block.offset.y;
                var blockIsBackground = block.isBackground;

                //虽然知道一般会是预计的方块，但还是检查一下
                if (!regionGeneration.region.TryGetBlock(block.blockId, blockIsBackground, out var blockSave) ||
                    !blockSave.TryGetLocation(blockX, blockY, out var blockLocation))
                {
                    Debug.LogError($"写入方块自定义数据失败: 方块的位置不正确, 位置 ({blockX}, {blockY}) 应当是 {blockId} 方块, 但实际上是 {blockSave?.blockId} (偏移为 {block.offset.x}, {block.offset.y})");
                    continue;
                }

                //检查成功，写入 customData
                blockLocation.cd = GetCustomData(blockX, blockY, blockIsBackground)?.ToString(Formatting.None);
            }
        }

        protected virtual JObject GenerateLootCustomData(float lootProbability, Func<ItemData, bool> itemCondition, Func<Spell, bool> spellCondition)
        {
            var random = regionGeneration.random;
            var tokens = new JToken[21];

            for (int i = 0; i < tokens.Length; i++)
            {
                //每个格子都有 lootProbability 的概率被填充
                if (!Tools.Prob100(lootProbability, random))
                    continue;



                //尝试抽取一个战利品（就算失败了也不会报错，不会有影响）
                ItemData item = null;
                MethodAgent.TryRun(() => item = ModFactory.GetRandomItem(random, itemCondition));

                //如果获取失败了, 这个格子会为空
                if (item == null)
                    continue;

                var extendedItem = item.DataToItem();

                //为魔咒容器填入魔咒
                if (item.IsSpellContainer())
                {
                    //抽取一种魔咒
                    Spell spell = null;
                    MethodAgent.TryRun(() => spell = ModFactory.GetRandomSpell(random, spellCondition));

                    //初始化 customData 并写入魔能和魔咒
                    var customData = new JObject();
                    ISpellContainer.SetSpell(customData, spell?.id);

                    //把 customData 写入物品
                    extendedItem.customData = customData;
                }

                tokens[i] = JToken.FromObject(extendedItem);
            };

            JObject jo = new();
            jo.AddObject("ori:container");
            jo["ori:container"].AddObject("items");
            jo["ori:container"]["items"].AddArray("array", tokens);

            //返回 customData
            return jo;
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
                    //木桶
                    if (lootBlockPos.x == 15)
                    {
                        JToken[] group = new JToken[28];

                        //填充每个栏位
                        for (int i = 0; i < group.Length; i++)
                        {
                            //抽取一个物品
                            Item item = i switch
                            {
                                0 => ModFactory.CompareItem(BlockID.Dirt).DataToItem().SetCount(30),
                                1 => ModFactory.CompareItem(ItemID.SportsVest).DataToItem(),
                                2 => ModFactory.CompareItem(ItemID.SportsShorts).DataToItem(),
                                3 => ModFactory.CompareItem(ItemID.Sneakers).DataToItem(),
                                4 => ModFactory.CompareItem(BlockID.OnionCrop).DataToItem().SetCount(3),
                                _ => null
                            };

                            //填充物品
                            if (item != null)  //如果获取失败了, 这个格子也会为空
                            {
                                group[i] = JToken.FromObject(item);
                            }
                        }

                        //把物品写入 customData
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

            //决定岛的大小 (偶数)
            size = new(Mathf.Clamp(regionGeneration.random.Next((int)(Region.placeVec.x * biome.minScale.x), (int)(Region.placeVec.x * biome.maxScale.x)), 10, Region.place.x),
                        Mathf.Clamp(regionGeneration.random.Next((int)(Region.placeVec.y * biome.minScale.y), (int)(Region.placeVec.y * biome.maxScale.y)), 10, Region.place.y));
            if (size.x % 2 != 0) size.x++;
            if (size.y % 2 != 0) size.y++;

            //边际 (左下右上)
            maxPoint = new(size.x / 2, size.y / 2);
            minPoint = -maxPoint;

            //使空岛有 y轴 偏移
            yOffset = regionGeneration.random.Next(15, 30);
            surface = maxPoint.y - yOffset;
            surfaceExtra1 = surface + 1;

            directBlockComputationAlgebra = GetIslandGenerationFormulaAlgebra(minPoint.y, surface, maxPoint.y);






            //获取方块生成规则
            List<BiomeData_Block> directBlocksTemp = new();
            List<BiomeData_Block> perlinBlocksTemp = new();
            List<BiomeData_Block> postProcessBlocksTemp = new();
            List<BiomeData_Block> unexpectedBlocksTemp = new();

            if (biome.blocks != null)
            {
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

                    if (IsValidDirectBlock(g))
                        directBlocksTemp.Add(g);
                    else if (IsValidPerlinBlock(g))
                        perlinBlocksTemp.Add(g);
                    else if (IsValidPostProcessBlock(g))
                        postProcessBlocksTemp.Add(g);
                    else
                        unexpectedBlocksTemp.Add(g);
                }
            }

            directBlocks = directBlocksTemp.ToArray();
            perlinBlocks = perlinBlocksTemp.ToArray();
            postProcessBlocks = postProcessBlocksTemp.ToArray();
            unexpectedBlocks = unexpectedBlocksTemp.ToArray();
            if (unexpectedBlocks.Length > 0) Debug.LogWarning($"生成岛屿时发现了 {unexpectedBlocks.Length} 个未知的方块生成规则, 它们将被忽略:\n{string.Join(",\n", unexpectedBlocks.Select(b => b.id))}");



            //获取结构
            List<BiomeData_Structure> structuresTemp = new();
            if (biome.structures != null)
            {
                foreach (var structure in biome.structures)
                {
                    if (structure == null)
                    {
                        Debug.Log($"生成岛屿时发现了一个空的结构生成规则");
                        continue;
                    }

                    if (IsValidStructure(structure))
                        structuresTemp.Add(structure);
                }
            }
            structures = structuresTemp.ToArray();
        }

        public virtual bool IsValidDirectBlock(BiomeData_Block block) => block.type == "direct";
        public virtual bool IsValidPerlinBlock(BiomeData_Block block) => block.type == "perlin";
        public virtual bool IsValidPostProcessBlock(BiomeData_Block block) => block.type == "post_process";
        public virtual bool IsValidStructure(BiomeData_Structure structure) => true;
    }
}