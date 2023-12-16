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
            }
        }

        public virtual bool Use()
        {
            if (owner is Player player)
            {
                //放置方块
                if (instance.data.isBlock && player.InUseRadius() && !player.map.HasBlock(PosConvert.WorldToMapPos(player.cursorWorldPos), player.isControllingBackground))
                {
                    UseAsBlock(PosConvert.WorldToMapPos(player.cursorWorldPos), player.isControllingBackground);

                    return true;
                }
                //多水食物
                else if (instance.data.Edible().hasTag && instance.data.Drinkable().hasTag)
                {
                    player.hungerValue += instance.data.Edible().tagValue;
                    player.thirstValue += instance.data.Drinkable().tagValue;

                    player.ServerReduceItemCount(inventoryIndex, 1);

                    GAudio.Play(AudioID.Eat);

                    return true;
                }
                //进食
                else if (instance.data.Edible().hasTag)
                {
                    player.hungerValue += instance.data.Edible().tagValue;

                    player.ServerReduceItemCount(inventoryIndex, 1);

                    GAudio.Play(AudioID.Eat);

                    return true;
                }
                //饮水
                else if (instance.data.Drinkable().hasTag)
                {
                    player.thirstValue += instance.data.Drinkable().tagValue;

                    player.ServerReduceItemCount(inventoryIndex, 1);

                    GAudio.Play(AudioID.Drink);

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
                //护腿
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

        public virtual void ModifyInfo(ItemInfoUI ui)
        {

        }

        public virtual void Render()
        {

        }

        public ItemBehaviour(IInventoryOwner owner, Item instance, string inventoryIndex)
        {
            this.owner = owner;
            this.instance = instance;
            this.inventoryIndex = inventoryIndex;
        }
    }
}
