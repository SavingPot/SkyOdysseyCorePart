using System;
using UnityEngine;

namespace GameCore
{
    public static class EntityIsOnGroundBehaviour
    {
        public static readonly Collider2D[] OnGroundHits = new Collider2D[10];

        public static void OnFixedUpdate<T>(T ground) where T : Entity, IEntityIsOnGround
        {
            bool value = false;

            //还原数组
            Array.Clear(OnGroundHits, 0, OnGroundHits.Length);

            //获取数据
            var position = ground.transform.position;
            var colliderOffset = ground.mainCollider.offset;
            var colliderSize = ground.mainCollider.size;

            //射线检测
            Physics2D.OverlapAreaNonAlloc(
                    new(
                        position.x + colliderOffset.x - colliderSize.x * 0.5f,
                        position.y + colliderOffset.y - colliderSize.y * 0.5f
                    ),
                    new(
                        position.x + colliderOffset.x + colliderSize.x * 0.5f,
                        position.y + colliderOffset.y - colliderSize.y * 0.5f - 0.2f
                    ),
                    OnGroundHits,
                    Block.blockLayerMask);

            //检测获取到的物体中有没有非触发器方块
            foreach (var item in OnGroundHits)
            {
                if (item == null)
                    break;

                if (!item.isTrigger && Block.TryGetBlockFromCollider(item, out _))
                {
                    value = true;
                    break;
                }
            }

            //设置值
            ground.isOnGround = value;
        }
    }

    public interface IEntityIsOnGround
    {
        bool isOnGround { get; set; }
    }
}