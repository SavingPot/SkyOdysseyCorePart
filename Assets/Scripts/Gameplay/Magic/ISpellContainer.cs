using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameCore
{
    public interface ISpellContainer
    {
        public Spell spell { get; set; }
        public SpellBehaviour spellBehaviour { get; set; }

        public static void FixJObject(JObject jo)
        {
            if (!jo.TryGetJToken("ori:spell_container", out var containerJT))
            {
                containerJT = new JObject();
                jo.Add(new JProperty("ori:spell_container", containerJT));
            }

            if (!containerJT.TryGetJToken("spell", out var spellJT))
            {
                spellJT = new JValue(string.Empty);
                ((JObject)containerJT).Add(new JProperty("spell", spellJT));
            }
        }

        public static void LoadFromJObject(ISpellContainer container, JObject jo)
        {
            var jt = jo["ori:spell_container"];
            var spellString = jt["spell"].ToString();

            if (spellString == null)
                container.spell = null;
            else
            {
                container.spell = ModFactory.CompareSpell(spellString);
                container.spellBehaviour = (SpellBehaviour)Activator.CreateInstance(container.spell.behaviourType, container, container.spell);
            }
        }

        public static void SetSpell(JObject customData, string spell)
        {
            FixJObject(customData);
            customData["ori:spell_container"]["spell"] = spell;
        }
    }
}