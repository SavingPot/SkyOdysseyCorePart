using UnityEngine;

namespace GameCore
{
    public interface IInteractableEntity
    {
        Vector2 interactionSize { get; }
        void PlayerInteraction(Player player);
    }
}