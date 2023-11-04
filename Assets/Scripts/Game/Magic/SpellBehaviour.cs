using UnityEngine;

namespace GameCore
{
    public class SpellBehaviour
    {
        public IManaContainer manaContainer;
        public ISpellContainer spellContainer;
        public Spell instance;

        //TODO: NO THIS, ALSO IN ITEM-BEHAVIOUR
        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }

        //TODO: NO PLAYER VAR
        public virtual void Release(Vector2 releaseDirection, Vector2 releasePosition, Player player)
        {

        }

        public SpellBehaviour(IManaContainer manaContainer, ISpellContainer spellContainer, Spell instance)
        {
            this.manaContainer = manaContainer;
            this.spellContainer = spellContainer;
            this.instance = instance;
        }
    }
}
