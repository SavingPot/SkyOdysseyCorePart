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
    public class Region
    {
        public Vector2Int index;
        [NonSerialized] ReaderWriterLockSlim savesReaderWriterLock = new(LockRecursionPolicy.NoRecursion);
        public List<BlockSave> saves = new();
        public List<EntitySave> entities = new();
        public string regionTheme;
        public Vector2Int size;
        public Vector2Int spawnPoint;
        public bool generatedAlready;

        //单数!
        public const int chunkCount = 12 + 1;
        public const float chunkCountReciprocal = 1f / chunkCount;
        public const float halfChunkCount = chunkCount / 2f;
        public const float negativeHalfChunkCount = -halfChunkCount;
        public static readonly Vector2Int placeVec = new(Chunk.blockCountPerAxis * chunkCount, Chunk.blockCountPerAxis * chunkCount);
        public static readonly Vector2 halfPlaceVec = new(placeVec.x / 2f, placeVec.y / 2f);
        public static Vector2Int place => placeVec;
        public static Vector2 halfPlace => halfPlaceVec;

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

        public void AddPos(string id, Vector2Int centerPoint, Vector3Int[] areas, bool overwriteIfContains, string customData = null)
        {
            savesReaderWriterLock.EnterWriteLock();
            try
            {
                if (!saves.Any(p => p.blockId == id))
                    saves.Add(new(id));
            }
            finally
            {
                savesReaderWriterLock.ExitWriteLock();
            }



            savesReaderWriterLock.EnterReadLock();
            try
            {
                foreach (var save in saves)
                {
                    if (save.blockId == id)
                    {
                        foreach (var area in areas)
                        {
                            Vector2Int targetPos = new(centerPoint.x + area.x, centerPoint.y + area.y);
                            bool targetIsBackground = area.z < 0;

                            save.AddLocation(targetPos, targetIsBackground, overwriteIfContains);
                        }

                        return;
                    }
                }
            }
            finally
            {
                savesReaderWriterLock.ExitReadLock();
            }
        }

        public void AddPos(string id, Vector2Int pos, bool isBackground, bool overwriteIfContains = false, string customData = null)
        {
            //TODO: 检测 pos 是否超出区域边界
            //如果 id 为空, 意思就是要删掉这个点
            if (string.IsNullOrEmpty(id))
            {
                savesReaderWriterLock.EnterReadLock();
                try
                {
                    foreach (var save in saves)
                    {
                        if (save.HasLocation(pos, isBackground))
                        {
                            save.RemoveLocation(pos, isBackground);
                            return;
                        }
                    }
                }
                finally
                {
                    savesReaderWriterLock.ExitReadLock();
                }
            }
            else
            {
                savesReaderWriterLock.EnterUpgradeableReadLock();
                try
                {
                    foreach (var save in saves)
                    {
                        if (save.blockId == id)
                        {
                            save.AddLocation(pos, isBackground, overwriteIfContains, customData);

                            return;
                        }
                    }

                    savesReaderWriterLock.EnterWriteLock();
                    saves.Add(new(pos, isBackground, id, customData));
                    savesReaderWriterLock.ExitWriteLock();
                }
                finally
                {
                    savesReaderWriterLock.ExitUpgradeableReadLock();
                }
            }
        }

        public void RemovePos(Vector2Int pos, bool isBackground)
        {
            lock (saves)
                foreach (var save in saves)
                {
                    try
                    {
                        save.readerWriterLock.EnterWriteLock();

                        for (int b = 0; b < save.locations.Count; b++)
                        {
                            BlockSave_Location location = save.locations[b];

                            if (location.pos == pos && location.isBackground == isBackground)
                            {
                                save.RemovePosAt(b);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        save.readerWriterLock.ExitWriteLock();
                    }
                }
        }

        public void RemovePos(string id, Vector2Int pos, bool isBackground)
        {
            lock (saves)
                foreach (var save in saves)
                {
                    if (save.blockId != id)
                        continue;

                    try
                    {
                        save.readerWriterLock.EnterWriteLock();
                        for (int b = 0; b < save.locations.Count; b++)
                        {
                            BlockSave_Location location = save.locations[b];

                            if (location.pos == pos && location.isBackground == isBackground)
                            {
                                save.RemovePosAt(b);
                                return;
                            }
                        }
                    }
                    finally
                    {
                        save.readerWriterLock.ExitWriteLock();
                    }
                }
        }

        public bool HasBlock(Vector2Int pos, bool isBackground) => GetBlock(pos, isBackground).location != null;

        public BlockSave GetBlock(string blockId)
        {
            foreach (var save in saves)
                if (save.blockId == blockId)
                    return save;

            return null;
        }

        public (BlockSave save, BlockSave_Location location) GetBlock(Vector2Int pos, bool isBackground)
        {
            savesReaderWriterLock.EnterReadLock();
            try
            {
                foreach (var save in saves)
                    if (save.TryGetLocation(pos, isBackground, out var result))
                        return (save, result);
            }
            finally
            {
                savesReaderWriterLock.ExitReadLock();
            }

            return (null, null);
        }

        public bool TryGetBlock(Vector2Int pos, bool isBackground, out (BlockSave save, BlockSave_Location location) result)
        {
            result = GetBlock(pos, isBackground);

            return result.location != null;
        }
    }

    [Serializable]
    public class BlockSave_Location
    {
        public Vector2Int pos;
        public bool isBackground;
        [LabelText("自定义数据")] public string customData;

        public BlockSave_Location(Vector2Int pos, bool isBackground, string customData)
        {
            this.pos = pos;
            this.isBackground = isBackground;
            this.customData = customData;
        }
    }

    [Serializable]
    public class BlockSave
    {
        public List<BlockSave_Location> locations;
        public string blockId;
        [NonSerialized] internal ReaderWriterLockSlim readerWriterLock;


        public BlockSave()
        {
            readerWriterLock = new(LockRecursionPolicy.SupportsRecursion);
        }

        public BlockSave(string blockId) : this()
        {
            this.locations = new() { };
            this.blockId = blockId;
        }

        public BlockSave(Vector2Int pos, bool isBackground, string blockId, string customData) : this()
        {
            this.locations = new() { new(pos, isBackground, customData) };
            this.blockId = blockId;
        }

        public bool HasLocation(Vector2Int pos, bool isBackground)
        {
            try
            {
                readerWriterLock.EnterReadLock();

                foreach (var location in locations)
                    if (location.pos == pos && location.isBackground == isBackground)
                        return true;
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }


            return false;
        }

        public BlockSave_Location GetLocation(Vector2Int pos, bool isBackground)
        {
            try
            {
                readerWriterLock.EnterReadLock();

                foreach (var location in locations)
                    if (location.pos == pos && location.isBackground == isBackground)
                        return location;
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }

            return null;
        }

        public void SetLocation(Vector2Int pos, bool isBackground, BlockSave_Location newValue)
        {
            try
            {
                readerWriterLock.EnterWriteLock();

                for (int i = 0; i < locations.Count; i++)
                {
                    var location = locations[i];

                    if (location.pos == pos && location.isBackground == isBackground)
                    {
                        locations[i] = newValue;
                        return;
                    }
                }
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }

            Debug.LogError($"修改方块 {blockId} ({pos}, {isBackground}) 失败：方块不存在");
        }

        public bool TryGetLocation(Vector2Int pos, bool isBackground, out BlockSave_Location result)
        {
            result = GetLocation(pos, isBackground);

            return result != null;
        }

        public void AddLocation(Vector2Int pos, bool isBackground, bool overwriteIfContains = false, string customData = null)
        {
            try
            {
                readerWriterLock.EnterWriteLock();

                for (int i = 0; i < locations.Count; i++)
                {
                    BlockSave_Location location = locations[i];

                    if (location.pos.x == pos.x && location.pos.y == pos.y && location.isBackground == isBackground)
                    {
                        if (overwriteIfContains)
                            locations[i] = new(pos, isBackground, customData);
                        else
                            Debug.LogError($"添加方块 {blockId} ({pos}, {isBackground}) 失败：方块已存在");
                        return;
                    }
                }

                locations.Add(new(pos, isBackground, customData));
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public void RemoveLocation(Vector2Int pos, bool isBackground)
        {
            try
            {
                readerWriterLock.EnterUpgradeableReadLock();

                for (int i = 0; i < locations.Count; i++)
                {
                    BlockSave_Location location = locations[i];

                    if (location.pos.x == pos.x && location.pos.y == pos.y && location.isBackground == isBackground)
                    {
                        readerWriterLock.EnterWriteLock();
                        locations.RemoveAt(i);
                        readerWriterLock.ExitWriteLock();
                        return;
                    }
                }
            }
            finally
            {
                readerWriterLock.ExitUpgradeableReadLock();
            }
        }

        public void RemovePosAt(int index)
        {
            readerWriterLock.EnterWriteLock();
            locations.RemoveAt(index);
            readerWriterLock.ExitWriteLock();
        }
    }

    [Serializable]
    public class Vector3IntSave
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