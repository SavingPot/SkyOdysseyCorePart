using System.Collections.Generic;
using GameCore.High;
using UnityEngine;

namespace GameCore
{
    public static class EntityInventoryOwnerBehaviour
    {
        public static readonly Vector2 defaultItemLocalScale = new(1.2f, 1.2f);



        public static void SwitchUsingItemTo<T>(T entity, int index) where T : Entity, IInventoryOwner
        {
            var inventory = entity.GetInventory();

            if (inventory != null)
            {
                //切出回调
                if (inventory.TryGetItemBehaviour(entity.usingItemIndex, out var itemBehaviour))
                    itemBehaviour.OnSwitchFromThis();

                entity.usingItemIndex = index;

                //切入回调
                if (inventory.TryGetItemBehaviour(index, out itemBehaviour))
                    itemBehaviour.OnSwitchToThis();
            }
            else
            {
                throw new System.NullReferenceException($"实体 {entity.data.id} 没有物品栏");
            }
        }

        public static void OnUpdate<T>(T entity) where T : Entity, IInventoryOwner
        {
            var inventory = entity.GetInventory();

            if (inventory != null)
            {
                //装备
                inventory?.DoBehaviours();

                //使用中的物品
                if (inventory.TryGetItemBehaviour(entity.usingItemIndex, out var usingBehaviour))
                    usingBehaviour.OnHand();
            }
        }

        public static void RefreshItemRenderers<T>(T entity) where T : Entity, IInventoryOwner
        {
            //缓存物品栏以保证性能
            var inventoryTemp = entity.GetInventory();
            if (inventoryTemp == null)
            {
                Debug.LogError($"实体 {entity.gameObject.name} 的物品栏为空");
                return;
            }


            /* ---------------------------------- 渲染盾牌 ---------------------------------- */
            if (!Item.Null(inventoryTemp.shield) && inventoryTemp.shield.data.Shield != null)
            {
                entity.usingShieldRenderer.sprite = inventoryTemp.shield.data.texture.sprite;
            }
            else
            {
                entity.usingShieldRenderer.sprite = null;
            }

            /* --------------------------------- 渲染手部物品 --------------------------------- */
            if (inventoryTemp.TryGetItemBehaviour(entity.usingItemIndex, out var usingBehaviour))
            {
                usingBehaviour.Rendering(entity.usingItemRenderer);
            }
            else
            {
                entity.usingItemRenderer.sprite = null;
            }
        }

        public static void RefreshInventory<T>(T entity) where T : Entity, IInventoryOwner, IHumanBodyParts<CreatureBodyPart>
        {
            //缓存物品栏以保证性能
            var inventoryTemp = entity.GetInventory();
            if (inventoryTemp == null)
            {
                Debug.LogError($"实体 {entity.gameObject.name} 的物品栏为空");
                return;
            }



            //渲染手部物品
            RefreshItemRenderers(entity);

            //刷新盔甲的贴图
            CreatureHumanBodyPartsBehaviour.RefreshArmors(entity, inventoryTemp.helmet?.data, inventoryTemp.breastplate?.data, inventoryTemp.legging?.data, inventoryTemp.boots?.data);
        }

        public static void CreateItemRenderers<T>(T entity, Transform shieldParent, Transform itemParent, int usingShieldSortingOrder, int usingItemSortingOrder) where T : Entity, IInventoryOwner
        {
            entity.usingShieldRenderer = ObjectTools.CreateSpriteObject(shieldParent, "UsingShield");
            entity.usingShieldRenderer.sortingOrder = usingShieldSortingOrder;
            entity.renderers.Add(entity.usingShieldRenderer);
            entity.spriteRenderers.Add(entity.usingShieldRenderer);

            entity.usingItemRenderer = ObjectTools.CreateSpriteObject(itemParent, "UsingItem");
            entity.usingItemRenderer.sortingOrder = usingItemSortingOrder;
            entity.renderers.Add(entity.usingItemRenderer);
            entity.spriteRenderers.Add(entity.usingItemRenderer);

            entity.usingItemCollider = entity.usingItemRenderer.gameObject.AddComponent<BoxCollider2D>();
            entity.usingItemCollider.isTrigger = true;

            entity.usingItemCollisionComponent = entity.usingItemRenderer.gameObject.AddComponent<InventoryItemRendererCollision>();
            entity.usingItemCollisionComponent.owner = entity;

            entity.ModifyUsingShieldRendererTransform();
            entity.ModifyUsingItemRendererTransform(Vector2.zero, defaultItemLocalScale, 0);
        }

