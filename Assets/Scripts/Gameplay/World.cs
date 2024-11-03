using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SP.Tools;
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
        public static string GetLaborDataPath(string worldPath) => Path.Combine(worldPath, "labor_data.json");
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
        public LaborData laborData = new();



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
            GFiles.SaveFileJson(GetLaborDataPath(worldPath), laborData, false, true);
        }



        public static World Load(string dirPath)
        {
            WorldBasicData basicData = JsonUtils.LoadTypeFromJsonPath<WorldBasicData>(GetBasicDataPath(dirPath));
            List<Region> regionData = JsonUtils.LoadTypeFromJsonPath<List<Region>>(GetRegionDataPath(dirPath));
            List<PlayerSave> playerData = JsonUtils.LoadTypeFromJsonPath<List<PlayerSave>>(GetPlayerDataPath(dirPath));
            LaborData laborData = JsonUtils.LoadTypeFromJsonPath<LaborData>(GetLaborDataPath(dirPath));

            return new(basicData, regionData, playerData, laborData);
        }



        public World(int seed, string worldName)
        {
            basicData.seed = seed;
            basicData.gameVersion = GInit.gameVersion;
            basicData.worldName = worldName;
        }

        public World(WorldBasicData basicData, List<Region> regionData, List<PlayerSave> playerSaves, LaborData laborData)
        {
            this.basicData = basicData;
            this.regionData = regionData;
            this.playerSaves = playerSaves;
            this.laborData = laborData;
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
        public Vector2Int position;
        public LaborWorkStep[] steps;
        public int currentStep;
        public int laborCount;
    }

    public abstract class LaborWorkStep
    {

    }
}