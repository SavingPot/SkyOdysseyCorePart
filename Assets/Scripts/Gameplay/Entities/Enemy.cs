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
        Entity targetEntity_temp; void targetEntity_set(Entity value) { }
        [Sync] public Entity targetEntity { get => targetEntity_temp; set => targetEntity_set(value); }
        public float searchTime = float.NegativeInfinity;
        public float attackTimer;
        public string[] attackAnimations = new[] { "attack_leftarm", "attack_rightarm" }; //TODO: 包含 动画的layer 信息







        public override void AfterInitialization()
        {
            base.AfterInitialization();

            EnemyCenter.AddEnemy(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            EnemyCenter.RemoveEnemy(this);
        }


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

            targetEntity = null;

            //搜索 CD 3s
            if (Tools.time < searchTime + 3)
                return;

            searchTime = Tools.time;

            foreach (var player in PlayerCenter.all)
            {
                if ((player.transform.position - transform.position).sqrMagnitude <= data.searchRadiusSqr && !player.isDead)
                {
                    targetEntity = player;
                }
            }
        }

        public void CheckTargetStatus()
        {
            if (targetEntity.isDead || (targetEntity.transform.position - transform.position).sqrMagnitude > data.searchRadiusSqr)
                targetEntity = null;
        }
    }

    public static class EnemyCenter
    {
        public static List<Enemy> all = new();
        public static Action<Enemy> OnAddEnemy = _ => { };
        public static Action<Enemy> OnRemoveEnemy = _ => { };

        public static void AddEnemy(Enemy enemy)
        {
            all.Add(enemy);
            OnAddEnemy(enemy);
        }

        public static void RemoveEnemy(Enemy enemy)
        {
            all.Remove(enemy);
            OnRemoveEnemy(enemy);
        }
    }
}
