using GameCore.High;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameCore
{
    [Serializable]
    public class Map : SingletonToolsClass<Map>
    {
        [Serializable]
        public class BlockPool
        {
            public Stack<GameObject> stack = new();
            private readonly Map map;

            public Block Get(Vector2Int pos, bool isBackground, BlockData data, string customData)
            {
                GameObject go = stack.Count == 0 ? Instantiate(GInit.instance.GetBlockPrefab()) : stack.Pop();

                Block block = (Block)go.AddComponent(data.behaviourType ?? typeof(Block));

                block.sr = block.GetComponent<SpriteRenderer>();
                block.blockCollider = block.GetComponent<BoxCollider2D>();
                block.health = Block.totalMaxHealth;
                block.customData = JsonTools.LoadJObjectByString(customData);



                /* ---------------------------------- 设置位置 ---------------------------------- */
                block.pos = pos;
                block.isBackground = isBackground;

                /* --------------------------------- 根据层设置参数 -------------------------------- */
                block.transform.position = new(pos.x, pos.y, isBackground ? 0.02f : 0f);
                block.sr.color = isBackground ? Block.backgroundColor : Block.wallColor;


                /* --------------------------------- 移动到新的区块 -------------------------------- */
                block.chunk = map.AddChunk(PosConvert.BlockPosToChunkIndex(pos)); ;
                block.transform.SetParent(block.chunk.transform);

                bool addedToChunk = false;
                for (int i = 0; i < block.chunk.blocks.Length; i++)
                {
                    if (!block.chunk.blocks[i])
                    {
                        block.chunk.blocks[i] = block;
                        addedToChunk = true;
                        break;
                    }
                }
                if (!addedToChunk)
                {
                    Debug.LogError("添加方块到区块失败, 可能是没有空位置存放方块!");
                }

                //根据区块设置参数
                block.sr.enabled = block.chunk.totalRendererEnabled;
                /* ---------------------------------- 设置数据 ---------------------------------- */
                block.data = data;
                block.gameObject.name = data.id;

                /* ----------------------------------- 初始化 ---------------------------------- */
                if (data.lightLevel > 0)
                {
                    block.blockLight = map.blockLightPool.Get(block); ;
                }
                block.DoStart();
                map.UpdateAt(block.pos, block.isBackground);

                return block;
            }

            public BlockPool(Map map)
            {
                this.map = map;
            }

            public void Recover(Block block)
            {
                block.OnRecovered();
                if (block.crackSr) map.blockCrackPool.Recover(block.crackSr);
                if (block.blockLight) map.blockLightPool.Recover(block.blockLight);
                block.transform.SetParent(map.blockPoolTrans);

                if (block.chunk)
                {
                    for (int i = 0; i < block.chunk.blocks.Length; i++)
                    {
                        if (block.chunk.blocks[i] == block)
                        {
                            block.chunk.blocks[i] = null;
                            break;
                        }
                    }
                }

                GameObject go = block.gameObject;
                Block.Destroy(block);

                stack.Push(go);
            }
        }

        [Serializable]
        public class BlockCrackPool
        {
            public Stack<SpriteRenderer> stack = new();

            public SpriteRenderer Get(Block block, byte progress)
            {
                if (progress >= 0)
                {
                    SpriteRenderer sr = stack.Count == 0 ? ObjectTools.CreateSpriteObject() : stack.Pop();

                    //设置贴图
                    sr.sprite = ModFactory.CompareTexture($"ori:block_crack_{progress}").sprite;
                    sr.sortingOrder = block.sr.sortingOrder;
                    sr.gameObject.SetActive(true);

                    //设置为方块的状态
                    sr.transform.SetParent(block.transform);
                    sr.transform.localPosition = new(0, 0, block.transform.position.z - 0.01f);
                    sr.color = block.sr.color;

                    return sr;
                }

                return null;
            }
            public void Recover(SpriteRenderer sr)
            {
                sr.transform.SetParent(null);
                sr.gameObject.SetActive(false);
                stack.Push(sr);
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

                light.color = Block.blockLightDefaultColor;
                light.pointLightOuterRadius = block.data.lightLevel * 1.5f;
                light.intensity = block.data.lightLevel / 10f;

                return light;
            }

            public void Recover(Light2D light)
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
                chunk.sandboxIndex = PosConvert.ChunkToSandboxIndex(chunkIndex);
                chunk.ComputePoints();

                map.chunks.Add(chunk);
            }

            public void Recover(Chunk chunk)
            {
                map.chunks.Remove(chunk);
                chunk.gameObject.SetActive(false);

                //回收区块里的方块
                chunk.RecoverAllBlocks();

                stack.Push(chunk);
            }
        }

        public BlockPool blockPool;
        public BlockCrackPool blockCrackPool;
        public BlockLightPool blockLightPool;
        public ChunkPool chunkPool;
        public Transform blockPoolTrans { get; private set; }



        public List<Chunk> chunks = new();

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
            //         Vector2Int sbi = ChunkToSandboxIndex(ci);

            //         Debug.Log($"({x}, {y}) {ci}, {sbi}");
            //     }
            // }
        }

        public Chunk AddChunk(Vector2Int chunkIndex)
        {
            foreach (Chunk chunk in chunks)
                if (chunk.chunkIndex == chunkIndex)
                    return chunk;

            return chunkPool.Get(chunkIndex);
        }

        public Block GetBlock(Vector2Int pos, bool isBackground)
        {
            Vector2Int chunkIndexTo = PosConvert.BlockPosToChunkIndex(pos);

            return AddChunk(chunkIndexTo).GetBlock(pos, isBackground);
        }

        public bool TryGetBlock(Vector2Int pos, bool isBackground, out Block b)
        {
            b = GetBlock(pos, isBackground);

            return b;
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
                chunk.RecoverAllBlocks();
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

        public void RecoverChunks()
        {
            for (int i = chunks.Count - 1; i >= 0; i--)
            {
                chunkPool.Recover(chunks[i]);
            }
        }

        public bool HasBlock(Vector2Int pos, bool isBackground)
        {
            foreach (Chunk chunk in chunks)
            {
                foreach (Block block in chunk.blocks)
                {
                    if (block && block.pos == pos && block.isBackground == isBackground)
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
            return GetBlock(PosConvert.WorldToMapPos(pos), isBackground);
        }

        public bool HasBlockWorldPos(Vector2 pos, bool isBackground)
        {
            return HasBlock(PosConvert.WorldToMapPos(pos), isBackground);
        }

        public Vector2Int GetPlayerCursorMapPos(Player player) => PosConvert.WorldToMapPos(player.cursorWorldPos);

        public void SetBlockNet(Vector2Int pos, bool isBackground, string id, string customData) => Client.Send<NMSetBlock>(new(pos, isBackground, id, customData));

#if UNITY_EDITOR
        [Button("放置方块")] private Block EditorSetBlock(Vector2Int pos, bool isBackground, string block, bool editSandbox) => SetBlock(pos, isBackground, ModFactory.CompareBlockDatum(block), null, editSandbox);
#endif

        public Block SetBlock(Vector2Int pos, bool isBackground, BlockData block, string customData, bool editSandbox)
        {
            Chunk chunk = AddChunk(PosConvert.BlockPosToChunkIndex(pos));

            chunk.RemoveBlock(pos, isBackground, editSandbox);

            return chunk.AddBlock(pos, isBackground, block, customData, editSandbox);
        }

        public void DestroyBlockNet(Vector2Int pos, bool isBackground) => SetBlockNet(pos, isBackground, null, null);

        public void DestroyBlock(Vector2Int pos, bool isBackground, bool editSandbox) => SetBlock(pos, isBackground, null, null, editSandbox);

        public Block AddBlock(Vector2Int pos, bool isBackground, BlockData block, string customData, bool editSandbox)
        {
            if (block == null)
                return null;

            Vector2Int chunkIndexTo = PosConvert.BlockPosToChunkIndex(pos);

            return AddChunk(chunkIndexTo).AddBlock(pos, isBackground, block, customData, editSandbox);
        }

        public void RemoveBlock(Vector2Int pos, bool isBackground, bool editSandbox)
        {
            Vector2Int chunkIndexTo = PosConvert.BlockPosToChunkIndex(pos);

            AddChunk(chunkIndexTo).RemoveBlock(pos, isBackground, editSandbox);
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

                if (block)
                {
                    block.OnUpdate();
                }
            }
        }
    }

    [Serializable]
    public class BlockData : ModClass, IEquatable<BlockData>, ITags, IJOFormatCore
    {
        public static float defaultHardness = 5;

        //以 1 为标准
        [LabelText("硬度")] public float hardness;
        [JsonIgnore, LabelText("默认贴图")] public TextureData defaultTexture;
        [LabelText("介绍")] public string description;
        [LabelText("可碰撞")] public bool collidible = true;
        [LabelText("掉落物")] public List<DropData> drops = new();
        [LabelText("光亮等级")] public float lightLevel;

        #region 方块行为

        public string behaviourName;
        public Type behaviourType { get; internal set; }

        #endregion

        #region 接口

        public List<string> tags = new();
        List<string> ITags.tags { get => tags; }

        #endregion

        public bool Equals(BlockData block) => block?.id == id;

        public BlockData()
        {

        }

        public BlockData(string id)
        {
            this.id = id;
            this.hardness = defaultHardness;
            this.defaultTexture = GInit.instance.blockUnknown?.defaultTexture;
            this.collidible = GInit.instance.blockUnknown?.collidible ?? true;
        }

        public BlockData(string id, float hardness) : this(id)
        {
            this.hardness = hardness;
        }

        public BlockData(string id, float hardness, string textureId) : this(id, hardness)
        {
            this.defaultTexture = new(textureId);
        }

        public BlockData(string id, float hardness, string textureId, bool collidible) : this(id, hardness, textureId)
        {
            this.collidible = collidible;
        }

        public override string ToString()
        {
            StringBuilder sb = Tools.stringBuilderPool.Get();

            string content = ToString(sb);
            Tools.stringBuilderPool.Recover(sb);
            return content;
        }

        public string ToString(StringBuilder sb)
        {
            string indentation = "   ";

            sb.AppendLine("{");

            sb.Append(indentation).Append("id: ").AppendLine(id);
            sb.Append(indentation).Append("hardness: ").AppendLine(hardness.ToString());
            sb.Append(indentation).Append("defaultTexture: ").AppendLine(defaultTexture?.ToString());
            sb.Append(indentation).Append("collidible: ").AppendLine(collidible.ToString());

            sb.Append(indentation).AppendLine("tags");
            sb.Append(indentation).AppendLine("{");
            indentation += "   ";
            for (int i = 0; i < tags.Count; i++)
            {
                sb.Append(indentation).Append("E").Append(i).AppendLine(tags[i]);
            }
            indentation = "   ";
            sb.Append(indentation).AppendLine("}");

            sb.AppendLine("}");

            return sb.ToString();
        }

#if UNITY_EDITOR
        [Button("输出方块信息")] public void EditorOutputDatum() => Debug.Log(ToString());

        [Button]
        private void OutputBehaviourInfo()
        {
            Debug.Log($"{behaviourName}, {behaviourType == null}");
        }
#endif
    }

    public static class PosConvert
    {
        public static Vector2Int WorldToMapPos(Vector2 vec)
        {
            return new((int)Math.Round(vec.x), (int)Math.Round(vec.y));
        }

        public static Vector2Int BlockPosToChunkIndex(Vector2Int pos)
        {
            //排除初始区块的影响 (初始区块只占了一半)
            float xDelta = pos.x > 0 ? Chunk.halfBlockCountPerAxis : -Chunk.halfBlockCountPerAxis;
            float yDelta = pos.y > 0 ? Chunk.halfBlockCountPerAxis : -Chunk.halfBlockCountPerAxis;

            return new((int)((pos.x + xDelta) * Chunk.blockCountPerAxisReciprocal), (int)((pos.y + yDelta) * Chunk.blockCountPerAxisReciprocal));
        }

        public static Vector2Int ChunkToSandboxIndex(Vector2Int index)
        {
            //排除初始沙盒的影响 (初始沙盒只占了一半)
            float xDelta = index.x > 0 ? Sandbox.halfChunkCountX : -Sandbox.halfChunkCountX;
            float yDelta = index.y > 0 ? Sandbox.halfChunkCountY : -Sandbox.halfChunkCountY;

            return new((int)((index.x + xDelta) * Sandbox.chunkCountXReciprocal), (int)((index.y + yDelta) * Sandbox.chunkCountYReciprocal));
        }

        public static Vector2Int WorldPosToSandboxIndex(Vector2 pos)
        {
            return ChunkToSandboxIndex(BlockPosToChunkIndex(new((int)pos.x, (int)pos.y)));
        }

        public static Vector2Int MapToSandboxPos(this Chunk chunk, Vector2Int mapPos)
        {
            return new(mapPos.x - chunk.sandboxMiddleX, mapPos.y - chunk.sandboxMiddleY);
        }

        public static Vector2Int MapToSandboxPos(Vector2Int mapPos, Vector2Int sandboxIndex)
        {
            return new(mapPos.x - Sandbox.GetMiddleX(sandboxIndex), mapPos.y - Sandbox.GetMiddleY(sandboxIndex));
        }
    }
}
