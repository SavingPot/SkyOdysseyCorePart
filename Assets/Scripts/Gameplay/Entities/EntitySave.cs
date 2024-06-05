using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using static GameCore.UI.PlayerUI;

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
    //该类只在 ManagerNetwork 的 AddPlayer 环节中创建
    public sealed class PlayerSave : EntitySave
    {
        public float hungerValue;
        public float happinessValue;
        public int coin;
        public Inventory inventory;// = new();
        public List<TaskStatusForSave> completedTasks = new();
        public List<SkillStatusForSave> unlockedSkills = new();

        [NonSerialized] public byte[] skinHead;
        [NonSerialized] public byte[] skinBody;
        [NonSerialized] public byte[] skinLeftArm;
        [NonSerialized] public byte[] skinRightArm;
        [NonSerialized] public byte[] skinLeftLeg;
        [NonSerialized] public byte[] skinRightLeg;
        [NonSerialized] public byte[] skinLeftFoot;
        [NonSerialized] public byte[] skinRightFoot;

        public void WriteFromPlayer(Player player)
        {
            inventory = player.inventory;
            hungerValue = player.hungerValue;
            coin = player.coin;
            happinessValue = player.happinessValue;
            completedTasks = player.completedTasks;
            unlockedSkills = player.unlockedSkills;
        }

        public static PlayerSave NewPlayer(string playerName)
        {
            return new PlayerSave()
            {
                id = playerName,
                coin = 30,
                inventory = new(Player.inventorySlotCountConst, null),
                hungerValue = Player.defaultHungerValue,
                happinessValue = Player.defaultHappinessValue,
                completedTasks = new(),
                unlockedSkills = new()
            }; ;
        }
    }
}