using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [NotSummonable]
    public class Bullet : Entity
    {
        public bool destroyOnHit = true;
        public float damage;
        public uint ownerId;
        [HideInInspector] public Collider2D[] besideObjectsDetected = new Collider2D[15];

        protected override void Start()
        {
            base.Start();

            hurtable = false;

            try
            {
                ownerId = customData["ori:bullet"]["ownerId"].ToObject<uint>();
            }
            catch
            {
                Death();
            }
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            if (isDead)
                return;

            Physics2D.OverlapCircleNonAlloc(transform.position, 0.3f, besideObjectsDetected);
            foreach (var obj in besideObjectsDetected)
            {
                if (!obj)
                {
                    break;
                }

                if (obj.gameObject.TryGetComponent<Creature>(out var hit) && hit.netId != ownerId)
                {
                    hit.TakeDamage(damage, 0.2f, transform.position, Vector2.zero);

                    if (destroyOnHit)
                        Death();
                }
                else if (Block.TryGetBlockFromCollision(obj, out Block block) && !block.blockCollider.isTrigger)
                {
                    BlockCollision(block);
                }
            }
        }

        public void LookAtDirection()
        {
            float angle = Tools.IncludedAngleBetweenX(rb.velocity);
            transform.eulerAngles = new Vector3(0, 0, angle);
        }

        public virtual void BlockCollision(Block block)
        {
            Death();
        }
    }
}
