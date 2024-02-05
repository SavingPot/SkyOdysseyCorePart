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
    public sealed class Chunk : MonoBehaviour
    {
        public const int blockCountPerAxis = 24 + 1;
        public const float blockCountPerAxisReciprocal = 1f / blockCountPerAxis;
        public const float halfBlockCountPerAxis = blockCountPerAxis / 2f;
        public const float negativeHalfBlockCountPerAxis = -halfBlockCountPerAxis;
        public const int blockCountSingleLayer = blockCountPerAxis * blockCountPerAxis;
        public const int blockCountMultiLayer = blockCountSingleLayer * 2;

        [LabelText("方块")] public readonly Block[] blocks = new Block[blockCountMultiLayer];
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

        public bool IsOutOfView()
        {
            return left > tools.viewRightSideWorldPos || right < tools.viewLeftSideWorldPos || down > tools.viewUpSideWorldPos || up < tools.viewDownSideWorldPos;
            //return tools.IsInView2D(leftUpPoint) || tools.IsInView2D(leftDownPoint) || tools.IsInView2D(rightUpPoint) || tools.IsInView2D(rightDownPoint);
        }

        private void Start()
        {
            EnableRenderers();
        }

        public void RecoverTotalRegion()
        {
            //删除区块所属的区域
            GM.instance.RecoverRegion(regionIndex);
        }

        [Button("关闭所有渲染器")] public void DisableRenderers() => SetRenderersEnabled(this, false);

        [Button("打开所有渲染器")] public void EnableRenderers() => SetRenderersEnabled(this, true);

        public static Action<Chunk, bool> SetRenderersEnabled = (chunk, value) =>
        {
            foreach (Block block in chunk.blocks)
            {
                if (block != null && block.sr)
                {
                    block.sr.enabled = value;
                }
            }

            chunk.totalRendererEnabled = value;
        };

        public Block GetBlock(Vector2Int mapPos, bool isBackground)
        {
            foreach (Block block in blocks)
            {
                if (block != null && block.pos == mapPos && block.isBackground == isBackground)
                {
                    return block;
                }
            }

            return null;
        }

        public bool TryGetBlock(Vector2Int mapPos, bool isBackground, out Block block)
        {
            block = GetBlock(mapPos, isBackground);

            return block != null;
        }

        public void RecoverAllBlocks()
        {
            foreach (Block block in blocks)
            {
                if (block != null)
                {
                    Map.instance.blockPool.Recover(block);
                }
            }
        }

        public void RemoveBlock(Vector2Int pos, bool isBackground, bool editRegion, bool executeBlockUpdate)
        {
            foreach (var block in blocks)
            {
                if (block != null && block.pos == pos && block.isBackground == isBackground)
                {
                    if (editRegion && Server.isServer)
                    {
                        Vector2Int blockRegionPos = this.MapToRegionPos(block.pos);

                        //在区域中删除
                        Region region = GFiles.world.GetOrAddRegion(regionIndex);
                        region.RemovePos(block.data.id, blockRegionPos.x, blockRegionPos.y, block.isBackground);
                    }

                    map.blockPool.Recover(block);

                    GameCallbacks.OnRemoveBlock(pos, isBackground, editRegion, true);
                    return;
                }
            }

            if (executeBlockUpdate)
            {
                map.UpdateAt(pos, isBackground);
            }

            GameCallbacks.OnRemoveBlock(pos, isBackground, editRegion, false);
        }

        public Block AddBlock(Vector2Int pos, bool isBackground, BlockData blockData, string customData, bool editRegion, bool executeBlockUpdate)
        {
            if (blockData == null)
                return null;

            Block b = map.blockPool.Get(pos, isBackground, blockData, customData);

            if (editRegion && Server.isServer)
            {
                Vector2Int blockRegionPos = this.MapToRegionPos(pos);

                //在区域中添加
                Region region = GFiles.world.GetOrAddRegion(regionIndex);
                region.AddPos(blockData.id, blockRegionPos.x, blockRegionPos.y, isBackground);
            }

            if (executeBlockUpdate)
            {
                map.UpdateAt(b.pos, b.isBackground);
            }

            GameCallbacks.OnAddBlock(pos, isBackground, b, this);
            return b;
        }
    }
}
