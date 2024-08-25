using System;
using System.Collections;
using System.Collections.Generic;
using GameCore.Network;
using UnityEngine;

namespace GameCore
{
    [NotSummonable]
    public class Bullet : Entity
    {
        public bool destroyOnHitEntity = true;
        public bool destroyOnCollideWithBlock = true;
        public int damage;
        [Sync] public uint? ownerId;
        [HideInInspector] public Collider2D[] besideObjectsDetected = new Collider2D[15];

        protected override void Awake()
        {
            base.Awake();

            isHurtable = false;
        }

        protected virtual void HitEntity(Entity entity)
        {
            entity.TakeDamage(damage, 0.2f, transform.position, Vector2.zero);
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            if (isDead)
                return;

            Array.Clear(besideObjectsDetected, 0, besideObjectsDetected.Length);
            Physics2D.OverlapCircleNonAlloc(transform.position, 0.3f, besideObjectsDetected);
            foreach (var obj in besideObjectsDetected)
            {
                if (!obj)
                    break;

                //排除自己
                if (obj.gameObject == gameObject)
                    continue;

                //实体
                if (obj.gameObject.TryGetComponent<Entity>(out var entity) && entity.netId != ownerId && entity.CanTakeDamage())
                {
                    HitEntity(entity);

                    if (destroyOnHitEntity)
                        Death();
                }
                //方块
                else if (Block.TryGetBlockFromCollider(obj, out Block block) && !block.blockCollider.isTrigger)
                {
                    BlockCollision(block);
                }
            }
        }

        public void LookAtDirection()
        {
            float angle = AngleTools.IncludedDegreeBetweenX(rb.velocity);
            transform.eulerAngles = new Vector3(0, 0, angle);
        }

        public virtual void BlockCollision(Block block)
        {
            if (destroyOnCollideWithBlock)
                Death();
        }
    }
}
