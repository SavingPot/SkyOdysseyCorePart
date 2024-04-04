using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using GameCore.High;
using Unity.Burst;
using System.Threading;

namespace GameCore
{
    [Serializable]
    public sealed class Region
    {
        public Vector2Int index;
        public List<BlockSave> blocks = new();
        public List<EntitySave> entities = new();
        public string regionTheme;
        public Vector2Int size;
        public Vector2Int spawnPoint;
        public bool generatedAlready;

        public Region ShallowCopy()
        {
            return (Region)MemberwiseClone();
        }

        //单数!
        public const int chunkCount = 4 + 1;
        public const float chunkCountReciprocal = 1f / chunkCount;
        public const float halfChunkCount = chunkCount / 2f;
        public const float negativeHalfChunkCount = -halfChunkCount;
        public static readonly Vector2Int placeVec = new(Chunk.blockCountPerAxis * chunkCount, Chunk.blockCountPerAxis * chunkCount);
        public static readonly Vector2 halfPlaceVec = new(placeVec.x / 2f, placeVec.y / 2f);

        public static Vector2Int place => placeVec;
        public static Vector2 halfPlace => halfPlaceVec;

        [BurstCompile]
        public static Vector2 GetMiddle(int indexX, int indexY)
        {
            int x = GetMiddleX(indexX);
            int y = GetMiddleY(indexY);

            return new(x, y);
        }

        [BurstCompile]
        public static int GetMiddleX(int indexX)
        {
            int x = Region.place.x * indexX;

            return x;
        }

        [BurstCompile]
        public static int GetMiddleY(int indexY)
        {
            int y = Region.place.y * indexY;

            return y;
        }

        [BurstCompile]
        public static Vector2 GetMiddle(Vector2Int index)
        {
            int x = GetMiddleX(index);
            int y = GetMiddleY(index);

            return new(x, y);
        }

        [BurstCompile]
        public static int GetMiddleX(Vector2Int index)
        {
            int x = Region.place.x * index.x;

            return x;
        }

        [BurstCompile]
        public static int GetMiddleY(Vector2Int index)
        {
            int y = Region.place.y * index.y;

            return y;
        }

        [BurstCompile]
        public static float GetRightX(Vector2Int index)
        {
            float x = GetMiddleX(index) + Region.halfPlace.x;

            return x;
        }

        [BurstCompile]
        public static float GetLeftX(Vector2Int index)
        {
            float x = GetMiddleX(index) - Region.halfPlace.x;

            return x;
        }

        [BurstCompile]
        public static float GetUpY(Vector2Int index)
        {
            float y = GetMiddleY(index) + Region.halfPlace.y;

            return y;
        }

        [BurstCompile]
        public static float GetDownY(Vector2Int index)
        {
            float y = GetMiddleY(index) - Region.halfPlace.y;

            return y;
        }

        public void AddPos(string id, int centerX, int centerY, Vector3Int[] areas, bool overwriteIfContains, string customData = null)
        {
            //TODO: Performance
            if (!blocks.Any(p => p.blockId == id && p.isBg))
                blocks.Add(new(id, true));
            if (!blocks.Any(p => p.blockId == id && !p.isBg))
                blocks.Add(new(id, false));

            foreach (var area in areas)
            {
                int targetX = centerX + area.x;
                int targetY = centerY + area.y;
                bool targetIsBackground = area.z < 0;

                foreach (var save in blocks)
                {
                    if (save.blockId == id && save.isBg == targetIsBackground)
                    {
                        save.AddLocation(targetX, targetY, overwriteIfContains);
                        break;
                    }
                }
            }
        }

        public void AddPos(string id, int x, int y, bool isBackground, bool overwriteIfContains = false, string customData = null)
        {
            //TODO: 检测 pos 是否超出区域边界
            //如果 id 为空, 意思就是要删掉这个点
            if (string.IsNullOrEmpty(id))
            {
                foreach (var save in blocks)
                {
                    if (save.isBg != isBackground)
                        continue;

                    save.RemoveLocation(x, y);
                    return;
                }
            }
            //如果 id 不为空, 意思就是要加上这个点
            else
            {
                foreach (var save in blocks)
                {
                    if (save.blockId == id && save.isBg == isBackground)
                    {
                        save.AddLocation(x, y, overwriteIfContains, customData);

                        return;
                    }
                }

                blocks.Add(new(x, y, id, isBackground, customData));
            }
        }

