using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SP.Tools;
using SP.Tools.Unity;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public sealed class World
    {
        public static string GetImagePath(string worldPath) => Path.Combine(GetDisplayCachePath(worldPath), "image.png");
        public static string GetBasicDataPath(string worldPath) => Path.Combine(worldPath, "basic_data.json");
        public static string GetRegionDataPath(string worldPath) => Path.Combine(worldPath, "region_data.json");
        public static string GetPlayerDataPath(string worldPath) => Path.Combine(worldPath, "player_data.json");
        public static string GetLordDataPath(string worldPath) => Path.Combine(worldPath, "lord_data.json");
        public static string GetCachePath(string worldPath) => Path.Combine(worldPath, "caches");
        public static string GetDisplayCachePath(string worldPath) => Path.Combine(GetCachePath(worldPath), "display");



        public static Action ApplyBlockData = () => { };
        public static Action ApplyLaborData = () => { };
        public static Action OnSaveAllDataToFiles = () => { Debug.Log("保存了所有数据至文件"); };



        public string worldPath => basicData.worldPath;
        public string worldImagePath => GetImagePath(worldPath);
        public string worldRegionDataPath => GetRegionDataPath(worldPath);



        public WorldBasicData basicData = new();
        public List<Region> regionData = new();
        public List<PlayerSave> playerSaves = new();
        public List<Lord> lords = new();


        public void CreateLord(string id, Vector2Int manorIndex, int coins = 0)
        {
            lords.Add(new()
            {
                id = id,
                manorIndex = manorIndex,
                coins = coins,
                laborData = new()
            });
        }

        public void CreateLord(Lord lord)
        {
            lords.Add(lord);
        }

        public Lord FindLord(string lordId)
        {
            foreach (var lord in lords)
                if (lord.id == lordId)
                    return lord;

            return null;
        }

        public bool TryFindLord(string lordId, out Lord lord)
        {
            lord = FindLord(lordId);
            return lord != null;
        }

        public static void ApplyEntityData()
        {
            List<Entity> entities = EntityCenter.all;
            foreach (Entity entity in entities)
            {
                entity.WriteDataToWorldSave();
            }
        }

        public void ApplyData()
        {
            //将时间写入
            basicData.time = GTime.time;
            basicData.isAM = GTime.isMorning;
            basicData.totalTime = GTime.totalTime;
            basicData.weather = GWeather.weatherId;

            //将方块数据写入
            ApplyBlockData();

            //应用实体数据
            ApplyEntityData();

            //应用劳动数据
            ApplyLaborData();
        }

        public void WriteDataToFiles()
        {
            IOTools.CreateDirsIfNone(GetCachePath(worldPath), GetDisplayCachePath(worldPath));
            GFiles.SaveFileJson(GetBasicDataPath(worldPath), basicData, false, true);
            GFiles.SaveFileJson(GetRegionDataPath(worldPath), regionData, false, false);
            GFiles.SaveFileJson(GetPlayerDataPath(worldPath), playerSaves, false, true);
            GFiles.SaveFileJson(GetLordDataPath(worldPath), lords, false, true);
        }



        public static World Load(string dirPath)
        {
            WorldBasicData basicData = JsonUtils.LoadTypeFromJsonPath<WorldBasicData>(GetBasicDataPath(dirPath));
            List<Region> regionData = JsonUtils.LoadTypeFromJsonPath<List<Region>>(GetRegionDataPath(dirPath));
            List<PlayerSave> playerData = JsonUtils.LoadTypeFromJsonPath<List<PlayerSave>>(GetPlayerDataPath(dirPath));
            List<Lord> lords = JsonUtils.LoadTypeFromJsonPath<List<Lord>>(GetLordDataPath(dirPath)) ?? new();

            return new(basicData, regionData, playerData, lords);
        }



        public World(int seed, string worldName)
        {
            basicData.seed = seed;
            basicData.gameVersion = GInit.gameVersion;
            basicData.worldName = worldName;
        }

        public World(WorldBasicData basicData, List<Region> regionData, List<PlayerSave> playerSaves, List<Lord> lords)
        {
            this.basicData = basicData;
            this.regionData = regionData;
            this.playerSaves = playerSaves;
            this.lords = lords;
        }

        public void Modify()
        {

        }

        public void AddRegion(Region region)
        {
            //检查重复区域
            for (int i = 0; i < regionData.Count; i++)
            {
                var temp = regionData[i];

                //如果有就合并数据 (以 参数传入的区域(region)为主)
                if (temp.index == region.index)
                {
                    //遍历每一个方块并添加
                    foreach (var saveTemp in temp.blocks)
                    {
                        foreach (var locationTemp in saveTemp.locations)
                        {
                            region.AddPos(saveTemp.blockId, locationTemp.x, locationTemp.y, saveTemp.isBg, locationTemp.s, true, locationTemp.cd);
                        }
                    }

                    //添加后把对应区域去掉
                    regionData[i] = null;
                    regionData.RemoveAt(i);
                    break;
                }
            }

            regionData.Add(region);
        }

        public Region GetOrAddRegion(Vector2Int index)
        {
            if (TryGetRegion(index, out Region region))
            {
                return region;
            }

            AddRegion(new() { index = index });
            return regionData[^1];
        }

        public bool TryGetRegion(Vector2Int index, out Region region)
        {
            foreach (var sb in regionData)
            {
                if (sb.index == index)
                {
                    region = sb;
                    return true;
                }
            }

            region = null;
            return false;
        }

        public Region GetRegion(Vector2Int index)
        {
            foreach (var region in regionData)
                if (region.index == index)
                    return region;

            return null;
        }
    }

    [Serializable]
    public class WorldBasicData
    {
        public int seed;
        public string worldName;
        public string gameVersion;
        public float time = 300;
        public float totalTime = 0;
        public bool isAM = true;
        public string weather = WeatherID.Sunny;
        public List<Vector2> teleportPoints = new();



        public string worldPath => Path.Combine(GInit.worldPath, worldName);
    }

    [Serializable]
    public class LaborData
    {
        public List<LaborHousing> registeredHousings = new();
        public List<LaborWork> executingWorks = new();
        public int laborCount = 0;
        public float laborHappinessIndex = 1;

        //TODO
        public int GetHousingRent()
        {
            return laborCount * 5;
        }
    }

    [Serializable]
    public class LaborHousing
    {
        public Vector2Int[] spaces;
        public bool isOccupied;

        public LaborHousing() { }

        public LaborHousing((int x, int y)[] spaces)
        {
            var list = new List<Vector2Int>();
            foreach (var (x, y) in spaces)
            {
                //忽略 (0,0)，其通常是未赋值的点，就算真的是房屋中的一格空间也没关系
                if (x == 0 && y == 0)
                    continue;

                list.Add(new Vector2Int(x, y));
            }

            this.spaces = list.ToArray();
        }
    }

    [Serializable]
    public class LaborWork
    {
        public List<LaborPlaceBlockWorkStep> placeBlockSteps;
        public int currentStepIndex;
        public int allocatedLaborCount;

        public LaborWork(List<LaborPlaceBlockWorkStep> placeBlockSteps, int allocatedLaborCount)
        {
            this.placeBlockSteps = placeBlockSteps;
            this.currentStepIndex = 0;
            this.allocatedLaborCount = allocatedLaborCount;
        }

        public void Begin()
        {
            CoroutineStarter.Do(IELaborBuildingProcess());
        }

        IEnumerator IELaborBuildingProcess()
        {
            for (int i = currentStepIndex; i < placeBlockSteps.Count; i++)
            {
                var step = placeBlockSteps[i];

                //执行并等待耗时
                yield return step.Execute();

                //TODO: currentStepIndex++;
            }

            //TODO: 建筑完成音效
            GAudio.Play(AudioID.SidebarSwitchButton, null);
        }
    }

    public abstract class LaborWorkStep
    {
        public LaborWork laborWork;

        public LaborWorkStep(LaborWork laborWork)
        {
            this.laborWork = laborWork;
        }

        public virtual IEnumerator Execute()
        {
            yield break;
        }
    }

    public sealed class LaborPlaceBlockWorkStep : LaborWorkStep
    {
        public Vector2Int blockPos;
        public bool isBackground;
        public string blockId;
        public BlockStatus blockStatus;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockPos"></param>
        /// <param name="isBackground"></param>
        /// <param name="blockId"></param>
        /// <param name="blockStatus">影响放置的方块是什么状态。如果需要破坏方块，那么随便填一个都可以</param>
        public LaborPlaceBlockWorkStep(LaborWork laborWork, Vector2Int blockPos, bool isBackground, string blockId, BlockStatus blockStatus) : base(laborWork)
        {
            this.blockPos = blockPos;
            this.isBackground = isBackground;
            this.blockId = blockId;
            this.blockStatus = blockStatus;
        }

        public override IEnumerator Execute()
        {
            base.Execute();

            var blockToDestroy = Map.instance.GetBlock(blockPos, isBackground);

            //破坏方块
            if (blockId.IsNullOrEmpty())
            {
                if (blockToDestroy != null)
                {
                    blockToDestroy.DestroySelf();
                    GAudio.Play(audioId: AudioID.DestroyBlock, blockPos);
                }
                else
                {
                    yield break;
                }
            }
            //放置方块
            else
            {
                //等待方块被破坏
                if (blockToDestroy != null)
                {
                    blockToDestroy.DestroySelf();

                    while (Map.instance.HasBlock(blockPos, isBackground))
                        yield return null;

                    yield return null;
                }

                //放置方块
                Map.instance.SetBlockNet(blockPos, isBackground, blockStatus, blockId, null);
                GAudio.Play(AudioID.PlaceBlock, blockPos);
            }

            Debug.Log(blockId.IsNullOrEmpty() && blockToDestroy == null);
            yield return new WaitForSeconds(1f / laborWork.allocatedLaborCount);
        }
    }
}