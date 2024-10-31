using GameCore.UI;
using Newtonsoft.Json.Linq;
using SP.Tools;
using UnityEngine;

namespace GameCore
{
    [ItemBinding(ItemID.SkillManuscript)]
    public class SkillManuscriptBehaviour : ItemBehaviour
    {
        public override bool Use(Vector2 point)
        {
            if (owner is not Player player)
            {
                Debug.LogError("只有玩家可以使用技能稿纸");
                return false;
            }

            var skill = GetSkillId(instance.customData);
            if (skill.IsNullOrWhiteSpace())
            {
                Debug.LogError($"技能稿纸中没有技能");
                return false;
            }

            var node = player.pui.Backpack.skillNodeTree.FindTreeNode(skill);
            if (node == null)
            {
                Debug.LogError($"技能稿纸所对的技能 {skill} 不存在");
                return false;
            }

            if (node.status.unlocked)
            {
                player.ServerAddSkillPoint(node.data.cost);
                player.ServerReduceUsingItemCount(1);
                return true;
            }

            if (!node.IsParentLineUnlocked())
            {
                var parent = player.pui.Backpack.skillNodeTree.GetNodeButtonId(node.parent.data.id);
                InternalUIAdder.instance.SetStatusText($"请先解锁 {GameUI.CompareText($"{parent}.text")}");
                return true;
            }

            player.pui.Backpack.UnlockSkill(node);
            player.ServerReduceUsingItemCount(1);
            InternalUIAdder.instance.SetTitleText($"解锁技能: {GameUI.CompareText($"{player.pui.Backpack.skillNodeTree.GetNodeButtonId(node.data.id)}.text")}");
            return true;
        }


        public static void FixJObject(ref JObject jo)
        {
            jo ??= new();

            if (!jo.TryGetJToken("ori:skill_manuscript", out var manuscript))
            {
                manuscript = new JObject();
                jo.Add(new JProperty("ori:skill_manuscript", manuscript));
            }

            if (!manuscript.TryGetJToken("skill", out var skill))
            {
                skill = new JValue(string.Empty);
                ((JObject)manuscript).Add(new JProperty("skill", skill));
            }
        }

        public static string GetSkillId(JObject jo)
        {
            return jo?["ori:skill_manuscript"]?["skill"]?.ToString();
        }

        public static void WriteSkillId(ref JObject jo, string skillId)
        {
            FixJObject(ref jo);
            jo["ori:skill_manuscript"]["skill"] = skillId;
        }

        public SkillManuscriptBehaviour(IInventoryOwner owner, Item instance, string inventoryIndex) : base(owner, instance, inventoryIndex) { }
    }
}
