using UnityEngine;

namespace GameCore
{
    public static class CreatureHumanBodyPartsBehaviour
    {
        public static void RefreshArmors<T>(T entity, ItemData helmet, ItemData breastplate, ItemData legging, ItemData boots)
            where T : Entity, IHumanBodyParts<CreatureBodyPart>
        {
            /* --------------------------------- 刷新头盔的贴图 -------------------------------- */
            entity.head.armorSr.sprite = helmet?.Helmet?.head?.sprite ?? null;

            /* --------------------------------- 刷新胸甲的贴图 -------------------------------- */
            entity.body.armorSr.sprite = breastplate?.Breastplate?.body?.sprite ?? null;
            entity.leftArm.armorSr.sprite = breastplate?.Breastplate?.leftArm?.sprite ?? null;
            entity.rightArm.armorSr.sprite = breastplate?.Breastplate?.rightArm?.sprite ?? null;

            /* --------------------------------- 刷新护腿的贴图 -------------------------------- */
            entity.leftLeg.armorSr.sprite = legging?.Legging?.leftLeg?.sprite ?? null;
            entity.rightLeg.armorSr.sprite = legging?.Legging?.rightLeg?.sprite ?? null;

            /* --------------------------------- 刷新鞋子的贴图 -------------------------------- */
            entity.leftFoot.armorSr.sprite = boots?.Boots?.leftFoot?.sprite ?? null;
            entity.rightFoot.armorSr.sprite = boots?.Boots?.rightFoot?.sprite ?? null;
        }
    }

    public interface IHumanBodyParts<T> where T : CreatureBodyPart
    {
        [HideInInspector] T head { get; set; }
        [HideInInspector] T rightArm { get; set; }
        [HideInInspector] T body { get; set; }
        [HideInInspector] T leftArm { get; set; }
        [HideInInspector] T rightLeg { get; set; }
        [HideInInspector] T leftLeg { get; set; }
        [HideInInspector] T leftFoot { get; set; }
        [HideInInspector] T rightFoot { get; set; }
    }
}