        public static void LoadInventoryFromCustomData<T>(T entity) where T : Entity, IInventoryOwner
        {
            /* -------------------------------------------------------------------------- */
            /*                                //修正 JObject                                */
            /* -------------------------------------------------------------------------- */
            var customData = entity.customData ?? new();

            if (customData["ori:inventory"] == null)
                customData.AddObject("ori:inventory");
            if (customData["ori:inventory"]["data"] == null)
            {
                customData["ori:inventory"].AddProperty("data", JsonUtils.ToJToken(entity.DefaultInventory()));
            }

            entity.customData = customData;

            /* -------------------------------------------------------------------------- */
            /*                                    缓存数据                                    */
            /* -------------------------------------------------------------------------- */
            var data = customData["ori:inventory"]["data"];

            /* -------------------------------------------------------------------------- */
            /*                                    读取数据                                    */
            /* -------------------------------------------------------------------------- */
            Inventory inventory = data.ToObject<Inventory>();
            inventory ??= entity.DefaultInventory(); //这一行不是必要的, inventory 通常不会为空, 但是我们要保证代码 100% 正常运行
            inventory.SetSlotCount(entity.inventorySlotCount);
            entity.SetInventory(Inventory.ResumeFromStreamTransport(inventory, entity));
        }

        public static void WriteInventoryToCustomData<T>(this T entity) where T : Entity, IInventoryOwner
        {
            entity.customData["ori:inventory"]["data"] = JsonUtils.ToJson(entity.GetInventory());
        }
    }

    public interface IInventoryOwner : IItemContainer
    {
        Transform transform { get; }
        bool AttackEntity(Entity entity);
        bool isAttacking { get; }



        int inventorySlotCount { get; }
        void OnInventoryItemChange(Inventory newValue, string index); //* 为什么必须提供一个 newValue? Inventory 明明是引用类型, 我直接访问 Inventory 的变量不就好了吗? 这是受限于网络传输的需要, 详见 Player.OnInventoryItemChange
        Inventory GetInventory();
        void SetInventory(Inventory value); //! 需要对传入的 value 参数进行 ResumeFromStream
        Inventory DefaultInventory() => new(inventorySlotCount, this);



        int usingItemIndex { get; set; }
        SpriteRenderer usingShieldRenderer { get; set; }
        SpriteRenderer usingItemRenderer { get; set; }
        BoxCollider2D usingItemCollider { get; set; }
        InventoryItemRendererCollision usingItemCollisionComponent { get; set; }
        void ModifyUsingShieldRendererTransform();
        void ModifyUsingItemRendererTransform(Vector2 localPosition, Vector2 localScale, int localRotation);



        int TotalDefense { get; }
    }



    public class InventoryItemRendererCollision : MonoBehaviour
    {
        public IInventoryOwner owner;
        readonly List<Entity> entitiesInside = new();

        private void Update()
        {
            //? 这里为什么要遍历 entitiesInside 而不是直接在碰撞体进入时攻击呢?
            //? 因为如果在碰撞体进入时攻击会导致玩家开始攻击前进入武器范围的实体不会被攻击。
            if (owner.isAttacking)
            {
                for (int i = entitiesInside.Count - 1; i >= 0; i--)
                {
                    owner.AttackEntity(entitiesInside[i]);

                    //攻击完后移除实体避免二次攻击
                    entitiesInside.RemoveAt(i);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.transform == owner.transform)
                return;

            if (other.gameObject.TryGetComponent(out Entity entity))
            {
                entitiesInside.Add(entity);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.transform == owner.transform)
                return;

            if (other.gameObject.TryGetComponent(out Entity entity))
            {
                entitiesInside.Remove(entity);
            }
        }
    }
}