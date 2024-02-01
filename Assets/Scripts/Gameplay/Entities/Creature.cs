using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.High;
using Mirror;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        public Func<float> velocityFactor = null;
        [HideInInspector] public GameObject model;
        [BoxGroup("属性"), LabelText("移速")] public float moveSpeed = 3;
        [BoxGroup("属性"), LabelText("加速度倍数")] public float accelerationMultiple = 0.18f;
        [BoxGroup("属性"), LabelText("空气摩擦力")] public float airFriction = 0.95f;
        public AnimWeb animWeb = null;



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

        [LabelText("身体部位"), ReadOnly, BoxGroup("组件")]
        public List<CreatureBodyPart> bodyParts = new();






        /* -------------------------------------------------------------------------- */
        /*                                    临时数据                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("属性"), LabelText("跳跃计时器"), ReadOnly] public float jumpTimer;
        [BoxGroup("属性"), LabelText("跳跃CD"), ReadOnly] public float jumpCD = 0.3f;
        [BoxGroup("属性"), LabelText("掉落的Y位置")] public float fallenY;
        public static float fallenDamageHeight = 9;





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


            creature.animWeb = new();

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
                float rot = -70f;

                creature.animWeb.AddAnim("attack_leftarm", 1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftArm.transform, rot, 0f, Ease.Linear,RotateMode.Fast),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, -360 - rot, attackAnimTime, Ease.InOutSine,RotateMode.LocalAxisAdd)
                }, 0);

                creature.animWeb.AddAnim("attack_rightarm", 1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.rightArm.transform, rot, 0f, Ease.Linear,RotateMode.Fast),
                    new LocalRotationZAnimFragment(creature.rightArm.transform, -360 - rot, attackAnimTime, Ease.InOutSine,RotateMode.LocalAxisAdd)
                }, 0);
            }
            #endregion

            #region 抬手
            {
                float time = 0.15f; //!Player的绑定部分使用到了这个时间
                float rot = 35f;

                creature.animWeb.AddAnim("slight_leftarm_lift", 1, new AnimFragment[] {
                    new LocalRotationZAnimFragment(creature.leftArm.transform, rot, time, Ease.InOutSine),
                    new LocalRotationZAnimFragment(creature.leftArm.transform, 0f, time, Ease.InOutSine)
                }, 0);

                creature.animWeb.AddAnim("slight_rightarm_lift", 1, new AnimFragment[] {
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
                float bodyRotate = -5f;
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
                    new LocalRotationZAnimFragment(creature.head.transform, bodyRotate, bodyRotateTime, Ease.Linear),
                });
            }
            #endregion


            creature.animWeb.GroupAnim(0, "idle", "idle_head", "idle_body", "idle_leftarm", "idle_rightarm", "idle_leftleg", "idle_rightleg");

            creature.animWeb.GroupAnim(0, "run", "run_rightarm", "run_leftarm", "run_rightleg", "run_leftleg", "run_head", "run_body");
            creature.animWeb.CreateConnectionFromTo("idle", "run", () => creature.isMoving);
            creature.animWeb.CreateConnectionFromTo("run", "idle", () => !creature.isMoving);

            creature.animWeb.CreateConnectionFromTo("attack_leftarm", "idle", () => true, attackAnimTime, 0); //过渡时间为 attackAnimTime, 意思是播放完成再切换
            creature.animWeb.CreateConnectionFromTo("attack_rightarm", "idle", () => true, attackAnimTime, 0); //过渡时间为 attackAnimTime, 意思是播放完成再切换

            creature.animWeb.SwitchPlayingTo("idle", 0);
        }





        protected override void Awake()
        {
            velocityFactor = () => moveSpeed;

            base.Awake();
        }

        public override void InitAfterAwake()
        {
            base.InitAfterAwake();

            moveSpeed = data.speed;
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            animWeb?.UpdateWeb();

#if UNITY_EDITOR
            isMoving_for_editor_view = isMoving;
#endif
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            if (isDead)
                isMoving = false;
        }




        /// <summary>
        /// 注：这个方法耗时并不高，无需过度优化
        /// </summary>
        /// <param name="movementDirection"></param>
        /// <returns></returns>
        public Vector2 GetMovementVelocity(Vector2 movementDirection)
        {
            float max = velocityFactor();
            float acceleration = max * accelerationMultiple;

            float xVelocity = Math.Abs(rb.velocity.x) > max ? rb.velocity.x : (rb.velocity.x + movementDirection.x * acceleration);
            float yVelocity = Math.Abs(rb.velocity.y) > max ? rb.velocity.y : (rb.velocity.y + movementDirection.y * acceleration);

            return new(xVelocity * airFriction, yVelocity * airFriction);
        }






        [ServerRpc, Button]
        protected void ServerOnStartMovement(NetworkConnection caller = null)
        {
            isMoving = true;
            isMoving_get(); //必须要调用 isMoving_get()，否则 isMoving 的值不会更新 (我也不知道为什么)
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
            isMoving = false;
            isMoving_get(); //必须要调用 isMoving_get()，否则 isMoving 的值不会更新 (我也不知道为什么)
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

            Movement();

            if (isLocalPlayer)
            {
                //摔落伤害
                if (rb && rb.velocity.y >= 0)
                {
                    float fallingStartPoint = fallenY;
                    fallenY = transform.position.y;
                    float delta = fallingStartPoint - fallenY;
                    float damageValue = (delta - fallenDamageHeight) * 0.9f;

                    //generatedFirstRegion 是防止初始区域 y<0 导致直接摔死
                    if (damageValue >= 2)
                    {
                        TakeDamage(damageValue);
                    }
                }
            }
        }




        public virtual void OnStartAttack()
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
            rb.SetVelocityY(GetJumpVelocity(30));   //设置 Y 速度
        }

        public float GetJumpVelocity(float jumpVelocity)
        {
            if (Tools.time < jumpTimer)
                return rb.velocity.y;

            jumpTimer = Tools.time + jumpCD;

            return jumpVelocity;   //设置 Y 速度
        }


        public void CreateModel()
        {
            model = new("model");
            model.transform.SetParent(transform);
            model.transform.SetLocalPositionAndRotation(Vector2.zero, Quaternion.identity);
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
            AddSpriteRenderer(part.armorSr);
            AddSpriteRenderer(part.sr);
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
