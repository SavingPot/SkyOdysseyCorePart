using UnityEngine;

namespace GameCore
{
    public class SpellBehaviour
    {
        public ISpellContainer spellContainer;
        public Spell instance;

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }

        //TODO: VAR PLAYER -> IInventoryOwner
        public virtual void Release(Vector2 releaseDirection, Vector2 releasePosition, Player player)
        {

        }

        public SpellBehaviour(ISpellContainer spellContainer, Spell instance)
        {
            this.spellContainer = spellContainer;
            this.instance = instance;
        }
    }
}
