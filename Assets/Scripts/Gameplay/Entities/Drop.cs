using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;

namespace GameCore
{
    [EntityBinding(EntityID.Drop), NotSummonable]
    public sealed class Drop : Entity
    {
        private SpriteRenderer _spriteRenderer;
        public SpriteRenderer spriteRenderer { get { if (!_spriteRenderer) _spriteRenderer = gameObject.GetOrAddComponent<SpriteRenderer>(); return _spriteRenderer; } }

        [BoxGroup("属性")]
        [LabelText("物品")]
        public Item item;

        public float minTimeToPickUp { get; private set; }


        protected override void Awake()
        {
            base.Awake();

            isHurtable = false;

            renderers.Add(spriteRenderer);
            spriteRenderers.Add(spriteRenderer);

            transform.localScale = new(0.5f, 0.5f);
            transform.SetLocalPosZ(-0.001f);
        }

        public override void Initialize()
        {
            base.Initialize();

            SummonSetup();
        }



        public bool CanBePickedUp()
        {
            // 1秒后可拾取
            return Tools.time >= minTimeToPickUp && !isDead;
        }



        [Button("初始化")]
        public void SummonSetup()
        {
            //如果是被玩家抛出的物品，则设置最早的可拾起时间为 1s 后
            if (customData.TryGetJToken("ori:drop", out var dropData) &&
                dropData.TryGetJToken("is_thrown_by_player", out var isThrownByPlayer) && isThrownByPlayer.ToBool())
            {
                minTimeToPickUp = Tools.time + 1;
            }

            /* ---------------------------------- 切割字符串 --------------------------------- */
            if (ConvertStringItem(customData?["ori:item_data"].ToString(), out string id, out ushort count, out string itemCustomData, out string error))
            {
                /* ---------------------------------- 获取物品 ---------------------------------- */
                item = ModFactory.CompareItem(id).DataToItem();

                if (item == null)
                {
                    Debug.LogError($"{nameof(Drop)}.{nameof(SummonSetup)}: 未匹配到物品 {id}");
                    return;
                }

                /* ---------------------------------- 改变属性 ---------------------------------- */
                item.count = count;
                item.customData = JsonUtils.LoadJObjectByString(itemCustomData);

                if (item?.data?.texture != null)
                {
                    spriteRenderer.sprite = item.data.texture.sprite;
                }
            }
            else
            {
                Debug.LogError(error);
            }
        }



        public static bool ConvertStringItem(string str, out string id, out ushort count, out string customData, out string error)
        {
            if (str.IsNullOrWhiteSpace())
            {
                id = null;
                count = 0;
                customData = null;
                error = $@"物品为空";
                return false;
            }

            string[] sSplitted = str.Split(@"/=/");

            if (sSplitted == null || sSplitted.Length != 3)
            {
                id = null;
                count = 0;
                customData = null;
                error = $@"{nameof(str)} 的元素应被 /=/ 切割, 例如 ori:dirt/=/2, ori:dirt 为物品id, 2为物品数量";
                return false;
            }

            id = sSplitted[0];
            count = ushort.TryParse(sSplitted[1], out ushort result) ? result : (ushort)1;
            customData = sSplitted[2];

            if (id.IsNullOrWhiteSpace())
            {
                error = $@"{nameof(str)} 的一个元素为空";
                return false;
            }

            error = null;
            return true;
        }
    }
}

