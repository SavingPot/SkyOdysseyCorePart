using System;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 角色, 基于实体进行拓展, 不能直接生成
    /// </summary>
    public class NPC : Creature
    {
        //TODO: 所有实体都可用 PlayerInteraction
        public virtual Vector2 interactionSize { get; } = new(5, 5f);



        protected override void Awake()
        {
            base.Awake();

            isHurtable = false;
        }

        public override void AfterInitialization()
        {
            base.AfterInitialization();

            NPCCenter.AddNPC(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            NPCCenter.RemoveNPC(this);
        }

        public virtual void PlayerInteraction(Player caller)
        {

        }

        public override Vector2 GetMovementDirection() => Vector2.zero;
    }
}
