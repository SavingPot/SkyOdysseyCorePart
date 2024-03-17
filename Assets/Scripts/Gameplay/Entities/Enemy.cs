using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using SP.Tools.Unity;

namespace GameCore
{
    public abstract class Enemy : Creature, IHumanBodyParts<CreatureBodyPart>
    {
        public Transform targetTransform;
        public float searchTime = float.NegativeInfinity;
        public float attackTimer;
        public string[] attackAnimations = new[] { "attack_leftarm", "attack_rightarm" }; //TODO: 包含 动画的layer 信息







        public Func<Enemy, Transform> FindTarget = (enemy) =>
        {
            //搜索 CD 3s
            if (Tools.time < enemy.searchTime + 3)
                return null;

            enemy.searchTime = Tools.time;

            foreach (var player in PlayerCenter.all)
            {
                if ((player.transform.position - enemy.transform.position).sqrMagnitude <= enemy.data.searchRadiusSqr && !player.isDead)
                {
                    return player.transform;
                }
            }

            return null;
        };




        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            //检查目标
            if (targetTransform)
            {
                #region 普通攻击

                if (!isDead)
                {
                    //为了性能使用 x - x, y - y 而不是 Vector2.Distance()
                    float disX = Mathf.Abs(transform.position.x - targetTransform.position.x);
                    float disY = Mathf.Abs(transform.position.y - targetTransform.position.y);

                    //在攻击范围内, 并且 CD 已过
                    if (disX <= data.normalAttackRadius && disY <= data.normalAttackRadius && Tools.time >= attackTimer)
                    {
                        attackTimer = Tools.time + data.normalAttackCD;

                        //设置动画
                        if (animWeb != null)
                        {
                            foreach (var animId in attackAnimations)
                            {
                                animWeb.SwitchPlayingTo(animId, 0);
                            }
                        }

                        if (UObjectTools.GetComponent(targetTransform, out Entity entity))
                        {
                            AttackEntity(entity);
                        }
                    }
                }

                #endregion

                CheckEnemyTargetDistance();
            }
            else
                ReFindTarget();
        }

        public void AttackEntity(Entity entity)
        {
            entity.TakeDamage(data.normalAttackDamage, 0.3f, transform.position, transform.position.x < targetTransform.position.x ? Vector2.right * 12 : Vector2.left * 12);
        }

        public void ReFindTarget()
        {
            if (!isServer)
            {
                Debug.LogWarning("不应该在客户端寻找目标!");
                return;
            }

            var tempTransform = FindTarget(this);

            if (tempTransform)
                targetTransform = tempTransform;
        }

        public void CheckEnemyTargetDistance()
        {
            if ((targetTransform.position - transform.position).sqrMagnitude > data.searchRadiusSqr)
                targetTransform = null;
        }
    }
}
