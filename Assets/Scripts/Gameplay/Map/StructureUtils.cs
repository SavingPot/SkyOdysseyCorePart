using SP.Tools;
using UnityEngine;

namespace GameCore
{
    public static class StructureUtils
    {
        public static void GenerateStructure(StructureData structure, Vector2Int anchorPos)
        {
            //放置方块
            foreach (var structBlock in structure.fixedBlocks)
            {
                var blockPos = anchorPos + structBlock.offset;

                if (structBlock.blockId.IsNullOrEmpty())
                    Map.instance.GetBlock(blockPos, structBlock.isBackground)?.DestroySelf();
                else
                    Map.instance.SetBlockNet(blockPos, structBlock.isBackground, structBlock.status, structBlock.blockId, null);
            }
        }
    }
}