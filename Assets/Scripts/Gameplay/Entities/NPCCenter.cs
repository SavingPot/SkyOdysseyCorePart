using System;
using System.Collections.Generic;

namespace GameCore
{
    public static class NPCCenter
    {
        public static readonly List<NPC> all = new();
        public static Action<NPC> OnAddNPC = _ => { };
        public static Action<NPC> OnRemoveNPC = _ => { };

        public static void AddNPC(NPC npc)
        {
            all.Add(npc);
            OnAddNPC(npc);
        }

        public static void RemoveNPC(NPC npc)
        {
            all.Remove(npc);
            OnRemoveNPC(npc);
        }
    }
}