        public void RemovePos(int x, int y, bool isBackground)
        {
            lock (blocks)
                foreach (var save in blocks)
                {
                    if (save.isBg != isBackground)
                        continue;

                    for (int b = 0; b < save.locations.Count; b++)
                    {
                        BlockSave_Location location = save.locations[b];

                        if (location.x == x && location.y == y)
                        {
                            save.RemoveLocationAt(b);
                            break;
                        }
                    }
                }
        }

        public void RemovePos(string id, int x, int y, bool isBackground)
        {
            lock (blocks)
                foreach (var save in blocks)
                {
                    if (save.blockId != id || save.isBg != isBackground)
                        continue;

                    for (int i = 0; i < save.locations.Count; i++)
                    {
                        BlockSave_Location location = save.locations[i];

                        if (location.x == x && location.y == y)
                        {
                            save.RemoveLocationAt(i);
                            return;
                        }
                    }
                }
        }

        public bool HasBlock(int x, int y, bool isBackground) => GetBlock(x, y, isBackground).location != null;

        public BlockSave GetBlock(string blockId, bool isBackground)
        {
            foreach (var save in blocks)
                if (save.isBg == isBackground)
                    if (save.blockId == blockId)
                        return save;

            return null;
        }

        public (BlockSave save, BlockSave_Location location) GetBlock(int x, int y, bool isBackground)
        {
            foreach (var save in blocks)
                if (save.isBg == isBackground)
                    if (save.TryGetLocation(x, y, out var result))
                        return (save, result);

            return (null, null);
        }

        public bool TryGetBlock(int x, int y, bool isBackground, out (BlockSave save, BlockSave_Location location) result)
        {
            result = GetBlock(x, y, isBackground);

            return result.location != null;
        }
    }

    [Serializable]
    public class BlockSave_Location
    {
        public int x;
        public int y;
        [LabelText("自定义数据")] public string cd;

        public BlockSave_Location(int x, int y, string customData)
        {
            this.x = x;
            this.y = y;
            this.cd = customData;
        }
    }

    [Serializable]
    public class BlockSave
    {
        public List<BlockSave_Location> locations;
        public string blockId;
        public bool isBg;


        public BlockSave()
        {

        }

        public BlockSave(string blockId, bool isBg) : this()
        {
            this.locations = new() { };
            this.blockId = blockId;
            this.isBg = isBg;
        }

        public BlockSave(int x, int y, string blockId, bool isBg, string customData) : this()
        {
            this.locations = new() { new(x, y, customData) };
            this.blockId = blockId;
            this.isBg = isBg;
        }

        public bool HasLocation(int x, int y)
        {
            foreach (var location in locations)
                if (location.x == x && location.y == y)
                    return true;

            return false;
        }

        public BlockSave_Location GetLocation(int x, int y)
        {
            foreach (var location in locations)
                if (location.x == x && location.y == y)
                    return location;

            return null;
        }

        public bool TryGetLocation(int x, int y, out BlockSave_Location result)
        {
            result = GetLocation(x, y);

            return result != null;
        }

        public void AddLocation(int x, int y, bool overwriteIfContains = false, string customData = null)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                BlockSave_Location location = locations[i];

                if (location.x == x && location.y == y)
                {
                    if (overwriteIfContains)
                        locations[i] = new(x, y, customData);
                    else
                        Debug.LogError($"添加方块 {blockId} (({x},{y}), {isBg}) 失败：方块已存在");
                    return;
                }
            }

            locations.Add(new(x, y, customData));
        }

        public void RemoveLocation(int x, int y)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                BlockSave_Location location = locations[i];

                if (location.x == x && location.y == y)
                {
                    locations.RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveLocationAt(int index)
        {
            locations.RemoveAt(index);
        }
    }

    [Serializable]
    public sealed class Vector3IntSave
    {
        public int x;
        public int y;
        public int z;

        public bool EqualTo(Vector3Int vec)
        {
            return x == vec.x && y == vec.y && z == vec.z;
        }

        public bool EqualTo(Vector3IntSave vec)
        {
            return x == vec.x && y == vec.y && z == vec.z;
        }

        public Vector3IntSave()
        {

        }

        public Vector3IntSave(Vector3Int vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }

        public static explicit operator Vector3Int(Vector3IntSave vec)
        {
            return new(vec.x, vec.y, vec.z);
        }
    }
}