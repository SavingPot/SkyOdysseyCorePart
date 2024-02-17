using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameCore
{
    public interface ISpellContainer
    {
        public Spell spell { get; set; }
        public SpellBehaviour spellBehaviour { get; set; }

        public static void LoadFromJObject(ISpellContainer container, JObject jo)
        {
            var jt = jo["ori:spell_container"];
            var spellString = jt["spell"].ToString();

            if (spellString == null)
                container.spell = null;
            else
            {
                container.spell = ModFactory.CompareSpell(spellString);
                container.spellBehaviour = (SpellBehaviour)Activator.CreateInstance(container.spell.behaviourType, container as IManaContainer, container, container.spell);
            }
        }
    }
}