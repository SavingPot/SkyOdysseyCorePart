using GameCore.High;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public struct RandomUpdateClass
    {
        public string id;
        public float probability;
        public Action action;

        public RandomUpdateClass(string id, float probability, Action action)
        {
            this.id = id;
            this.probability = probability;
            this.action = action;
        }
    }

    /// <summary>
    /// 用于执行在游戏场景中的随机更新功能
    /// </summary>
    public static class RandomUpdater
    {
        //随机更新只在服务器进行, 无需同步至客户端
        [LabelText("随机更新几率")] public static byte randomUpdateProbability = 2;

        public static List<RandomUpdateClass> updates = new();





        public static void Bind(string id, float probability, Action action)
        {
            updates.Add(new(id, probability, action));
        }

        public static void Unbind(string id)
        {
            for (int i = 0; i < updates.Count; i++)
            {
                if (updates[i].id == id)
                {
                    updates.RemoveAt(i);
                    return;
                }
            }

            Debug.LogWarning($"未找到指定更新 {id}");
        }

        static readonly Type NotSummonableAttributeType = typeof(NotSummonableAttribute);

        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            #region 生成实体
            Bind("ori:summon_entities", 5, () =>
            {
                if (EntityCenter.all.Count >= 100 || !Map.HasInstance() || Map.instance.chunks.Count == 0)
                    return;

                List<EntityData> entities = new();

                //将符合条件的实体添加到预选列表
                Array.ForEach(ModFactory.mods, m => m.entities.ForEach(e =>
                {
                    if (e.behaviourType == null)
                    {
                        Debug.LogWarning($"{MethodGetter.GetLastAndCurrentMethodPath()}: 实体 {e.id} 的 {nameof(EntityData.behaviourType)} 未正确设置");
                        return;
                    }

                    //如果 符合几率 没有NotSummonableAttribute特性 时间符合 就添加至预选列表
                    else if (Tools.Prob100(e.summon.defaultProbability) &&
                            !AttributeGetter.MatchAttribute(e.behaviourType, NotSummonableAttributeType) &&
                            GTime.IsInTime(GTime.time24Format, e.summon.timeEarliest, e.summon.timeLatest))
                    {
                        entities.Add(e);
                    }
                }));

                if (entities.Count > 0)
                {
                    for (int tryCount = 0; tryCount < entities.Count - 1; tryCount++)
                    {
                        //随机抽取一个实体并生成
                        EntityData entity = entities.Extract(EntitySummonPosRandom);
                        Vector2 pos = EntitySummonPos(entity);

                        entities.Remove(entity);

                        if (!float.IsInfinity(pos.x) && !float.IsInfinity(pos.y))
                        {
                            //生成实体
                            GM.instance.SummonEntity(pos, entity.id, Tools.randomGUID);
                        }
                    }
                }
            });
            #endregion

            #region 环境音效
            Bind("ori:ambient_audio", 6, () =>
            {
                List<AudioData> audios = new();
                Array.ForEach(ModFactory.mods, m => m.audios.ForEach(a =>
                {
                    //如果不为空且是环境音效并符合时间要求, 就添加至预选列表
                    if (a != null && a.audioMixerType == AudioMixerType.Ambient && GTime.IsInTime(GTime.time24Format, a.earliestTime, a.latestTime))
                        audios.Add(a);
                }));

                if (audios.Count > 0)
                {
                    //随机抽取一个实体并生成
                    AudioData audio = audios.Extract(EntitySummonPosRandom);

                    GAudio.Play(audio.id);
                }
            });
            #endregion
        }

        /// <summary>
        /// 当随机更新时
        /// </summary>
        public static void RandomUpdate()
        {
            for (int i = 0; i < updates.Count; i++)
            {
                RandomUpdateClass update = updates[i];

                if (Tools.Prob100(update.probability))
                {
                    update.action();
                }
            }
        }

        public static System.Random EntitySummonPosRandom = new();
        public static Func<EntityData, Vector2> EntitySummonPos = e =>
        {
            //TODO: 优化性能
            //最多尝试 24 个区块
            for (byte cTime = 0; cTime < 24; cTime++)
            {
                //抽取一个区块
                Chunk chunk = Map.instance.chunks.Extract(EntitySummonPosRandom);

                if (!string.IsNullOrEmpty(e.summon.region) && GFiles.world.TryGetRegion(chunk.regionIndex, out Region region) && e.summon.region != region.regionTheme)
                {
                    continue;
                }

                //寻找合适的方块 (一个区块内最多尝试 32 个方块)
                for (byte bTime = 0; bTime < 32; bTime++)
                {
                    //抽取一个方块并检查
                    Block block = chunk.blocks.Extract(EntitySummonPosRandom);

                    if (block != null &&
                        !block.isBackground &&
                        block.data.id != BlockID.Barrel &&
                        !Map.instance.HasBlock(block.pos + Vector2Int.up, false) &&
                        //TODO: 把 MapPosToChunkPos 打包到 PosConvert
                        Math.Abs(block.pos.x - Region.GetMiddleX(chunk.regionIndex)) < Region.chunkCount * Chunk.blockCountPerAxis / 2 - 5 &&
                        Math.Abs(block.pos.y - Region.GetMiddleY(chunk.regionIndex)) < Region.chunkCount * Chunk.blockCountPerAxis / 2 - 5 &&
                        !Tools.instance.IsInView2D(block.pos.To2()))
                    {
                        return block.pos + new Vector2Int(0, 2);
                    }
                }
            }

            return new(float.PositiveInfinity, float.PositiveInfinity);
        };
    }
}
