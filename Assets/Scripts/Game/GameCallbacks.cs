using GameCore.High;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class GameCallbacks
    {
        public static event Action<Sandbox> BeforeGeneratingExistingSandbox = sandbox =>
        {
            if (GFiles.world == null)
                Debug.Log($"尝试生成原有沙盒, index={sandbox.index}");
            else
                Debug.Log($"尝试在世界 {GFiles.world.basicData.worldName} 生成原有沙盒 {sandbox.index}, 生物群系为 {sandbox.biome}, 尺寸 {sandbox.size}");
        };
        public static event Action<Sandbox> AfterGeneratingExistingSandbox = _ => { };

        public static event Action<MapGeneration> BeforeGeneratingNewSandbox = (generation) =>
        {
            Debug.Log($"开始尝试以种子 {generation.actualSeed} ({generation.originalSeed} + {generation.index.x} * 2 + {generation.index.y} * 4)  在世界 {GFiles.world.basicData.worldName} 生成新地形 {generation.biome.id} {generation.index}");
        };
        public static event Action<Sandbox> AfterGeneratingNewSandbox = _ => { };

        public static Action<Vector2Int, bool, BlockData> OnBlockDestroyed = (_, _, _) => { };
        public static Action<Vector2Int, bool, bool, bool> OnRemoveBlock = (pos, layer, editSandbox, successful) =>
        {
            Map.instance.UpdateAt(pos, layer);
        };
        public static Action<Vector2Int, bool, Block, Chunk> OnAddBlock = (pos, layer, block, chunk) =>
        {
            Map.instance.UpdateAt(pos, layer);
        };


        public static event Action OnSaveAllDataToFiles = () => { Debug.Log("保存了所有数据至文件"); };

        internal static void CallBeforeGeneratingExistingSandbox(Sandbox sandbox) => BeforeGeneratingExistingSandbox(sandbox);
        internal static void CallAfterGeneratingExistingSandbox(Sandbox sandbox) => AfterGeneratingExistingSandbox(sandbox);

        internal static void CallBeforeGeneratingNewSandbox(MapGeneration generation) => BeforeGeneratingNewSandbox(generation);
        internal static void CallAfterGeneratingNewSandbox(Sandbox sandbox) => AfterGeneratingNewSandbox(sandbox);

        internal static void CallOnSaveAllDataToFiles() => OnSaveAllDataToFiles();
    }
}
