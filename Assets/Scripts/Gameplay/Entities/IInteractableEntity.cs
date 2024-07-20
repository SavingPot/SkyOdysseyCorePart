using UnityEngine;

namespace GameCore
{
    public interface IInteractableEntity
    {
        /// <summary>
        /// 交互区域的大小
        /// </summary>
        Vector2 interactionSize { get; }

        /// <summary>
        /// 返回是否成功与玩家交互
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        bool PlayerInteraction(Player player);
    }
}