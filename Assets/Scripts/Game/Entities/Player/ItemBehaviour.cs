using GameCore.UI;

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

        public virtual bool Use()
        {
            if (owner is Player)
            {
                Player player = (Player)owner;

                //放置方块
                if (instance.data.isBlock && player.InUseRadius() && !player.map.HasBlock(PosConvert.WorldToMapPos(player.cursorWorldPos), player.controllingLayer))
                {
                    player.map.SetBlockNet(PosConvert.WorldToMapPos(player.cursorWorldPos), player.controllingLayer, instance.data.id, instance.customData?.ToString());

                    if (GControls.mode == ControlMode.Gamepad)
                        GControls.GamepadVibrationSlightMedium();

                    player.ServerReduceItemCount(inventoryIndex, 1);

                    GAudio.Play(AudioID.PlaceBlock);

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
