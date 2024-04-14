using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.High;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameCore
{
    [Serializable]
    public sealed class Map : SingletonToolsClass<Map>
    {
        [Serializable]
        public class BlockPool
        {
            public Stack<GameObject> stack = new();
            private readonly Map map;

            public Block Get(Chunk chunk, Vector2Int pos, bool isBackground, BlockData data, string customData)
            {
                GameObject go;

                if (stack.Count == 0)
                {
                    go = Instantiate(GInit.instance.GetBlockPrefab());
                }
                else
                {
                    go = stack.Pop();
                    go.transform.localScale = Vector3.one;
                }

                Block block = data.behaviourType == null ? new Block() : (Block)Activator.CreateInstance(data.behaviourType);

                block.sr = go.GetComponent<SpriteRenderer>();
                block.blockCollider = go.GetComponent<BoxCollider2D>();
                block.gameObject = go;
                block.transform = go.transform;



                /* ---------------------------------- 设置位置 ---------------------------------- */
                block.pos = pos;
                block.isBackground = isBackground;

                /* --------------------------------- 根据层设置参数 -------------------------------- */
                if (isBackground)
                {
                    block.transform.position = new(pos.x, pos.y, 0.02f);
                    block.sr.color = Block.backgroundColor;
                }
                else
                {
                    block.transform.position = new(pos.x, pos.y, 0f);
                    block.sr.color = Block.wallColor;
                }


                /* --------------------------------- 移动到新的区块 -------------------------------- */
                block.chunk = chunk;
                block.sr.enabled = block.chunk.totalRendererEnabled;
                block.transform.SetParent(block.chunk.transform);

                bool addedToChunk = false;
                for (int i = 0; i < block.chunk.blocks.Length; i++)
                {
                    if (block.chunk.blocks[i] == null)
                    {
                        block.chunk.blocks[i] = block;
                        addedToChunk = true;
                        break;
                    }
                }
                if (!addedToChunk)
                {
                    Debug.LogError("添加方块到区块失败, 区块没有空位置存放方块!");
                }

                /* ---------------------------------- 设置数据 ---------------------------------- */
                block.health = Block.totalMaxHealth;
                block.customData = JsonTools.LoadJObjectByString(customData);
                block.data = data;
                block.gameObject.name = data.id;

                /* ----------------------------------- 初始化 ---------------------------------- */
                if (data.lightLevel > 0)
                {
                    block.blockLight = map.blockLightPool.Get(block);
                }

                block.DoStart();
                return block;
            }

            public BlockPool(Map map)
            {
                this.map = map;
            }

            public void Recycle(Block block)
            {
                block.OnRecovered();
                if (block.crackSr) map.blockCrackPool.Recycle(block);
                if (block.blockLight) map.blockLightPool.Recycle(block.blockLight);
                if (map.blocksToCheckHealths.Contains(block)) map.blocksToCheckHealths.Remove(block);
                block.scaleAnimationTween?.Kill();
                block.shakeRotationTween?.Kill();

                block.transform.SetParent(map.blockPoolTrans);
                block.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                for (int i = 0; i < block.chunk.blocks.Length; i++)
                {
                    if (block.chunk.blocks[i] == block)
                    {
                        block.chunk.blocks[i] = null;
                        break;
                    }
                }

                stack.Push(block.gameObject);
            }
        }

        [Serializable]
        public class BlockCrackPool
        {
            public Stack<SpriteRenderer> stack = new();

            public SpriteRenderer Get(Block block)
            {
                SpriteRenderer sr = stack.Count == 0 ? ObjectTools.CreateSpriteObject("Block Crack") : stack.Pop();

                //设置贴图
                sr.sortingOrder = block.sr.sortingOrder;
                sr.gameObject.SetActive(true);

                //设置为方块的状态
                sr.transform.SetParent(block.transform);
                sr.transform.localPosition = new(0, 0, block.transform.position.z - 0.01f);
                sr.color = block.sr.color;

                return sr;
            }
            public void Recycle(Block block)
            {
                block.crackSr.transform.SetParent(null);
                block.crackSr.gameObject.SetActive(false);
                block.crackSr.transform.localScale = Vector3.one;
                block.crackSr.transform.localRotation = Quaternion.identity;
                stack.Push(block.crackSr);
                block.crackSr = null;
            }
        }

        [Serializable]
        public class BlockLightPool
        {
            public Stack<Light2D> stack = new();

            public Light2D Get(Block block)
            {
                Light2D light = stack.Count == 0 ? GameObject.Instantiate(GInit.instance.GetBlockLightPrefab()) : stack.Pop();

                light.gameObject.SetActive(true);
                light.transform.position = block.transform.position;

                light.pointLightOuterRadius = block.data.lightLevel * 1.5f;
                light.intensity = block.data.lightLevel / 7f;

                return light;
            }

            public void Recycle(Light2D light)
            {
                light.gameObject.SetActive(false);
                stack.Push(light);
            }
        }

        [Serializable]
        public class ChunkPool
        {
            public Stack<Chunk> stack = new();
            private Map map;

            public Chunk Get(Vector2Int chunkIndex)
            {
                Chunk chunk;

                if (stack.Count == 0)
                {
                    GameObject go = new($"{chunkIndex.x}, {chunkIndex.y}")
                    {
                        isStatic = true
                    };
                    chunk = go.AddComponent<Chunk>();

                    go.layer = Block.blockLayer;
                    go.transform.SetParent(map.transform);
                }
                else
                {
                    chunk = stack.Pop();
                    chunk.gameObject.SetActive(true);
                }

                //生成一个新的
                Apply(chunk, chunkIndex);

                return chunk;
            }

            public ChunkPool(Map map)
            {
                this.map = map;
            }

            public void Apply(Chunk chunk, Vector2Int chunkIndex)
            {
                chunk.transform.localPosition = (chunkIndex * Chunk.blockCountPerAxis).To2();

                chunk.chunkIndex = chunkIndex;
                chunk.regionIndex = PosConvert.ChunkToRegionIndex(chunkIndex);
                chunk.ComputePoints();

                map.chunks.Add(chunk);
                map.chunkTable.Add(chunkIndex, chunk);
            }

            public void Recycle(Chunk chunk)
            {
                map.chunks.Remove(chunk);
                map.chunkTable.Remove(chunk.chunkIndex);
                chunk.gameObject.SetActive(false);

                //回收区块里的方块
                chunk.RecycleAllBlocks();

                stack.Push(chunk);
            }
        }

        public BlockPool blockPool;
        public BlockCrackPool blockCrackPool;
        public BlockLightPool blockLightPool;
        public ChunkPool chunkPool;
        public Transform blockPoolTrans { get; private set; }



        public List<Chunk> chunks = new();
        public Dictionary<Vector2Int, Chunk> chunkTable = new();
        public List<Block> blocksToCheckHealths = new();


        protected override void Awake()
        {
            base.Awake();

            blockPoolTrans = new GameObject("Block Pool").transform;
            blockPoolTrans.SetParent(transform);
            blockPoolTrans.gameObject.SetActive(false);

            blockPool = new(this);
            blockCrackPool = new();
            blockLightPool = new();
            chunkPool = new(this);

            gameObject.layer = Block.blockLayer;
        }

        protected override void Start()
        {
            base.Start();

            // for (int x = -100; x < 100; x++)
            // {
            //     for (int y = -80; y < 80; y++)
            //     {
            //         Vector2Int ci = BlockToChunkIndex(new(x, y));
            //         Vector2Int sbi = ChunkToRegionIndex(ci);

            //         Debug.Log($"({x}, {y}) {ci}, {sbi}");
            //     }
            // }
        }

        private void Update()
        {
            /* ---------------------------------- 为方块回血 --------------------------------- */
            if (Player.TryGetLocal(out Player localPlayer) && localPlayer.Init.isServerCompletelyReady && localPlayer.TryGetRegion(out _))
            {
                var deltaHealth = 20f * Performance.frameTime;

                for (int i = blocksToCheckHealths.Count - 1; i >= 0; i--)
                {
                    var block = blocksToCheckHealths[i];

                    if (Tools.time >= block.lastDamageTime + 7.5f)
                    {
                        block.SetHealth(block.health + deltaHealth);
                    }
                }
            }



            /* --------------------------------- 开关区块的显示 -------------------------------- */
            if (!tools.mainCamera)
                return;

            foreach (var chunk in chunks)
            {
                if (chunk.IsOutOfView())
                {
                    if (chunk.totalRendererEnabled)
                    {
                        chunk.DisableRenderers();
                    }
                }
                else
                {
                    if (!chunk.totalRendererEnabled)
                    {
                        chunk.EnableRenderers();
                    }
                }
            }
        }

        public Chunk AddChunk(Vector2Int chunkIndex)
        {
            if (chunkTable.TryGetValue(chunkIndex, out Chunk chunk))
            {
                return chunk;
            }
            else
            {
                return chunkPool.Get(chunkIndex);
            }
        }

        public Block GetBlock(Vector2Int pos, bool isBackground)
        {
            Vector2Int chunkIndexTo = PosConvert.MapPosToChunkIndex(pos);

            return AddChunk(chunkIndexTo).GetBlock(pos, isBackground);
        }

        public bool TryGetBlock(Vector2Int pos, bool isBackground, out Block block)
        {
            block = GetBlock(pos, isBackground);

            return block != null;
        }

        public void SetBlockCustomDataOL(Block block)
        {
            Client.Send<NMSetBlockCustomData>(new(block.pos, block.isBackground, block.customData.ToString()));
        }

        public void SetBlockCustomDataOL(Vector2Int pos, bool isBackground, string customData)
        {
            Client.Send<NMSetBlockCustomData>(new(pos, isBackground, customData));
        }

        public void ClearAllBlocks()
        {
            foreach (Chunk chunk in chunks)
            {
                chunk.RecycleAllBlocks();
            }
        }

        public List<Block> GetBlocks()
        {
            List<Block> bs = new();

            foreach (Chunk chunk in chunks)
            {
                bs.AddRange(chunk.blocks);
            }

            return bs;
        }

        public void RecycleChunks()
        {
            for (int i = chunks.Count - 1; i >= 0; i--)
            {
                chunkPool.Recycle(chunks[i]);
            }
        }

        public bool HasBlock(Vector2Int pos, bool isBackground)
        {
            foreach (Chunk chunk in chunks)
            {
                foreach (Block block in chunk.blocks)
                {
                    if (block != null && block.pos == pos && block.isBackground == isBackground)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool HasBlockPlayerCursorPos(Player player, bool isBackground)
        {
            var pos = GetPlayerCursorMapPos(player);

            return HasBlock(pos, isBackground);
        }

        public bool GetBlockWorldPos(Vector2 pos, bool isBackground)
        {
            return GetBlock(PosConvert.WorldToMapPos(pos), isBackground) != null;
        }

        public bool HasBlockWorldPos(Vector2 pos, bool isBackground)
        {
            return HasBlock(PosConvert.WorldToMapPos(pos), isBackground);
        }

        public Vector2Int GetPlayerCursorMapPos(Player player) => PosConvert.WorldToMapPos(player.cursorWorldPos);

        public void SetBlockNet(Vector2Int pos, bool isBackground, string id, string customData) => Client.Send<NMSetBlock>(new(pos, isBackground, id, customData));

#if UNITY_EDITOR
        [Button("放置方块")] private Block EditorSetBlock(Vector2Int pos, bool isBackground, string block, bool editRegion) => SetBlock(pos, isBackground, ModFactory.CompareBlockData(block), null, editRegion, true);
#endif

        public Block SetBlock(Vector2Int pos, bool isBackground, BlockData block, string customData, bool editRegion, bool executeBlockUpdate)
        {
            Chunk chunk = AddChunk(PosConvert.MapPosToChunkIndex(pos));

            //Remove 不执行生命周期，等到 Add 再更新，以节约性能
            chunk.RemoveBlock(pos, isBackground, editRegion, false);

            return chunk.AddBlock(pos, isBackground, block, customData, editRegion, executeBlockUpdate);
        }

        public Block AddBlock(Vector2Int pos, bool isBackground, BlockData blockData, string customData, bool editRegion, bool executeBlockUpdate)
        {
            Vector2Int chunkIndexTo = PosConvert.MapPosToChunkIndex(pos);

            return AddChunk(chunkIndexTo).AddBlock(pos, isBackground, blockData, customData, editRegion, executeBlockUpdate);
        }

        public void RemoveBlock(Vector2Int pos, bool isBackground, bool editRegion, bool executeBlockUpdate)
        {
            Vector2Int chunkIndexTo = PosConvert.MapPosToChunkIndex(pos);

            AddChunk(chunkIndexTo).RemoveBlock(pos, isBackground, editRegion, executeBlockUpdate);
        }

        public void UpdateAt(Vector2Int pos, bool isBackground)
        {
            Vector2Int[] points = new[]
            {
                new (pos.x - 1, pos.y - 1),
                new (pos.x - 1, pos.y),
                new (pos.x - 1, pos.y + 1),
                new (pos.x, pos.y - 1),
                pos,
                new (pos.x, pos.y + 1),
                new (pos.x + 1, pos.y - 1),
                new (pos.x + 1, pos.y),
                new (pos.x + 1, pos.y + 1)
            };

            foreach (Vector2Int p in points)
            {
                var block = GetBlock(p, isBackground);

                block?.OnUpdate();
            }
        }
    }
}
