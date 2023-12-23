using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;
using SP.Tools;
using System.Threading.Tasks;

namespace GameCore.High
{
    public class Chunk : MonoBehaviour
    {
        public const int blockCountPerAxis = 24 + 1;
        public const float blockCountPerAxisReciprocal = 1f / blockCountPerAxis;
        public const float halfBlockCountPerAxis = blockCountPerAxis / 2f;
        public const int blockCountSingleLayer = blockCountPerAxis * blockCountPerAxis;
        public const int blockCountMultiLayer = blockCountSingleLayer * 2;

        [LabelText("方块")] public Block[] blocks = new Block[blockCountMultiLayer];
        public Vector2Int chunkIndex { get; internal set; }
        public bool totalRendererEnabled = true;
        //public bool collidersEnabled = true;
        public Vector2Int regionIndex;
        public Map map => Map.instance;

        public float left { get; private set; }
        public float right { get; private set; }

        public float up { get; private set; }
        public float down { get; private set; }

        public int regionMiddleX;
        public int regionMiddleY;

        public bool outOfView { get; private set; }

        #region 实现接口
        public Tools tools => Tools.instance;
        public GM managerGame => GM.instance;
        #endregion

        public void ComputePoints()
        {
            left = transform.position.x - halfBlockCountPerAxis - 0.5f;
            right = transform.position.x + halfBlockCountPerAxis + 0.5f;
            up = transform.position.y + halfBlockCountPerAxis + 0.5f;
            down = transform.position.y - halfBlockCountPerAxis - 0.5f;

            regionMiddleX = Region.GetMiddleX(regionIndex);
            regionMiddleY = Region.GetMiddleY(regionIndex);
        }

        public bool OutOfView()
        {
            return left > tools.viewRightSideWorldPos || right < tools.viewLeftSideWorldPos || down > tools.viewUpSideWorldPos || up < tools.viewDownSideWorldPos;
            //return tools.IsInView2D(leftUpPoint) || tools.IsInView2D(leftDownPoint) || tools.IsInView2D(rightUpPoint) || tools.IsInView2D(rightDownPoint);
        }

        private void Start()
        {
            EnableRenderers();
        }

        private void Update()
        {
            if (!tools.mainCamera)
                return;

            outOfView = OutOfView();

            if (outOfView)
            {
                if (totalRendererEnabled)
                {
                    DisableRenderers();
                }
            }
            else
            {
                if (!totalRendererEnabled)
                {
                    EnableRenderers();
                }
            }
        }


        private void FixedUpdate()
        {
            if (!Player.GetLocal(out Player localPlayer) || SyncPacker.vars.Count == 0 || !localPlayer.TryGetRegion(out _))
            {
                return;
            }

            /* ---------------------------------- 为方块回血 --------------------------------- */
            foreach (var block in blocks)
            {
                if (block == null)
                    continue;

                if (Tools.time >= block.lastDamageTime + 7.5f)
                {
                    block.health += 0.2f;
                }
            }
        }

        public void RecoverTotalRegion()
        {
            //删除区块所属的区域
            GM.instance.RecoverRegion(regionIndex);
        }

        [Button("关闭所有渲染器")] public void DisableRenderers() => SetRenderersEnabled(this, false);

        [Button("打开所有渲染器")] public void EnableRenderers() => SetRenderersEnabled(this, true);

        public static Action<Chunk, bool> SetRenderersEnabled = (chunk, e) =>
        {
            foreach (Block block in chunk.blocks)
            {
                if (block && block.sr)
                {
                    block.sr.enabled = e;
                }
            }

            chunk.totalRendererEnabled = e;
        };

        public Block GetBlock(Vector2Int mapPos, bool isBackground)
        {
            foreach (Block block in blocks)
            {
                if (block && block.pos == mapPos && block.isBackground == isBackground)
                {
                    return block;
                }
            }

            return null;
        }

        public bool TryGetBlock(Vector2Int mapPos, bool isBackground, out Block block)
        {
            block = GetBlock(mapPos, isBackground);

            return block;
        }

        public void RecoverAllBlocks()
        {
            foreach (Block block in blocks)
            {
                if (block)
                {
                    Map.instance.blockPool.Recover(block);
                }
            }
        }

        public void RemoveBlock(Vector2Int pos, bool isBackground, bool editRegion)
        {
            foreach (var block in blocks)
            {
                if (block && block.pos == pos && block.isBackground == isBackground)
                {
                    if (editRegion && Server.isServer)
                    {
                        Vector2Int blockChunkPos = this.MapToRegionPos(block.pos);

                        //在区域中删除
                        Region region = GFiles.world.GetOrAddRegion(regionIndex);
                        region.RemovePos(block.data.id, blockChunkPos, block.isBackground);
                    }

                    map.blockPool.Recover(block);

                    GameCallbacks.OnRemoveBlock(pos, isBackground, editRegion, true);
                    return;
                }
            }

            GameCallbacks.OnRemoveBlock(pos, isBackground, editRegion, false);
        }

        public Block AddBlock(Vector2Int pos, bool isBackground, BlockData block, string customData, bool editRegion)
        {
            if (block == null)
                return null;

            Block b = map.blockPool.Get(pos, isBackground, block, customData);

            if (editRegion && Server.isServer)
            {
                Vector2Int blockSBPos = this.MapToRegionPos(pos);

                //在区域中添加
                Region region = GFiles.world.GetOrAddRegion(regionIndex);
                region.AddPos(block.id, new(blockSBPos.x, blockSBPos.y), isBackground);
            }

            GameCallbacks.OnAddBlock(pos, isBackground, b, this);
            return b;
        }
    }
}
