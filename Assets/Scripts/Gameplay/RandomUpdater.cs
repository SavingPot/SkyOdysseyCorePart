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
        static EntityData[] entitiesSummonable;





        public static void Bind(string id, float probability, Action action)
        {
            foreach (var update in updates)
            {
                if (update.id == id)
                {
                    Debug.LogError($"随机更新更新 {id} 已存在");
                    return;
                }
            }

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

        internal static void Init()
        {
            #region 生成实体

            List<EntityData> entitiesSummonableTemp = new();

            //将符合条件的实体添加到预选列表
            ModFactory.globalEntities.ForEach(e =>
            {
                if (e.behaviourType == null)
                {
                    Debug.LogWarning($"{MethodGetter.GetLastAndCurrentMethodPath()}: 实体 {e.id} 的 {nameof(EntityData.behaviourType)} 未正确设置");
                    return;
                }

                //如果没有NotSummonableAttribute特性那么通过
                if (!AttributeGetter.MatchAttribute(e.behaviourType, NotSummonableAttributeType))
                {
                    entitiesSummonableTemp.Add(e);
                }
            });

            entitiesSummonable = entitiesSummonableTemp.ToArray();




            Bind("ori:summon_entities", 6, () =>
            {
                //白天概率为晚上的 2/3
                if (GTime.IsInTime(GTime.time24Format, 6, 18) && UnityEngine.Random.Range(0, 3) == 2)
                    return;

                //防止意外和卡顿
                if (EntityCenter.all.Count >= 100 || !Map.HasInstance() || Map.instance.chunks.Count == 0)
                    return;



                //将符合条件的实体添加到预选列表
                List<EntityData> entities = new();
                foreach (var entity in entitiesSummonable)
                {
                    //如果 符合几率 没有NotSummonableAttribute特性 时间符合 就添加至预选列表
                    if (Tools.Prob100(entity.summon.defaultProbability) &&
                            GTime.IsInTime(GTime.time24Format, entity.summon.timeEarliest, entity.summon.timeLatest))
                    {
                        entities.Add(entity);
                    }
                }



                if (entities.Count > 0)
                {
                    for (int tryCount = 0; tryCount < entities.Count - 1; tryCount++)
                    {
                        //随机抽取一个实体并生成
                        EntityData entity = entities.Extract(RandomUpdateRandom);

                        if (EntitySummonPos(entity, out var summonPos))
                        {
                            //生成实体
                            GM.instance.SummonEntity(summonPos, entity.id, Tools.randomGUID);
                        }

                        entities.Remove(entity);
                    }
                }
            });
            #endregion

            #region 环境音效
            Bind("ori:ambient_audio", 10, () =>
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
                    AudioData audio = audios.Extract(RandomUpdateRandom);

                    GAudio.Play(audio.id, null);
                }
            });
            #endregion

            #region 天气
            Bind("ori:weather_change", 0.09f, ChangeWeatherRandomly);
            #endregion
        }

        static readonly Type NotSummonableAttributeType = typeof(NotSummonableAttribute);

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

        public static System.Random RandomUpdateRandom = new();
        public static bool EntitySummonPos(EntityData e, out Vector2 pos)
        {
            //最多尝试 24 个区块
            for (byte chunkTime = 0; chunkTime < 24; chunkTime++)
            {
                //抽取一个区块
                Chunk chunk = Map.instance.chunks.Extract(RandomUpdateRandom);

                //检查实体的群系要求
                if (!string.IsNullOrEmpty(e.summon.biome) && GFiles.world.TryGetRegion(chunk.regionIndex, out Region region) && e.summon.biome != region.biomeId)
                {
                    continue;
                }

                //寻找合适的方块 (一个区块内最多尝试 20 个方块)
                for (byte blockTime = 0; blockTime < 20; blockTime++)
                {
                    //抽取一个方块并检查
                    Block block = chunk.wallBlocks.Extract(RandomUpdateRandom);

                    //TODO: 在视野范围内也允许生成，但是 Entity.AfterInitialization 中会检测是否在视野范围内，如果是那么生成一个粒子系统
                    //TODO: Particle System 在 GameScene 中已存在
                    if (block != null &&
                        !Map.instance.HasBlock(block.pos + Vector2Int.up, false) &&
                        chunk.IsInRegionBound(block.pos, 10) &&
                        !Tools.instance.IsInView2DFaster(block.pos))
                    {
                        pos = block.pos + new Vector2Int(0, 2);
                        return true;
                    }
                }
            }

            pos = Vector2.zero;
            return false;
        }



        public static void ChangeWeatherRandomly()
        {
            WeatherData newWeather = null;

            //最多抽取 10 次，以找到一个不同的天气
            for (int i = 0; i < 10; i++)
            {
                newWeather = GWeather.weathers.Extract(RandomUpdateRandom);

                if (GWeather.weatherId != newWeather.id)
                    break;
            }

            //检查天气
            if (newWeather == null || newWeather.id == GWeather.weatherId)
            {
                Debug.LogError("天气切换失败");
                return;
            }

            //切换天气
            GWeather.SetWeather(newWeather.id);
            Debug.Log("天气切换至 " + newWeather.id);
        }
    }
}
