using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using GameCore.High;
using Unity.Burst;
using static GameCore.PlayerUI;

namespace GameCore
{
    [Serializable]
    public class Sandbox
    {
        public Vector2Int index;
        public List<BlockSave> saves = new();
        public List<EntitySave> entities = new();
        public string biome;
        public Vector2Int size;
        public Vector2Int spawnPoint;
        public bool generatedAlready;

        //单数!
        public const int chunkCountX = 9;
        public const int chunkCountY = 7;
        public const float chunkCountXReciprocal = 1f / chunkCountX;
        public const float chunkCountYReciprocal = 1f / chunkCountY;
        public const float halfChunkCountX = chunkCountX / 2f;
        public const float halfChunkCountY = chunkCountY / 2f;
        public static readonly Vector2Int placeVec = new(Chunk.blockCountPerAxis * chunkCountX, Chunk.blockCountPerAxis * chunkCountY);
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
            int x = Sandbox.place.x * index.x;

            return x;
        }

        [BurstCompile]
        public static int GetMiddleY(Vector2Int index)
        {
            int y = Sandbox.place.y * index.y;

            return y;
        }

        [BurstCompile]
        public static float GetRightX(Vector2Int index)
        {
            float x = GetMiddleX(index) + Sandbox.halfPlace.x;

            return x;
        }

        [BurstCompile]
        public static float GetLeftX(Vector2Int index)
        {
            float x = GetMiddleX(index) - Sandbox.halfPlace.x;

            return x;
        }

        [BurstCompile]
        public static float GetUpY(Vector2Int index)
        {
            float y = GetMiddleY(index) + Sandbox.halfPlace.y;

            return y;
        }

        [BurstCompile]
        public static float GetDownY(Vector2Int index)
        {
            float y = GetMiddleY(index) - Sandbox.halfPlace.y;

            return y;
        }

        public void AddPos(string id, Vector3Int pos, Vector3Int[] areas, bool overwriteIfContains, string customData = null)
        {
            lock (saves)
            {
                bool addedCore = false;

            add:
                for (int i = 0; i < saves.Count; i++)
                {
                    BlockSave save = saves[i];

                    if (save.blockId == id)
                    {
                        foreach (var area in areas)
                        {
                            if (addedCore && area == pos)
                                continue;

                            Vector2Int targetPos = new(pos.x + area.x, pos.y + area.y);
                            BlockLayer targetLayer = BlockLayerHelp.Parse(area.z);

                            save.AddLocation(targetPos, targetLayer, overwriteIfContains);
                        }

                        return;
                    }
                }

                saves.Add(new(new(pos.x, pos.y), BlockLayerHelp.Parse(pos.z), id, customData));
                addedCore = true;
                goto add;
            }
        }

        public void AddPos(string id, Vector3Int pos, bool overwriteIfContains = false, string customData = null)
        {
            Vector2Int pos2 = new(pos.x, pos.y);
            BlockLayer layer = BlockLayerHelp.Parse(pos.z);

            lock (saves)
            {
                if (string.IsNullOrEmpty(id))
                {
                    foreach (var save in saves)
                    {
                        if (save.HasLocation(pos2, layer))
                        {
                            save.RemoveLocation(pos2, layer);
                            return;
                        }
                    }
                }
                else
                {
                    foreach (var save in saves)
                    {
                        if (save.blockId == id)
                        {
                            save.AddLocation(pos2, layer, overwriteIfContains, customData);

                            return;
                        }
                    }

                    saves.Add(new(pos2, layer, id, customData));
                }
            }
        }

        public void RemovePos(Vector2Int pos, BlockLayer layer)
        {
            lock (saves)
                for (int i = 0; i < saves.Count; i++)
                {
                    BlockSave save = saves[i];

                    for (int b = 0; b < save.locations.Count; b++)
                    {
                        BlockSave_Location location = save.locations[b];

                        if (location.pos == pos && location.layer == layer)
                        {
                            save.RemovePosAt(b);
                            break;
                        }
                    }
                }
        }

