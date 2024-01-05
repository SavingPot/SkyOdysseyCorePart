using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using SP.Tools.Unity;

namespace GameCore
{
    public class Enemy : Creature, IHumanBodyParts<CreatureBodyPart>
    {
        public Transform targetTransform;
        public float searchTime = float.NegativeInfinity;
        public float attackTimer;
        public string[] attackAnimations = new[] { "attack_leftarm", "attack_rightarm" }; //TODO: ���� ������layer ��Ϣ







        public Func<Enemy, Transform> FindTarget = (enemy) =>
        {
            //���� CD 3s
            if (Tools.time < enemy.searchTime + 3)
                return null;

            enemy.searchTime = Tools.time;

            foreach (var player in PlayerCenter.all)
            {
                if ((player.transform.position - enemy.transform.position).sqrMagnitude <= enemy.data.searchRadiusSqr)
                {
                    return player.transform;
                }
            }

            return null;
        };




        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            //���Ŀ��
            if (targetTransform)
            {
                #region ��ͨ����

                if (!isDead)
                {
                    //Ϊ������ʹ�� x - x, y - y ������ Vector2.Distance()
                    float disX = Mathf.Abs(transform.position.x - targetTransform.position.x);
                    float disY = Mathf.Abs(transform.position.y - targetTransform.position.y);

                    //�ڹ�����Χ��, ���� CD �ѹ�
                    if (disX <= data.normalAttackRadius && disY <= data.normalAttackRadius && Tools.time >= attackTimer)
                    {
                        attackTimer = Tools.time + data.normalAttackCD;

                        //���ö���
                        if (animWeb != null)
                        {
                            foreach (var animId in attackAnimations)
                            {
                                animWeb.SwitchPlayingTo(animId, 0);
                            }
                        }

                        if (UObjectTools.GetComponent(targetTransform, out Entity entity))
                        {
                            entity.TakeDamage(data.normalAttackDamage, 0.3f, transform.position, transform.position.x < targetTransform.position.x ? Vector2.right * 12 : Vector2.left * 12);
                        }
                    }
                }

                #endregion

                CheckEnemyTargetDistance();
            }
            else
                ReFindTarget();
        }

        public void ReFindTarget()
        {
            if (!isServer)
            {
                Debug.LogWarning("��Ӧ���ڿͻ���Ѱ��Ŀ��!");
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
