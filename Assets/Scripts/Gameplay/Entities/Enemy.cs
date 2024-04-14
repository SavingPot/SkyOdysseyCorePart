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
        public Entity targetEntity;
        public float searchTime = float.NegativeInfinity;
        public float attackTimer;
        public string[] attackAnimations = new[] { "attack_leftarm", "attack_rightarm" }; //TODO: 包含 动画的layer 信息









        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            //检查目标
            if (targetEntity)
            {
                #region 普通攻击

                if (!isDead)
                {
                    //为了性能使用 (x - x), (y - y) 而不是 Vector2.Distance()
                    float disX = Mathf.Abs(transform.position.x - targetEntity.transform.position.x);
                    float disY = Mathf.Abs(transform.position.y - targetEntity.transform.position.y);

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

                        AttackEntity(targetEntity);
                    }
                }

                #endregion

                CheckTargetStatus();
            }
            else
                FindTarget();
        }

        public void AttackEntity(Entity entity)
        {
            entity.TakeDamage(
                data.normalAttackDamage,
                0.3f,
                transform.position,
                transform.position.x < targetEntity.transform.position.x ? Vector2.right * 12 : Vector2.left * 12
            );
        }

        public void FindTarget()
        {
            if (!isServer)
            {
                Debug.LogWarning("不应该在客户端寻找目标!");
                return;
            }

            //搜索 CD 3s
            if (Tools.time < searchTime + 3)
                targetEntity = null;

            searchTime = Tools.time;

            foreach (var player in PlayerCenter.all)
            {
                if ((player.transform.position - transform.position).sqrMagnitude <= data.searchRadiusSqr && !player.isDead)
                {
                    targetEntity = player;
                }
            }

            targetEntity = null;
        }

        public void CheckTargetStatus()
        {
            if (targetEntity.isDead || (targetEntity.transform.position - transform.position).sqrMagnitude > data.searchRadiusSqr)
                targetEntity = null;
        }
    }
}
