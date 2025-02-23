using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using SP.Tools.Unity;
using GameCore.Network;
using GameCore.UI;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;

namespace GameCore
{
    public abstract class Enemy : Creature, IHumanBodyParts<CreatureBodyPart>
    {
        public static int enemyLayer { get; internal set; }
        public static int enemyLayerMask { get; internal set; }


        ImageIdentity healthBarBackground;
        ImageIdentity healthBarFull;

        [Sync] public Entity targetEntity;
        public float searchTime = float.NegativeInfinity;
        public string[] attackAnimations = new[] { "attack_leftarm", "attack_rightarm" }; //TODO: 包含 动画的layer 信息
        ImageIdentity statusMarkImage;
        NormalAttackTemp currentNormalAttack;
        class NormalAttackTemp
        {
            public float startTime;
            public float warningEndTime;
            public float dodgeEndTime;
            public float hitJudgementEndTime;
            public float recoveryEndTime;
            public bool hasSucceed;

            public NormalAttackTemp(EntityData.NormalAttackData data)
            {
                startTime = Tools.time;
                warningEndTime = startTime + data.warningTime;
                dodgeEndTime = warningEndTime + data.dodgeTime;
                hitJudgementEndTime = dodgeEndTime + data.hitJudgementTime;
                recoveryEndTime = hitJudgementEndTime + data.recoveryTime;
            }
        }





        public override void Initialize()
        {
            base.Initialize();

            var oldVelocityFactor = velocityFactor;
            velocityFactor = () =>
            {
                //后摇期间不能移动
                var result = oldVelocityFactor();
                if (currentNormalAttack != null && Tools.time > currentNormalAttack.dodgeEndTime)
                {
                    result = 0;
                }
                return result;
            };
        }

        public override void AfterInitialization()
        {
            base.AfterInitialization();

            //初始化画布
            GetOrAddEntityCanvas();

            //血条
            healthBarBackground = GameUI.AddImage(UIA.Middle, $"ori:image.health_bar_background_{netId}", "ori:enemy_health_bar", usingCanvas.transform);
            healthBarBackground.SetSizeDelta(15, 1.5f);
            healthBarBackground.image.color = Color.gray;
            healthBarBackground.gameObject.SetActive(false);
            healthBarBackground.AddAPosY(10);
            healthBarFull = GameUI.AddImage(UIA.Middle, $"ori:image.health_bar_background_{netId}", "ori:enemy_health_bar", healthBarBackground);
            healthBarFull.image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            healthBarFull.image.type = UnityEngine.UI.Image.Type.Filled;
            healthBarFull.image.color = Color.red;
            healthBarFull.SetSizeDelta(healthBarBackground.sd);
            OnHealthChange += _ => RefreshHealthBar();
            RefreshHealthBar();

            //感叹号
            statusMarkImage = GameUI.AddImage(UIA.Middle, $"ori:image.status_mark_{netId}", "ori:enemy_exclamation_mark", usingCanvas.transform);
            statusMarkImage.AddAPosY(7);
            statusMarkImage.SetSizeDelta(13, 13);
            statusMarkImage.image.enabled = false;

            EnemyCenter.AddEnemy(this);
        }

        protected override void Update()
        {
            base.Update();

            RefreshHealthBar();
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
                //发动普通攻击
                if (!isDead && !isHurting && data.normalAttack != null)
                    NormalAttackLoop();
                else
                    statusMarkImage.image.enabled = false;

                //显示血条
                healthBarBackground.gameObject.SetActive(true);

                CheckTargetStatus();
            }
            else
            {
                FindTarget();

                //隐藏血条
                healthBarBackground.gameObject.SetActive(false);
            }
        }

        void NormalAttackLoop()
        {
            if (currentNormalAttack == null)
            {
                //如果目标在攻击范围内
                if (IsEntityInNormalAttackRange(targetEntity))
                    currentNormalAttack = new(data.normalAttack);

                return;
            }

            //正在警告中
            if (Tools.time < currentNormalAttack.warningEndTime)
            {
                //显示红色感叹号
                statusMarkImage.image.enabled = true;
                statusMarkImage.image.color = Color.red;
            }
            //正在躲避空档中
            else if (Tools.time < currentNormalAttack.dodgeEndTime)
            {
                //显示蓝色感叹号
                statusMarkImage.image.enabled = true;
                statusMarkImage.image.color = Color.blue;

                //如果在攻击范围内且面对着目标
                if (targetEntity is Player player && Tools.time < player.parryEndTime && player.lockOnTarget == this && IsEntityInNormalAttackRange(targetEntity))
                {
                    var targetOrientation = targetEntity.GetOrientation();
                    if (targetOrientation != GetOrientation())
                    {
                        //攻击无效
                        currentNormalAttack = null;

                        //敌人被击退
                        rb.velocity += new Vector2(8 * (targetOrientation ? 1 : -1), 0);

                        //播放音效
                        GAudio.Play(AudioID.ParrySucceed, transform.position);

                        //播放盾反粒子
                        GM.instance.parrySuccessParticle.transform.position = 0.5f * (transform.position + targetEntity.transform.position);
                        GM.instance.parrySuccessParticle.Play();
                    }
                }
            }
            //攻击还未结束
            else if (Tools.time < currentNormalAttack.recoveryEndTime)
            {
                //隐藏感叹号
                statusMarkImage.image.enabled = false;

                //如果正在攻击判定时间中且还没有攻击到目标
                if (Tools.time < currentNormalAttack.hitJudgementEndTime && !currentNormalAttack.hasSucceed && IsEntityInNormalAttackRange(targetEntity))
                {
                    currentNormalAttack.hasSucceed = true;
                    NormallyAttackEntity(targetEntity);
                }
            }
            //攻击结束
            else
            {
                currentNormalAttack = null;
            }
        }

        public virtual void NormallyAttackEntity(Entity entity)
        {
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

        public bool IsEntityInNormalAttackRange(Entity entity)
        {
            //为了性能使用 (x - x), (y - y) 而不是 Vector2.Distance()
            float disX = Mathf.Abs(transform.position.x - entity.transform.position.x);
            float disY = Mathf.Abs(transform.position.y - entity.transform.position.y);

            return disX <= data.normalAttack.radius && disY <= data.normalAttack.radius;
        }

        public bool IsEntityInSearchRange(Entity entity)
        {
            return (entity.transform.position - transform.position).sqrMagnitude <= data.searchRadiusSqr;
        }

        /// <returns>是否向服务器发送了伤害请求</returns>
        public virtual bool AttackEntity(Entity entity)
        {
            if (data.normalAttack == null)
                return false;

            return entity.TakeDamage(
                data.normalAttack.damage,
                0.3f,
                transform.position,
                new(impactForceConst.x * (transform.position.x < targetEntity.transform.position.x ? 1 : -1), impactForceConst.y)
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
                if (IsEntityInSearchRange(player) && !player.isDead)
                {
                    targetEntity = player;
                }
            }
        }

        public void CheckTargetStatus()
        {
            if (targetEntity.isDead || !IsEntityInSearchRange(targetEntity))
                targetEntity = null;
        }

        void RefreshHealthBar()
        {
            healthBarFull.image.fillAmount = health / (float)maxHealth;
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



        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            Enemy.enemyLayer = LayerMask.NameToLayer("Enemy");
            Enemy.enemyLayerMask = Enemy.enemyLayer.LayerMaskOnly();
        }
    }
}
