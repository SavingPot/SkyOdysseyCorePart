using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// NPC
    /// </summary>
    [NotSummonable]
    public abstract class NPC : Creature, IInteractableEntity
    {
        public static int npcLayer { get; internal set; }
        public static int npcLayerMask { get; internal set; }


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

        public virtual bool PlayerInteraction(Player caller)
        {
            return false;
        }

        public override JObject ModifyCustomData(JObject data)
        {
            data = base.ModifyCustomData(data) ?? new();

            //添加 "ori:npc
            if (!data.TryGetJToken("ori:npc", out var npcJT))
            {
                data.AddProperty("ori:npc", new JObject());
                npcJT = data["ori:npc"];
            }

            return data;
        }

        public override void LoadFromCustomData()
        {
            base.LoadFromCustomData();

            LoadFromNpcJT(customData["ori:npc"]);
        }

        protected virtual void LoadFromNpcJT(JToken jt)
        {

        }

        public override Vector2 GetMovementDirection() => Vector2.zero;









        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            npcLayer = LayerMask.NameToLayer("NPC");
            npcLayerMask = npcLayer.LayerMaskOnly();
        }
    }
}
