using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.High;
using Mirror;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 生物, 实体的扩展
    /// </summary>
    public class Creature : Entity
    {
        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        public Func<float> moveMultiple = () => 1;
        [HideInInspector] public GameObject model;
        [BoxGroup("属性"), LabelText("移速")] public float moveSpeed = 3;
        [BoxGroup("属性"), LabelText("起步速度倍数")] public float startingSpeedMultiple = 0.25f;
        [BoxGroup("属性"), LabelText("坠落空气阻力")] public float movementAirResistance = 0.95f;
        public bool onGround { get; set; }
        public AnimObject anim = new();



        [SyncGetter] bool isMoving_get() => default; [SyncSetter] void isMoving_set(bool value) { }
        [Sync, SyncDefaultValue(false)] public bool isMoving { get => isMoving_get(); set => isMoving_set(value); }


        [HideInInspector] public CreatureBodyPart head { get; set; }
        [HideInInspector] public CreatureBodyPart rightArm { get; set; }
        [HideInInspector] public CreatureBodyPart body { get; set; }
        [HideInInspector] public CreatureBodyPart leftArm { get; set; }
        [HideInInspector] public CreatureBodyPart rightLeg { get; set; }
        [HideInInspector] public CreatureBodyPart leftLeg { get; set; }
        [HideInInspector] public CreatureBodyPart rightFoot { get; set; }
        [HideInInspector] public CreatureBodyPart leftFoot { get; set; }





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static Func<Creature, bool> CanMove = c => true;

        [LabelText("身体部位"), ReadOnly, BoxGroup("组件")]
        public List<CreatureBodyPart> bodyParts = new();






        /* -------------------------------------------------------------------------- */
        /*                                    临时数据                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("属性"), LabelText("跳跃计时器"), ReadOnly] public float jumpTimer;





        /* -------------------------------------------------------------------------- */
        /*                                Behaviour 系统                                */
        /* -------------------------------------------------------------------------- */
        public virtual void Movement() { }





        /* -------------------------------------------------------------------------- */
        /*                               Static Methods                               */
        /* -------------------------------------------------------------------------- */
        public static void BindHumanAnimations(Creature creature)
        {
            #region 静止
            {
                creature.anim.AddAnim("idle_head", () =>
                {
                    creature.anim.ResetAnimations("idle_head");
                }, () => new Tween[]
                {
                    creature.head.transform.DOLocalRotateZ(-4.5f, 1.5f).SetEase(Ease.Linear),
                    creature.head.transform.DOLocalRotateZ(0f, 1.5f).SetEase(Ease.Linear),
                    creature.head.transform.DOLocalRotateZ(4.5f, 1.5f).SetEase(Ease.Linear),
                    creature.head.transform.DOLocalRotateZ(0f, 1.5f).SetEase(Ease.Linear)
                }, () => creature.head.sequence);
            }
            #endregion

            #region 攻击
            {
                float time = 0.3f;
                float rot = 55f;
                Vector2 move = new(0.085f, 0.05f);

                creature.anim.AddAnim("attack_leftarm", () =>
                {
                    creature.leftArm.ResetSequence(1);

                    creature.leftArm.sequence
                        .OnStepComplete(() =>
                        {
                            creature.anim.SetAnim("attack_leftarm", false);

                            if (creature.isMoving)
                            {
                                //重播放移动动画
                                creature.OnStartMovement();
                            }
                            else
                            {
                                //重播放待机动画
                                creature.OnStopMovement();
                            }
                        });
                }, () => new Tween[]
                {
                    creature.leftArm.transform.DOLocalRotateZ(rot, time),
                    creature.leftArm.transform.DOLocalRotateZ(0, time)
                }, () => creature.leftArm.sequence);

                creature.anim.AddAnim("attack_rightarm", () =>
                {
                    creature.rightArm.ResetSequence(1);

                    creature.rightArm.sequence
                        .OnStepComplete(() =>
                        {
                            creature.anim.SetAnim("attack_rightarm", false);
                            Debug.Log($"Attacked!: {creature.isMoving}");
                            if (creature.isMoving)
                            {
                                //重播放移动动画
                                creature.OnStartMovement();
                            }
                            else
                            {
                                //重播放待机动画
                                creature.OnStopMovement();
                            }
                        });
                }, () => new Tween[]
                {
                    creature.rightArm.transform.DOLocalRotateZ(rot, time),
                    creature.rightArm.transform.DOLocalRotateZ(0, time)
                }, () => creature.rightArm.sequence);
            }
            #endregion

            #region 挖掘
            {
                float time = 0.15f;
                float rot = 35f;
                Vector2 move = new(0.065f, 0.04f);

                creature.anim.AddAnim("excavate_leftarm", () =>
                {
                    creature.leftArm.ResetSequence(1);

                    creature.leftArm.sequence
                        .OnStepComplete(() =>
                        {
                            //停止挖掘动画
                            creature.anim.SetAnim("excavate_leftarm", false);

                            if (creature.isMoving)
                            {
                                //重播放移动动画
                                creature.OnStartMovement();
                            }
                            else
                            {
                                //重播放待机动画
                                creature.OnStopMovement();
                            }
                        });
                }, () => new Tween[]
                {
                    creature.leftArm.transform.DOLocalRotateZ(rot, time),
                    creature.leftArm.transform.DOLocalRotateZ(0, time)
                }, () => creature.leftArm.sequence);

                creature.anim.AddAnim("excavate_rightarm", () =>
                {
                    creature.rightArm.ResetSequence(1);

                    creature.rightArm.sequence
                        .OnStepComplete(() =>
                        {
                            //停止挖掘动画
                            creature.anim.SetAnim("excavate_rightarm", false);

                            if (creature.isMoving)
                            {
                                //重播放移动动画
                                creature.OnStartMovement();
                            }
                            else
                            {
                                //重播放待机动画
                                creature.OnStopMovement();
                            }
                        });
                }, () => new Tween[]
                {
                    creature.rightArm.transform.DOLocalRotateZ(rot, time),
                    creature.rightArm.transform.DOLocalRotateZ(0, time)
                }, () => creature.rightArm.sequence);
            }
            #endregion

            #region 奔跑
            {
                float armRotateTime = 0.3f;
                float legRotateTime = 0.3f;
                float headRotateTime = 0.75f;
                float bodyRotateTime = 0.75f;
                float armRotate = 17.5f;
                float legRotate = 25f;
                float bodyRotate = 3.5f;
                float headRotate = 7;

                creature.anim.AddAnim("run_rightleg", () =>
                {
                    creature.rightLeg.ResetSequence();
                }, () => new Tween[]
                {
                    creature.rightLeg.transform.DOLocalRotateZ(-legRotate, legRotateTime),
                    creature.rightLeg.transform.DOLocalRotateZ(legRotate, legRotateTime),
                    creature.rightLeg.transform.DOLocalRotateZ(0, legRotateTime / 2)
                }, () => creature.rightLeg.sequence);

                creature.anim.AddAnim("run_leftleg", () =>
                {
                    creature.leftLeg.ResetSequence();
                }, () => new Tween[]
                {
                    creature.leftLeg.transform.DOLocalRotateZ(legRotate, legRotateTime),
                    creature.leftLeg.transform.DOLocalRotateZ(-legRotate, legRotateTime),
                    creature.leftLeg.transform.DOLocalRotateZ(0, legRotateTime / 2)
                }, () => creature.leftLeg.sequence);

                creature.anim.AddAnim("run_rightarm", () =>
                {
                    creature.rightArm.ResetSequence();
                }, () => new Tween[]
                {
                    creature.rightArm.transform.DOLocalRotateZ(-armRotate, armRotateTime),
                    creature.rightArm.transform.DOLocalRotateZ(armRotate, armRotateTime),
                    creature.rightArm.transform.DOLocalRotateZ(0, armRotateTime / 2)
                }, () => creature.rightArm.sequence);

                creature.anim.AddAnim("run_leftarm", () =>
                {
                    creature.leftArm.ResetSequence();
                }, () => new Tween[]
                {
                    creature.leftArm.transform.DOLocalRotateZ(armRotate, armRotateTime),
                    creature.leftArm.transform.DOLocalRotateZ(-armRotate, armRotateTime),
                    creature.leftArm.transform.DOLocalRotateZ(0f, armRotateTime / 2)
                }, () => creature.leftArm.sequence);

                creature.anim.AddAnim("run_head", () =>
                {
                    creature.head.ResetSequence();
                }, () => new Tween[]
                {
                    creature.head.transform.DOLocalRotateZ(-headRotate, headRotateTime),
                    creature.head.transform.DOLocalRotateZ(headRotate, headRotateTime),
                    creature.head.transform.DOLocalRotateZ(0f, headRotateTime)
                }, () => creature.head.sequence);

                creature.anim.AddAnim("run_body", () =>
                {
                    creature.body.ResetSequence();
                }, () => new Tween[]
                {
                    creature.body.transform.DOLocalRotateZ(-bodyRotate, bodyRotateTime),
                    creature.body.transform.DOLocalRotateZ(bodyRotate, bodyRotateTime),
                    creature.body.transform.DOLocalRotateZ(0f, bodyRotateTime)
                }, () => creature.body.sequence);
            }
            #endregion


            creature.OnStartMovementAction += () =>
            {
                creature.anim.ResetAnimations();

                creature.anim.SetAnim("run_rightarm");
                creature.anim.SetAnim("run_leftarm");
                creature.anim.SetAnim("run_rightleg");
                creature.anim.SetAnim("run_leftleg");
                creature.anim.SetAnim("run_head");
                creature.anim.SetAnim("run_body");
            };

            creature.OnStopMovementAction += () =>
            {
                creature.anim.ResetAnimations();

                creature.anim.SetAnim("idle_head");
            };
        }





        protected override void Awake()
        {
            moveMultiple = () => moveSpeed;

            model = new("model");
            model.transform.SetParent(transform);

            base.Awake();
        }

        protected override void Update()
        {
            base.Update();

            RefreshHurtEffect();

            //修正位置
            if (model)
            {
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one;

                if (body)
                {
                    body.transform.localPosition = Vector3.zero;
                    body.transform.localScale = Vector3.one;
                }
            }
        }





        public Vector2 GetMovementVelocity(Vector2 movementDirection)
        {
            float multi = moveMultiple();
            float xVelocity = (rb.velocity.x.Abs() > multi ? (rb.velocity.x) : (rb.velocity.x + movementDirection.x * multi * startingSpeedMultiple)) * movementAirResistance;
            float yVelocity = (rb.velocity.y.Abs() > multi ? (rb.velocity.y) : (rb.velocity.y + movementDirection.y * multi * startingSpeedMultiple)) * movementAirResistance;

            return new(xVelocity, yVelocity);
        }






        public void OnStartMovement()
        {
            ServerOnStartMovement(null);
        }

        [ServerRpc]
        protected void ServerOnStartMovement(NetworkConnection caller)
        {
            Debug.Log($"Started!: {isMoving}");
            isMoving = true;
            ConnectionOnStartMovement(caller);
        }

        [ConnectionRpc]
        protected void ConnectionOnStartMovement(NetworkConnection caller)
        {
            OnStartMovementAction();
        }

        public Action OnStartMovementAction = () => { Debug.Log("STARTED"); };









        public void OnStopMovement()
        {
            ServerOnStopMovement(null);
        }

        [ServerRpc]
        protected void ServerOnStopMovement(NetworkConnection caller)
        {
            Debug.Log($"Stopped!: {isMoving}");
            isMoving = false;
            ClientOnStopMovement(caller);
        }

        [ConnectionRpc]
        protected void ClientOnStopMovement(NetworkConnection caller)
        {
            OnStopMovementAction();
        }

        public Action OnStopMovementAction = () => { };



        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (CanMove(this))
                Movement();
        }

        protected override void Start()
        {
            base.Start();

            if (!isPlayer)
            {
                //等一帧再设置, 否则会被 Entity 覆盖
                //TODO: 移动到Init
                WaitOneFrame(() =>
                {
                    moveSpeed = data.speed;
                });
            }
        }




        protected virtual void OnStartAttack()
        {

        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            //Kill 所有的动画防止警告
            var animationSequences = GetComponentsInChildren<IAnimSequence>();

            for (int i = animationSequences.Length - 1; i >= 0; i--)
            {
                animationSequences[i].sequence.Kill();
            }
        }

        public void Jump()
        {
            rb.SetVelocityY(GetJumpVelocity(35));   //设置 Y 速度
        }

        public float GetJumpVelocity(float jumpVelocity)
        {
            if (Tools.time < jumpTimer)
                return rb.velocity.y;

            jumpTimer = Tools.time + 0.3f;

            return jumpVelocity;   //设置 Y 速度
        }

        public virtual void RefreshHurtEffect()
        {
            foreach (var sr in spriteRenderers)
            {
                if (isHurting)
                    sr.color = new(sr.color.r, 0.5f, 0.5f);
                else
                    sr.color = Color.white;
            }
        }



        public virtual CreatureBodyPart AddBodyPart(string partName, Sprite sprite, Vector2 jointExtraOffset, int sortingOrder, CreatureBodyPart parent, BodyPartType type = BodyPartType.Body, Vector2? textureOffset = null)
        => AddBodyPart<CreatureBodyPart>(partName, sprite, jointExtraOffset, sortingOrder, parent, parent.transform, type, textureOffset ?? Vector2.zero);

        public virtual CreatureBodyPart AddBodyPart(string partName, Sprite sprite, Vector2 jointExtraOffset, int sortingOrder, Transform parentTrans, BodyPartType type = BodyPartType.Body, Vector2? textureOffset = null)
        => AddBodyPart<CreatureBodyPart>(partName, sprite, jointExtraOffset, sortingOrder, null, parentTrans, type, textureOffset ?? Vector2.zero);

        public virtual T AddBodyPart<T>(string partName, Sprite sprite, Vector2 jointExtraOffset, int sortingOrder, CreatureBodyPart parent, Transform parentTrans, BodyPartType type, Vector2 textureExtraOffset) where T : CreatureBodyPart
        {
            //创建物体并添加组件
            T part = new GameObject(partName).AddComponent<T>();

            //将其设置为子物体
            part.transform.SetParent(parentTrans);

            //为身体匹配贴图并设置层级
            part.armorSr.sortingOrder = sortingOrder + 1;
            part.sr.sprite = sprite;
            part.sr.sortingOrder = sortingOrder;

            //定义偏移变量
            Vector2 jointOffset = Vector2.zero;
            Vector2 textureOffset = Vector2.zero;

            float parentHeight = parent?.GetHeightToNormal() ?? 0;
            float parentWidth = parent?.GetWidthToNormal() ?? 0;
            float selfHeight = part.GetHeightToNormal();

            //根据类型自动设置位置
            switch (type)
            {
                case BodyPartType.RightLeg:
                    jointOffset = new(-parentWidth / 4, -parentHeight / 2);
                    textureOffset = new(0, -selfHeight / 2);
                    break;

                case BodyPartType.LeftLeg:
                    jointOffset = new(parentWidth / 4, -parentHeight / 2);
                    textureOffset = new(0, -selfHeight / 2);
                    break;

                case BodyPartType.Head:
                    jointOffset = new(0, parentHeight / 2);
                    textureOffset = new(0, selfHeight / 2);
                    break;

                case BodyPartType.RightArm:
                    jointOffset = new(-parentWidth / 2, parentWidth / 2);
                    textureOffset = new(0, -selfHeight / 2);
                    break;

                case BodyPartType.LeftArm:
                    jointOffset = new(parentWidth / 2, parentWidth / 2);
                    textureOffset = new(0, -selfHeight / 2);
                    break;

                case BodyPartType.RightFoot:
                    jointOffset = new(0, -parentHeight);
                    textureOffset = new(0, -selfHeight / 2);
                    break;

                case BodyPartType.LeftFoot:
                    jointOffset = new(0, -parentHeight);
                    textureOffset = new(0, -selfHeight / 2);
                    break;
            }

            //应用额外偏移
            jointOffset += jointExtraOffset;
            textureOffset += textureExtraOffset;

            //设置位置
            part.transform.localPosition = jointOffset;
            part.armor.transform.localPosition = textureOffset;

            part.mainBody = this;

            bodyParts.Add(part);
            anim.sequences.Add(part);
            renderers.Add(part.armorSr);
            renderers.Add(part.sr);
            spriteRenderers.Add(part.armorSr);
            spriteRenderers.Add(part.sr);
            return part;
        }
    }





    /* -------------------------------------------------------------------------- */
    /*                                    公共类型                                    */
    /* -------------------------------------------------------------------------- */
    public interface IHumanBodyParts<T> where T : CreatureBodyPart
    {
        [HideInInspector] T head { get; set; }
        [HideInInspector] T rightArm { get; set; }
        [HideInInspector] T body { get; set; }
        [HideInInspector] T leftArm { get; set; }
        [HideInInspector] T rightLeg { get; set; }
        [HideInInspector] T leftLeg { get; set; }
        [HideInInspector] T leftFoot { get; set; }
        [HideInInspector] T rightFoot { get; set; }
    }

    public interface IHumanUsingItemRenderer
    {
        [HideInInspector] SpriteRenderer usingItemRenderer { get; set; }
    }
}
