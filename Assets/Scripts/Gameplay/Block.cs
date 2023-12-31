using Sirenix.OdinInspector;
using UnityEngine;
using System;
using GameCore.High;
using SP.Tools.Unity;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SP.Tools;
using UnityEngine.Rendering.Universal;
using System.Collections;
using DG.Tweening;

namespace GameCore
{
    public class Block
    {
        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static Color backgroundColor = new(0.6f, 0.6f, 0.7f);
        public static Color wallColor = new(1f, 1f, 1f);
        public static Vector3 blockDamageScale = new(1.2f, 1.2f, 1.2f);





        /* -------------------------------------------------------------------------- */
        /*                                  Instance                                  */
        /* -------------------------------------------------------------------------- */
        public float health;

        public Chunk chunk { get; internal set; }
        [LabelText("上次受伤时间")] public float lastDamageTime;
        public BlockData data;
        public SpriteRenderer crackSr { get; internal set; }
        public Light2D blockLight { get; internal set; }
        public DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> scaleAnimationTween;


        public Vector2Int pos { get; internal set; }
        public bool isBackground { get; internal set; }
        [LabelText("自定义数据")] public JObject customData;

        public Tweener shakeRotationTween;
        public Transform transform;
        public GameObject gameObject;
        public SpriteRenderer sr { get; internal set; }
        public BoxCollider2D blockCollider { get; internal set; }



        #region Static

        public static float totalMaxHealth = 100;
        public static Action<Block> OnHealthChange = (block) =>
        {
            //(-∞,0]
            if (block.health <= 0)
            {
                block.Destroy();
            }
            //(0,100)
            else if (block.health != 100)
            {
                if (!block.chunk.map.blocksToCheckHealths.Contains(block))
                    block.chunk.map.blocksToCheckHealths.Add(block);
            }
            //100
            else if (block.chunk.map.blocksToCheckHealths.Contains(block))
            {
                block.chunk.map.blocksToCheckHealths.Remove(block);
            }
        };

        public static int blockLayer { get; private set; }
        public static int blockLayerMask { get; private set; }

        #endregion



        public void WriteCustomDataToSave()
        {
            //写入存档中
            GFiles.world.GetRegion(chunk.regionIndex).GetBlock(PosConvert.MapToRegionPos(pos, chunk.regionIndex), isBackground).customData = customData?.ToString(Formatting.None);
        }

        public virtual void OutputDrops(Vector3 pos)
        {
            data.drops.For(drop =>
            {
                GM.instance.SummonDrop(pos, drop.id, drop.count, customData?.ToString());
            });
        }

        public virtual void OnRecovered()
        {

        }

        public virtual void OnEntityStay(Entity entity)
        {

        }

        public virtual void OnServerSetCustomData()
        {

        }

        public virtual bool PlayerInteraction(Player player)
        {
            return false;
        }

        public virtual void DoStart()
        {
            sr.sprite = data.defaultTexture.sprite;
            blockCollider.isTrigger = !data.collidible || isBackground;
        }

        public virtual void OnUpdate()
        {
            //生成世界时候这个方法也会被调用, 因此要检查 chunk
            if (crackSr)
                chunk.map.blockCrackPool.Recover(crackSr);

            if (health != 100)
                crackSr = chunk.map.blockCrackPool.Get(this, (byte)Mathf.Min(4, 4 - health / totalMaxHealth * 5));
        }


        public void TakeDamage(float damage)
        {
            lastDamageTime = Time.time;
            SetHealth(health - damage / data.hardness);
            scaleAnimationTween = transform.DOScale(blockDamageScale, 0.1f).OnStepComplete(() => transform.DOScale(Vector3.one, 0.1f));
            shakeRotationTween = transform.DOShakeRotation(0.1f, new Vector3(0, 0, 15));
        }

        public void SetHealth(float value)
        {
            if (health == value)
                return;

            if (value > totalMaxHealth)
                value = totalMaxHealth;

            health = value;
            OnHealthChange(this);
            OnUpdate();
        }


        public void Destroy()
        {
            Client.Send<NMDestroyBlock>(new(chunk.regionIndex, pos, isBackground));
        }

        [RuntimeInitializeOnLoadMethod]
        static void BindMethod()
        {
            blockLayer = LayerMask.NameToLayer("Block");
            blockLayerMask = blockLayer.LayerMaskOnly();
        }

        public static bool TryGetBlockFromCollision(Collider2D other, out Block block)
        {
            if (other.gameObject.layer == blockLayer)
            {
                var mapPos = PosConvert.WorldToMapPos(other.transform.position);
                var chunk = Map.instance.AddChunk(PosConvert.MapPosToChunkIndex(mapPos));

                //先尝试获取背景层
                block = chunk.GetBlock(mapPos, true);

                //如果获取失败, 就从墙层获取
                if (block == null || block.gameObject != other.gameObject)
                    block = chunk.GetBlock(mapPos, false);

                if (block == null)
                {
                    Debug.LogError("碰撞到了方块，但是无法获取碰撞到的方块");
                    return false;
                }

                return true;
            }

            block = null;
            return false;
        }
    }
}
