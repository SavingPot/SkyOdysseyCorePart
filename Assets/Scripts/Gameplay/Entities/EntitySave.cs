using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using UnityEngine;
using static GameCore.BackpackPanelUI;
using static GameCore.UI.PlayerUI;
using Vector2 = UnityEngine.Vector2;

namespace GameCore
{
    [Serializable]
    public class EntitySave
    {
        public string id;
        public string customData;
        public Vector2 pos;
        public int? health;
        public string saveId;

        public void WriteFromEntity(Entity entity)
        {
            pos = entity.transform.position;
            health = entity.health;
            customData = entity.customData?.ToString(Formatting.None);
        }

        public EntitySave()
        {

        }

        public static EntitySave Create(string entityId)
        {
            var result = new EntitySave
            {
                id = entityId
            };

            return result;
        }
    }

    [Serializable, EntityBinding(EntityID.Player)]
    public sealed class PlayerSave : EntitySave
    {
        public int coin;
        public float mana;
        public Inventory inventory;// = new();
        public List<TaskStatusForSave> completedTasks = new();
        public List<SkillStatusForSave> unlockedSkills = new();
        public float skillPoints;
        public Vector2 respawnPoint;

        [NonSerialized] public byte[] skinHead;
        [NonSerialized] public byte[] skinBody;
        [NonSerialized] public byte[] skinLeftArm;
        [NonSerialized] public byte[] skinRightArm;
        [NonSerialized] public byte[] skinLeftLeg;
        [NonSerialized] public byte[] skinRightLeg;
        [NonSerialized] public byte[] skinLeftFoot;
        [NonSerialized] public byte[] skinRightFoot;
        public static int defaultCoin = 100;

        public void WriteFromPlayer(Player player)
        {
            coin = player.coin;
            mana = player.mana;
            inventory = player.inventory;
            completedTasks = player.completedTasks;
            unlockedSkills = player.unlockedSkills;
            skillPoints = player.skillPoints;
        }

        public static PlayerSave NewPlayer(string playerName)
        {
            PlayerSave result = new()
            {
                id = playerName,
                coin = defaultCoin,
                mana = Player.defaultMana,
                inventory = new(Player.inventorySlotCountConst, null),
                completedTasks = new(),
                unlockedSkills = new(),
                skillPoints = 0,
            };

            //初始物品
            result.inventory.AddItem(ModFactory.CompareItem(BlockID.PotatoCrop).DataToItem().SetCount(5));
            result.inventory.AddItem(ModFactory.CompareItem(ItemID.Wrench).DataToItem());

            return result;
        }
    }
}