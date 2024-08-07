using System;
using System.Collections.Generic;
using GameCore.High;
using GameCore.Network;
using GameCore.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameCore
{
    public static class PlayerCenter
    {
        public static List<Player> all = new();
        public static Action<Player> OnAddPlayer = _ => { };
        public static Action<Player> OnRemovePlayer = _ => { };
        public static float playerHealthUpTimer;

        public static void AddPlayer(Player player)
        {
            all.Add(player);
            OnAddPlayer(player);
        }

        public static void RemovePlayer(Player player)
        {
            all.Remove(player);
            OnRemovePlayer(player);
        }

        public static void Update()
        {
            if (Server.isServer)
            {
                var frameTime = Performance.frameTime;

                foreach (var player in all)
                {
                    if (player.isDead)
                        continue;

                    bool isMoving = player.isMoving;
                    int health = player.health;

                    //一秒回一次血
                    if (Tools.time >= playerHealthUpTimer)
                    {
                        //受伤的八秒内不回血
                        if (health < player.maxHealth && Tools.time > player.previousHurtTime + 8)
                        {
                            playerHealthUpTimer = Tools.time + 1f;
                            player.health = health + 1;
                        }
                    }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            /* -------------------------------------------------------------------------- */
            /*                                    初始化任务                                   */
            /* -------------------------------------------------------------------------- */
            PlayerUI.tasks = new()
            {
                new("ori:get_log", "ori:task.get_log", null, 1, null),
                new("ori:get_feather_wing", "ori:task.get_feather_wing", "ori:get_log", 0, null),
                new("ori:get_straw_rope", "ori:task.get_straw_rope", "ori:get_log", 0, new[] { $"{ItemID.StrawRope}/=/3/=/null" }),
                new("ori:get_planks", "ori:task.get_planks", "ori:get_log", 0, new[] { $"{BlockID.OakPlanks}/=/10/=/null" }),
                new("ori:get_flint_knife", "ori:task.get_flint_knife", "ori:get_planks", 0, null),
                new("ori:get_flint_hoe", "ori:task.get_flint_hoe", "ori:get_planks", 0, null),
                new("ori:get_flint_sword", "ori:task.get_flint_sword", "ori:get_planks", 0, null),
                new("ori:get_bark", "ori:task.get_bark", "ori:get_log", 0, new[] { $"{ItemID.Bark}/=/1/=/null" }),
            };
            {
                JObject jo = new();
                SkillManuscriptBehaviour.WriteSkillId(ref jo, SkillID.Industry);
                PlayerUI.tasks.Add(new("ori:get_ore", "ori:task.get_ore", "ori:get_log", 0, new[] { $"{ItemID.SkillManuscript}/=/1/=/{jo.ToString(Newtonsoft.Json.Formatting.None)}" }));
            }
            {
                JObject jo = new();
                SkillManuscriptBehaviour.WriteSkillId(ref jo, SkillID.Agriculture_Cooking);
                PlayerUI.tasks.Add(new("ori:get_meat", "ori:task.get_meat", "ori:get_log", 0, new[] { $"{ItemID.SkillManuscript}/=/1/=/{jo.ToString(Newtonsoft.Json.Formatting.None)}" }));
            }
            {
                JObject jo = new();
                SkillManuscriptBehaviour.WriteSkillId(ref jo, SkillID.Economy_Building);
                PlayerUI.tasks.Add(new("ori:get_campfire", "ori:task.get_campfire", "ori:get_planks", 0, new[] { $"{ItemID.SkillManuscript}/=/1/=/{jo.ToString(Newtonsoft.Json.Formatting.None)}" }));
            }





            /* -------------------------------------------------------------------------- */
            /*                                    初始化技能                                   */
            /* -------------------------------------------------------------------------- */
            PlayerUI.skills = new()
            {
                new(SkillID.Root, "ori:skill.root", null, "ori:skill_description.root", 99999),
                new(SkillID.Agriculture, "ori:skill.agriculture", "ori:root", "ori:skill_description.agriculture", 1),
                new(SkillID.Agriculture_Cooking, "ori:skill.agriculture.cooking", "ori:agriculture", "ori:skill_description.agriculture.cooking", 1),
                new(SkillID.Agriculture_Quick, "ori:skill.agriculture.quick", "ori:agriculture", "ori:skill_description.agriculture.quick", 2),
                new(SkillID.Agriculture_Coin, "ori:skill.agriculture.coin", "ori:agriculture", "ori:skill_description.agriculture.coin", 2),
                new(SkillID.Agriculture_Harvest, "ori:skill.agriculture.harvest", "ori:agriculture", "ori:skill_description.agriculture.harvest", 2),
                new(SkillID.Agriculture_Fishing, "ori:skill.agriculture.fishing", "ori:agriculture", "ori:skill_description.agriculture.fishing", 2),
                new(SkillID.Magic, "ori:skill.magic", "ori:root", "ori:skill_description.magic", 1),
                new(SkillID.Economy, "ori:skill.economy", "ori:root", "ori:skill_description.economy", 1),
                new(SkillID.Economy_Building, "ori:skill.economy.building", "ori:economy", "ori:skill_description.economy.building", 1),
                new(SkillID.Industry, "ori:skill.industry", "ori:root", "ori:skill_description.industry", 1),
            };
        }
    }
}