using GameCore.UI;
using UnityEngine;

namespace GameCore
{
    public class ItemBehaviour
    {
        public IInventoryOwner owner;
        public Item instance;
        public string inventoryIndex;

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }

        public void UseAsBlock(Vector2Int pos, bool isBackground)
        {
            if (owner is Player player)
            {
                //检测是否有方块
                if (player.map.HasBlock(pos, isBackground))
                    return;

                //放置方块
                player.map.SetBlockNet(pos, isBackground, instance.data.id, instance.customData?.ToString());

                //手柄震动
                if (GControls.mode == ControlMode.Gamepad)
                    GControls.GamepadVibrationSlightMedium();

                //减少物品数量
                player.ServerReduceItemCount(inventoryIndex, 1);

                //播放音效
                GAudio.Play(AudioID.PlaceBlock);

                //播放手臂动画
                if (!player.animWeb.GetAnim("attack_rightarm", 0).isPlaying)
                    player.animWeb.SwitchPlayingTo("attack_rightarm");
            }
        }

        public virtual bool Use(Vector2 point)
        {
            if (owner is Player player)
            {
                //放置方块
                if (instance.data.isBlock && player.IsPointInteractable(point) && !player.map.HasBlock(PosConvert.WorldToMapPos(point), player.isControllingBackground))
                {
                    UseAsBlock(PosConvert.WorldToMapPos(point), player.isControllingBackground);

                    return true;
                }
                //进食
                else if (instance.data.Edible().hasTag)
                {
                    player.health = Mathf.Min(player.health + instance.data.Edible().tagValue, player.maxHealth);

                    player.ServerReduceItemCount(inventoryIndex, 1);

                    GAudio.Play(AudioID.Eat);

                    return true;
                }
                //头盔
                else if (instance.data.Helmet != null)
                {
                    player.ServerSwapItem(inventoryIndex, Inventory.helmetVar);
                    GAudio.Play(AudioID.WearArmor);

                    return true;
                }
                //胸甲
                else if (instance.data.Breastplate != null)
                {
                    player.ServerSwapItem(inventoryIndex, Inventory.breastplateVar);
                    GAudio.Play(AudioID.WearArmor);

                    return true;
                }
                //护腿
                else if (instance.data.Legging != null)
                {
                    player.ServerSwapItem(inventoryIndex, Inventory.leggingVar);
                    GAudio.Play(AudioID.WearArmor);

                    return true;
                }
                //靴子
                else if (instance.data.Boots != null)
                {
                    player.ServerSwapItem(inventoryIndex, Inventory.bootsVar);
                    GAudio.Play(AudioID.WearArmor);

                    return true;
                }
            }

            return false;
        }

        public virtual void OnHand()
        {

        }

        public virtual void AsHelmet()
        {

        }

        public virtual void AsBreastplate()
        {

        }

        public virtual void AsLegging()
        {

        }

        public virtual void AsBoots()
        {

        }

        public virtual void AsShield()
        {

        }

        public virtual void Rendering(SpriteRenderer sr)
        {
            sr.sprite = instance.data.texture.sprite;

            owner.ModifyUsingItemRendererTransform(instance.data.offset, instance.data.size, instance.data.rotation);
        }

        public ItemBehaviour(IInventoryOwner owner, Item instance, string inventoryIndex)
        {
            this.owner = owner;
            this.instance = instance;
            this.inventoryIndex = inventoryIndex;
        }
    }
}
