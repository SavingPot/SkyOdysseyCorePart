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

        static Type NotSummonableAttributeType = typeof(NotSummonableAttribute);

        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            #region 生成实体
            Bind("ori:summon_entities", 4, () =>
            {
                if (EntityCenter.all.Count >= 100 || !Map.HasInstance() || Map.instance.chunks.Count == 0)
                    return;

                List<EntityData> entities = new();

                //将符合条件的实体添加到预选列表
                ModFactory.mods.ForEach(m => m.entities.ForEach(e =>
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
                        EntityData entity = entities.Extract();
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
                ModFactory.mods.ForEach(m => m.audios.ForEach(a =>
                {
                    //如果不为空且是环境音效并符合时间要求, 就添加至预选列表
                    if (a != null && a.audioMixerType == AudioMixerType.Ambient && GTime.IsInTime(GTime.time24Format, a.earliestTime, a.latestTime))
                        audios.Add(a);
                }));

                if (audios.Count > 0)
                {
                    //随机抽取一个实体并生成
                    AudioData audio = audios.Extract();

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

        public static Func<EntityData, Vector2> EntitySummonPos = e =>
        {
            //最多尝试 24 个区块
            for (byte cTime = 0; cTime < 24; cTime++)
            {
                //抽取一个区块
                Chunk chunk = Map.instance.chunks.Extract();

                if (!string.IsNullOrEmpty(e.summon.biome) && GFiles.world.TryGetSandbox(chunk.sandboxIndex, out Sandbox sb) && e.summon.biome != sb.biome)
                {
                    continue;
                }

                //寻找合适的方块 (一个区块内最多尝试 32 个方块)
                for (byte bTime = 0; bTime < 32; bTime++)
                {
                    //抽取一个方块并检查
                    Block block = chunk.blocks.Extract();

                    if (block && !block.isBackground && !Map.instance.GetBlock(block.pos + Vector2Int.up, false) && !Tools.instance.IsInView2D(block.pos.To2()))
                    {
                        return block.pos + new Vector2Int(0, 2);
                    }
                }
            }

            return new(float.PositiveInfinity, float.PositiveInfinity);
        };
    }
}
