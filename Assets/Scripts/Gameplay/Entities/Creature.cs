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
        public AnimWeb animWeb = new();



        [SyncGetter] bool isMoving_get() => default; [SyncSetter] void isMoving_set(bool value) { }
        [Sync, SyncDefaultValue(false)] public bool isMoving { get => isMoving_get(); set => isMoving_set(value); }

#if UNITY_EDITOR
        [SerializeField] bool isMoving_for_editor_view;
#endif


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
        [BoxGroup("属性"), LabelText("跳跃CD"), ReadOnly] public float jumpCD = 0.3f;





        /* -------------------------------------------------------------------------- */
        /*                                Behaviour 系统                                */
        /* -------------------------------------------------------------------------- */
        public virtual void Movement() { }





        /* -------------------------------------------------------------------------- */
        /*                               Static Methods                               */
        /* -------------------------------------------------------------------------- */
        public static void BindHumanAnimations(Creature creature)
        {
            float attackAnimTime = 0.3f;

            #region 静止
            {
                float time = 1.5f;

                creature.animWeb.AddAnim("idle_head", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.head.transform, -4.5f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.head.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.head.transform, 4.5f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.head.transform, 0f, time, Ease.InOutSine)
                });

                creature.animWeb.AddAnim("idle_body", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.body.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.body.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.body.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.body.transform, 0f, time, Ease.InOutSine)
                });

                creature.animWeb.AddAnim("idle_leftarm", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, time, Ease.InOutSine)
                });

                creature.animWeb.AddAnim("idle_rightarm", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.rightArm.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, 0f, time, Ease.InOutSine)
                });

                creature.animWeb.AddAnim("idle_leftleg", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftLeg.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftLeg.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftLeg.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftLeg.transform, 0f, time, Ease.InOutSine)
                });

                creature.animWeb.AddAnim("idle_rightleg", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.rightLeg.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightLeg.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightLeg.transform, 0f, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightLeg.transform, 0f, time, Ease.InOutSine)
                });
            }
            #endregion

            #region 攻击
            {
                float rot = 55f;

                creature.animWeb.AddAnim("attack_leftarm", 1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftArm.transform, rot, attackAnimTime, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, attackAnimTime, Ease.InOutSine)
                }, 0);

                creature.animWeb.AddAnim("attack_rightarm", 1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.rightArm.transform, rot, attackAnimTime, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, 0f, attackAnimTime, Ease.InOutSine)
                }, 0);
            }
            #endregion

            #region 挖掘
            {
                float time = 0.15f; //!Player的绑定部分使用到了这个时间
                float rot = 35f;

                creature.animWeb.AddAnim("excavate_leftarm", 1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftArm.transform, rot, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, time, Ease.InOutSine)
                }, 0);

                creature.animWeb.AddAnim("excavate_rightarm", 1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.rightArm.transform, rot, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, 0f, time, Ease.InOutSine)
                }, 0);
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

                creature.animWeb.AddAnim("run_rightleg", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.rightLeg.transform, -legRotate, legRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.rightLeg.transform, legRotate, legRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.rightLeg.transform, 0f, legRotateTime / 2, Ease.Linear),
                });

                creature.animWeb.AddAnim("run_leftleg", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftLeg.transform, legRotate, legRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.leftLeg.transform, -legRotate, legRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.leftLeg.transform, 0f, legRotateTime / 2, Ease.Linear),
                });

                creature.animWeb.AddAnim("run_rightarm", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.rightArm.transform, -armRotate, armRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, armRotate, armRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, 0f, armRotateTime / 2, Ease.Linear),
                });

                creature.animWeb.AddAnim("run_leftarm", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftArm.transform, armRotate, armRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, -armRotate, armRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, armRotateTime / 2, Ease.Linear),
                });

                creature.animWeb.AddAnim("run_head", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.head.transform, -headRotate, headRotateTime, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.head.transform, headRotate, headRotateTime, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.head.transform, 0f, headRotateTime / 2, Ease.InOutSine),
                });

                creature.animWeb.AddAnim("run_body", -1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.head.transform, -bodyRotate, bodyRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.head.transform, bodyRotate, bodyRotateTime, Ease.Linear),
                    new LocalRotationZAnimFragment(creature.head.transform, 0f, bodyRotateTime / 2, Ease.Linear),
                });
            }
            #endregion


            creature.animWeb.GroupAnim(0, "idle", "idle_head", "idle_body", "idle_leftarm", "idle_rightarm", "idle_leftleg", "idle_rightleg");

            creature.animWeb.GroupAnim(0, "run", "run_rightarm", "run_leftarm", "run_rightleg", "run_leftleg", "run_head", "run_body");
            creature.animWeb.CreateConnectionFromTo("idle", "run", () => creature.isMoving);
            creature.animWeb.CreateConnectionFromTo("run", "idle", () => !creature.isMoving);

            creature.animWeb.CreateConnectionFromTo("attack_leftarm", "idle", () => true, attackAnimTime * 2, 0); //过渡时间为 attackAnimTime, 意思是播放完成再切换
            creature.animWeb.CreateConnectionFromTo("attack_rightarm", "idle", () => true, attackAnimTime * 2, 0); //过渡时间为 attackAnimTime, 意思是播放完成再切换



            creature.animWeb.SwitchPlayingTo("idle", 0);
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

            animWeb?.UpdateWeb();
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


#if UNITY_EDITOR
            isMoving_for_editor_view = isMoving;
#endif
        }





        public Vector2 GetMovementVelocity(Vector2 movementDirection)
        {
            float multi = moveMultiple();
            float xVelocity = (rb.velocity.x.Abs() > multi ? (rb.velocity.x) : (rb.velocity.x + movementDirection.x * multi * startingSpeedMultiple)) * movementAirResistance;
            float yVelocity = (rb.velocity.y.Abs() > multi ? (rb.velocity.y) : (rb.velocity.y + movementDirection.y * multi * startingSpeedMultiple)) * movementAirResistance;

            return new(xVelocity, yVelocity);
        }






        [ServerRpc]
        protected void ServerOnStartMovement(NetworkConnection caller = null)
        {
            Debug.Log("Start");
            isMoving = true;
            ClientOnStartMovement();
        }

        [ClientRpc]
        protected void ClientOnStartMovement(NetworkConnection caller = null)
        {
            OnStartMovementAction();
        }

        public Action OnStartMovementAction = () => { };









        [ServerRpc]
        protected void ServerOnStopMovement(NetworkConnection caller = null)
        {
            Debug.Log("Stop");
            isMoving = false;
            ClientOnStopMovement();
        }

        [ClientRpc]
        protected void ClientOnStopMovement(NetworkConnection caller = null)
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
            animWeb?.Stop();
        }

        public void Jump()
        {
            rb.SetVelocityY(GetJumpVelocity(35));   //设置 Y 速度
        }

        public float GetJumpVelocity(float jumpVelocity)
        {
            if (Tools.time < jumpTimer)
                return rb.velocity.y;

            jumpTimer = Tools.time + jumpCD;

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