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
    public class Block : MonoBehaviour, IEntity
    {
        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static Color backgroundColor = new(0.6f, 0.6f, 0.7f);
        public static Color wallColor = new(1f, 1f, 1f);
        public static Vector3 blockDamageScale = new(1.2f, 1.2f, 1.2f);
        public static Color blockLightDefaultColor = Tools.HexToColor("#FFA578");





        /* -------------------------------------------------------------------------- */
        /*                                  Instance                                  */
        /* -------------------------------------------------------------------------- */
        private float _health;
        public float health
        {
            get => _health;
            set
            {
                if (_health == value)
                    return;

                if (value > totalMaxHealth)
                    value = totalMaxHealth;

                _health = value;
                OnHealthChange(this);
                OnUpdate();
            }
        }

        public Chunk chunk { get; internal set; }
        [LabelText("上次受伤时间")] public float lastDamageTime;
        public BlockData data;
        public SpriteRenderer sr { get; internal set; }
        public SpriteRenderer crackSr { get; internal set; }
        public Light2D blockLight { get; internal set; }


        public Vector2Int pos { get; internal set; }
        public bool isBackground { get; internal set; }
        public BoxCollider2D blockCollider { get; internal set; }
        [LabelText("自定义数据")] public JObject customData;

        public DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> scaleAnimationTween;
        public Tweener shakeRotationTween;



        #region Static

        public static float totalMaxHealth = 100;
        public static Action<Block> OnHealthChange = (block) =>
        {
            if (block.health <= 0)
            {
                block.Death();
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

        #region Behaviour

        public virtual void OutputDrops(Vector3 pos)
        {
            data.drops.For(drop =>
            {
                GM.instance.SummonItem(pos, drop.id, drop.count, customData?.ToString());
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

            if (health < 100)
                crackSr = chunk.map.blockCrackPool.Get(this, (byte)Mathf.Min(4, 4 - health / totalMaxHealth * 5));
        }

        #endregion


        public void TakeDamage(float damage)
        {
            lastDamageTime = Time.time;
            health -= damage / data.hardness / 2;
            scaleAnimationTween = transform.DOScale(blockDamageScale, 0.1f).OnStepComplete(() => transform.DOScale(Vector3.one, 0.1f));
            shakeRotationTween = transform.DOShakeRotation(0.1f, new Vector3(0, 0, 15));
        }

        private void OnDestroy()
        {
            scaleAnimationTween?.Kill();
            shakeRotationTween?.Kill();
        }

        public void Death()
        {
            Client.Send<NMDestroyBlock>(new(chunk.regionIndex, pos, isBackground));
        }

        [RuntimeInitializeOnLoadMethod]
        static void BindMethod()
        {
            blockLayer = LayerMask.NameToLayer("Block");
            blockLayerMask = blockLayer.LayerMaskOnly();
        }
    }
}
