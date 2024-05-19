using UnityEngine;

namespace GameCore
{
    public static class MapUtils
    {
        public static bool IsConstructionWall(Vector2Int pos)
            => Map.instance.TryGetBlock(pos, false, out var block) && block.data.IsConstructionWall();

        // TODO：检测是否是一个封闭的建筑
    }
}