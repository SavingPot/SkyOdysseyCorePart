using GameCore.High;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class GameCallbacks
    {
        public static event Action<Region> BeforeGeneratingExistingRegion = region =>
        {
            if (GFiles.world == null)
                Debug.Log($"尝试生成原有区域, index={region.index}");
            else
                Debug.Log($"尝试在世界 {GFiles.world.basicData.worldName} 生成原有区域 {region.index}, 群系为 {region.biomeId}, 尺寸 {region.size}");
        };
        public static event Action<Region> AfterGeneratingExistingRegion = _ => { };

        public static event Action<RegionGeneration> BeforeGeneratingNewRegion = (generation) =>
        {
            Debug.Log($"开始尝试以种子 {generation.actualSeed} ({generation.originalSeed} + {generation.index.x} * 2 + {generation.index.y} * 4)  在世界 {GFiles.world.basicData.worldName} 生成新地形");
        };
        public static event Action<Region> AfterGeneratingNewRegion = _ => { };

        public static OnBlockDestroyedDelegate OnBlockDestroyed = (_, _, _) => { };
        public static OnRemoveBlockDelegate OnRemoveBlock = (pos, isBackground, editRegion, successful) => { };
        public static OnAddBlockDelegate OnAddBlock = (pos, isBackground, block, chunk) => { };


        public static event Action OnSaveAllDataToFiles = () => { Debug.Log("保存了所有数据至文件"); };

        internal static void CallBeforeGeneratingExistingRegion(Region region) => BeforeGeneratingExistingRegion(region);
        internal static void CallAfterGeneratingExistingRegion(Region region) => AfterGeneratingExistingRegion(region);

        internal static void CallBeforeGeneratingNewRegion(RegionGeneration generation) => BeforeGeneratingNewRegion(generation);
        internal static void CallAfterGeneratingNewRegion(Region region) => AfterGeneratingNewRegion(region);

        internal static void CallOnSaveAllDataToFiles() => OnSaveAllDataToFiles();




        public delegate void OnBlockDestroyedDelegate(Vector2Int pos, bool isBackground, BlockData blockData);
        public delegate void OnRemoveBlockDelegate(Vector2Int pos, bool isBackground, bool editRegion, bool successful);
        public delegate void OnAddBlockDelegate(Vector2Int pos, bool isBackground, Block block, Chunk chunk);
    }
}