        public void RemovePos(string id, Vector2Int pos, BlockLayer layer)
        {
            lock (saves)
                for (int i = 0; i < saves.Count; i++)
                {
                    BlockSave save = saves[i];

                    if (save.blockId != id)
                        continue;

                    for (int b = 0; b < save.locations.Count; b++)
                    {
                        BlockSave_Location location = save.locations[b];

                        if (location.pos == pos && location.layer == layer)
                        {
                            save.RemovePosAt(b);
                            return;
                        }
                    }
                }
        }

        public bool HasBlock(Vector2Int pos, BlockLayer layer) => GetBlock(pos, layer) != null;

        public BlockSave_Location GetBlock(Vector2Int pos, BlockLayer layer)
        {
            lock (saves)
                foreach (var block in saves)
                    if (block.TryGetLocation(pos, layer, out var result))
                        return result;

            return null;
        }

        public bool GetBlockOut(Vector2Int pos, BlockLayer player, out BlockSave_Location dbs)
        {
            dbs = GetBlock(pos, player);

            return dbs != null;
        }
    }

    [Serializable]
    public class BlockSave_Location
    {
        [NonSerialized] public BlockSave block;
        public Vector2Int pos;
        public BlockLayer layer;
        [LabelText("自定义数据")] public string customData;

        public BlockSave_Location()
        {

        }

        public BlockSave_Location(BlockSave block, Vector2Int pos, BlockLayer player, string customData)
        {
            this.block = block;
            this.pos = pos;
            this.layer = player;
            this.customData = customData;
        }
    }

    [Serializable]
    public class BlockSave
    {
        public List<BlockSave_Location> locations;
        public string blockId;


        public BlockSave()
        {

        }

        public BlockSave(Vector2Int pos, BlockLayer layer, string blockId, string customData) : this()
        {
            this.locations = new() { new(this, pos, layer, customData) };
            this.blockId = blockId;
        }

        public bool HasLocation(Vector2Int pos, BlockLayer layer)
        {
            lock (locations)
                foreach (var location in locations)
                    if (location.pos == pos && location.layer == layer)
                        return true;

            return false;
        }

        public BlockSave_Location GetLocation(Vector2Int pos, BlockLayer layer)
        {
            lock (locations)
                foreach (var location in locations)
                    if (location.pos == pos && location.layer == layer)
                        return location;

            return null;
        }

        public bool TryGetLocation(Vector2Int pos, BlockLayer layer, out BlockSave_Location result)
        {
            result = GetLocation(pos, layer);

            return result != null;
        }

        public void AddLocation(Vector2Int pos, BlockLayer layer, bool overwriteIfContains = false, string customData = null)
        {
            lock (locations)
            {
                for (int i = 0; i < locations.Count; i++)
                {
                    BlockSave_Location location = locations[i];

                    if (location.pos.x == pos.x && location.pos.y == pos.y && location.layer == layer)
                    {
                        if (overwriteIfContains)
                            locations[i] = new(this, pos, layer, customData);
                        else
                            Debug.LogError($"位置 {pos} [{layer}] 已存在");
                        return;
                    }
                }

                locations.Add(new(this, pos, layer, customData));
            }
        }

        public void RemoveLocation(Vector2Int pos, BlockLayer layer)
        {
            lock (locations)
            {
                for (int i = 0; i < locations.Count; i++)
                {
                    BlockSave_Location location = locations[i];

                    if (location.pos.x == pos.x && location.pos.y == pos.y && location.layer == layer)
                    {
                        locations.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public void RemovePosAt(int index)
        {
            lock (locations)
            {
                locations.RemoveAt(index);
            }
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

    [Serializable]
    public class PlayerData
    {
        public string playerName;
        public Vector2Int currentSandbox;
        public float hungerValue;
        public float thirstValue;
        public float happinessValue;
        public float health;
        public Inventory inventory;// = new();
        public List<TaskStatusForSave> completedTasks = new();
    }
}