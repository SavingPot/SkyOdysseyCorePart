using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.High;
using GameCore.Network;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Prng;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameCore
{
    [Serializable]
    public sealed class Map : SingletonClass<Map>
    {
        public BlockPool blockPool;
        public BlockCrackPool blockCrackPool;
        public BlockLightPool blockLightPool;
        public ChunkPool chunkPool;
        public Transform blockPoolTrans { get; private set; }



        public List<Chunk> chunks = new();
        public Dictionary<Vector2Int, Chunk> chunkTable = new();
        public List<PlatformEffector2D> platformEffectorsToCheck = new();
        public List<Block> blocksToCheckHealths = new();




        float lastTimeDisablePlatformPressed;
        bool isPlatformDisabledLastFrame;





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

        private void Update()
        {
            if (Player.TryGetLocal(out Player player) && player.Init.isServerCompletelyReady && player.TryGetRegion(out _))
            {
                /* ---------------------------------- 为方块回血 --------------------------------- */
                var deltaHealth = 20f * Performance.frameTime;

                for (int i = blocksToCheckHealths.Count - 1; i >= 0; i--)
                {
                    var block = blocksToCheckHealths[i];

                    if (Tools.time >= block.lastDamageTime + 7.5f)
                    {
                        block.SetHealth(block.health + deltaHealth);
                    }
                }


                /* ---------------------------------- 设置平台 ---------------------------------- */
                if (player.playerController.DisablePlatform())
                    lastTimeDisablePlatformPressed = Tools.time;

                bool isPlatformDisabled = Tools.time <= lastTimeDisablePlatformPressed + 0.12f; //按住后的0.12s内会禁用平台
                if (isPlatformDisabled != isPlatformDisabledLastFrame)
                {
                    float platformRotation = GetPlatformEffectorRotation(isPlatformDisabled);
                    foreach (var effector in platformEffectorsToCheck)
                    {
                        effector.rotationalOffset = platformRotation;
                    }
                    isPlatformDisabledLastFrame = isPlatformDisabled;
                }
            }



            /* --------------------------------- 开关区块的显示 -------------------------------- */
            if (!Tools.instance.mainCamera || !GFiles.settings.autoHideChunks)
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

        public float GetPlatformEffectorRotation() => GetPlatformEffectorRotation(Player.TryGetLocal(out var player) ? player.playerController.DisablePlatform() : false);
        public float GetPlatformEffectorRotation(bool isPlatformDisabled) => isPlatformDisabled ? 180f : 0f;

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

        public Block this[Vector2Int pos, bool isBackground] => GetBlock(pos, isBackground);
        public Block this[int x, int y, bool isBackground] => GetBlock(new(x, y), isBackground);

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

        public void ClearAllBlocks()
        {
            foreach (Chunk chunk in chunks)
            {
                chunk.RecycleAllBlocks();
            }
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
                if (isBackground)
                {
                    foreach (Block block in chunk.backgroundBlocks)
                    {
                        if (block != null && block.pos == pos)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (Block block in chunk.wallBlocks)
                    {
                        if (block != null && block.pos == pos)
                        {
                            return true;
                        }
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

        public void SetBlockNet(Vector2Int pos, bool isBackground, BlockStatus status, string id, string customData) => Client.Send<NMSetBlock>(new(pos, isBackground, status, id, customData));

#if UNITY_EDITOR
        [Button("放置方块")] private Block EditorSetBlock(Vector2Int pos, bool isBackground, BlockStatus status, string block, bool editRegion) => SetBlock(pos, isBackground, status, ModFactory.CompareBlockData(block), null, editRegion, true);
#endif

        public Block SetBlock(Vector2Int pos, bool isBackground, BlockStatus status, BlockData block, string customData, bool editRegion, bool executeBlockUpdate)
        {
            Chunk chunk = AddChunk(PosConvert.MapPosToChunkIndex(pos));

            //Remove 不执行生命周期，等到 Add 再更新，以节约性能
            chunk.RemoveBlock(pos, isBackground, editRegion, false);

            return chunk.AddBlock(pos, isBackground, status, block, customData, editRegion, executeBlockUpdate);
        }

        public Block AddBlock(Vector2Int pos, bool isBackground, BlockStatus status, BlockData blockData, string customData, bool editRegion, bool executeBlockUpdate)
        {
            Vector2Int chunkIndexTo = PosConvert.MapPosToChunkIndex(pos);

            return AddChunk(chunkIndexTo).AddBlock(pos, isBackground, status, blockData, customData, editRegion, executeBlockUpdate);
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

        public void UpdateAllBlocks()
        {
            //要复制一份，防止在生成过程中迭代器变化
            var chunksTemp = chunks.ToArray();

            foreach (var chunk in chunksTemp)
            {
                //要复制一份，防止在生成过程中迭代器变化
                Block[] blocks = new Block[chunk.wallBlocks.Length + chunk.backgroundBlocks.Length];
                chunk.wallBlocks.CopyTo(blocks, 0);
                chunk.backgroundBlocks.CopyTo(blocks, chunk.wallBlocks.Length);

                //更新
                foreach (var block in blocks)
                {
                    block?.OnUpdate();
                }
            }
        }








        [Serializable]
        public class BlockPool
        {
            public Stack<GameObject> stack = new();
            private readonly Map map;

            public Block Get(Chunk chunk, Vector2Int pos, bool isBackground,BlockStatus status, BlockData data, string customData)
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



                /* ---------------------------------- 设置位置、状态 ---------------------------------- */
                block.pos = pos;
                block.isBackground = isBackground;
                block.status = status;

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
                if (isBackground)
                {
                    for (int i = 0; i < block.chunk.backgroundBlocks.Length; i++)
                    {
                        if (block.chunk.backgroundBlocks[i] == null)
                        {
                            block.chunk.backgroundBlocks[i] = block;
                            addedToChunk = true;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < block.chunk.wallBlocks.Length; i++)
                    {
                        if (block.chunk.wallBlocks[i] == null)
                        {
                            block.chunk.wallBlocks[i] = block;
                            addedToChunk = true;
                            break;
                        }
                    }
                }
                if (!addedToChunk)
                {
                    Debug.LogError("添加方块到区块失败, 区块没有空位置存放方块!");
                }

                /* ---------------------------------- 设置数据 ---------------------------------- */
                block.health = Block.totalMaxHealth;
                block.customData = JsonUtils.LoadJObjectByString(customData);
                block.data = data;
                block.gameObject.name = $"{data.id} {pos}";

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

                //回收平台效果器
                if (block.platformEffector) block.DestroyPlatformEffector();

                block.transform.SetParent(map.blockPoolTrans);
                block.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                if (block.isBackground)
                {
                    for (int i = 0; i < block.chunk.backgroundBlocks.Length; i++)
                    {
                        if (block.chunk.backgroundBlocks[i] == block)
                        {
                            block.chunk.backgroundBlocks[i] = null;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < block.chunk.wallBlocks.Length; i++)
                    {
                        if (block.chunk.wallBlocks[i] == block)
                        {
                            block.chunk.wallBlocks[i] = null;
                            break;
                        }
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
                light.intensity = block.data.lightLevel / 8f;

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
    }
}
