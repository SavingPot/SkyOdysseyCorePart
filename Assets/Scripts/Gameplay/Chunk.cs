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
        public const int blockCountPerAxis = 17;
        public const float blockCountPerAxisReciprocal = 1f / blockCountPerAxis;
        public const float halfBlockCountPerAxis = blockCountPerAxis / 2f;
        public const int blockCountSingleLayer = blockCountPerAxis * blockCountPerAxis;
        public const int blockCountMultiLayer = blockCountSingleLayer * 2;

        [LabelText("方块")] public Block[] blocks = new Block[blockCountMultiLayer];
        public Vector2Int chunkIndex { get; internal set; }
        public bool totalRendererEnabled = true;
        //public bool collidersEnabled = true;
        public Vector2Int sandboxIndex;
        public Map map => Map.instance;

        public float left { get; private set; }
        public float right { get; private set; }

        public float up { get; private set; }
        public float down { get; private set; }

        public Vector3 leftUpPoint { get; private set; }
        public Vector3 leftDownPoint { get; private set; }
        public Vector3 rightUpPoint { get; private set; }
        public Vector3 rightDownPoint { get; private set; }

        public int sandboxMiddleX;
        public int sandboxMiddleY;

        public bool inView { get; private set; }

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

            leftUpPoint = new(left, up);
            leftDownPoint = new(left, down);
            rightUpPoint = new(right, up);
            rightDownPoint = new(right, down);

            sandboxMiddleX = Sandbox.GetMiddleX(sandboxIndex);
            sandboxMiddleY = Sandbox.GetMiddleY(sandboxIndex);
        }

        public bool InView()
        {
            return tools.IsInView2D(leftUpPoint) || tools.IsInView2D(leftDownPoint) || tools.IsInView2D(rightUpPoint) || tools.IsInView2D(rightDownPoint);
        }

        private void Start()
        {
            EnableRenderers();
        }

        private void Update()
        {
            if (!tools.mainCamera)
                return;

            inView = InView();

            if (!inView)
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

        private bool ShouldRecoverSandbox(Player localPlayer)
        {
            int deltaX = Mathf.Abs(localPlayer.sandboxIndex.x - sandboxIndex.x);
            int deltaY = Mathf.Abs(localPlayer.sandboxIndex.y - sandboxIndex.y);

            if (deltaX > 1 || deltaY > 1)
            {
                //如果是服务器的话, 还要考虑别的玩家离这里禁不禁
                if (Server.isServer)
                {
                    foreach (Player player in PlayerCenter.all)
                    {
                        if (player == localPlayer)
                            continue;

                        deltaX = Mathf.Abs(player.sandboxIndex.x - sandboxIndex.x);
                        deltaY = Mathf.Abs(player.sandboxIndex.y - sandboxIndex.y);

                        if (deltaX <= 1 || deltaY <= 1)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }


        private void FixedUpdate()
        {
            if (!Player.GetLocal(out Player localPlayer) || SyncPacker.vars.Count == 0 || !localPlayer.TryGetSandbox(out Sandbox sb))
            {
                return;
            }

            /* ---------------------------------- 回收沙盒 ---------------------------------- */
            if (localPlayer.sandboxIndex != sandboxIndex)
            {
                //执行回收沙盒 (生成沙盒时不回收)
                if (!managerGame.generatingExistingSandbox)
                {
                    if (ShouldRecoverSandbox(localPlayer))
                    {
                        RecoverTotalSandbox();
                    }
                }

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

        public void RecoverTotalSandbox()
        {
            //删除区块所属的沙盒
            GM.instance.RecoverSandbox(sandboxIndex);
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

        public void RemoveBlock(Vector2Int pos, bool isBackground, bool editSandbox)
        {
            foreach (var block in blocks)
            {
                if (block && block.pos == pos && block.isBackground == isBackground)
                {
                    if (editSandbox && Server.isServer)
                    {
                        Vector2Int blockChunkPos = this.MapToSandboxPos(block.pos);

                        //在沙盒中删除
                        Sandbox sb = GFiles.world.GetOrAddSandbox(sandboxIndex);
                        sb.RemovePos(block.data.id, blockChunkPos, block.isBackground);
                    }

                    map.blockPool.Recover(block);

                    GameCallbacks.OnRemoveBlock(pos, isBackground, editSandbox, true);
                    return;
                }
            }

            GameCallbacks.OnRemoveBlock(pos, isBackground, editSandbox, false);
        }

        public Block AddBlock(Vector2Int pos, bool isBackground, BlockData block, string customData, bool editSandbox)
        {
            if (block == null)
                return null;

            Block b = map.blockPool.Get(pos, isBackground, block, customData);

            if (editSandbox && Server.isServer)
            {
                Vector2Int blockSBPos = this.MapToSandboxPos(pos);

                //在沙盒中添加
                Sandbox sb = GFiles.world.GetOrAddSandbox(sandboxIndex);
                sb.AddPos(block.id, new(blockSBPos.x, blockSBPos.y), isBackground);
            }

            GameCallbacks.OnAddBlock(pos, isBackground, b, this);
            return b;
        }
    }
}
