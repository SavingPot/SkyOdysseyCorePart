using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.High;
using GameCore.UI;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GameCore.PlayerUI;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace GameCore
{
    /// <summary>
    /// 玩家的逻辑脚本
    /// </summary>
    [ChineseName("玩家"), EntityBinding(EntityID.Player), NotSummonable, RequireComponent(typeof(Rigidbody2D))]
    public class Player : Creature, IHumanBodyParts<CreatureBodyPart>, IHumanUsingItemRenderer, IPlayerInteraction, IOnInventoryItemChange, IItemContainer, IInventoryOwner, IUseRadius
    {
        /* -------------------------------------------------------------------------- */
        /*                                     接口                                     */
        /* -------------------------------------------------------------------------- */
        Item IItemContainer.GetItem(string index)
        {
            return inventory.GetItem(index);
        }

        void IItemContainer.SetItem(string index, Item value)
        {
            ServerSetItem(index, value);
        }

        Item[] IItemContainer.items { get => inventory.slots; set => inventory.slots = value; }



        public void Interactive(Player caller)
        {
            if (isDead && !playerSavingMe && !caller.savingPlayer)
            {
                Debug.Log("开始救援");
                caller.savingPlayer = this;
                caller.savingTime = 0;
                playerSavingMe = caller;
            }
        }



        public void OnInventoryItemChange(string index)
        {
            var item = inventory.GetItem(index);

            if (isLocalPlayer)
            {
                //刷新背包物品显示
                pui.backpackItemView.CustomMethod("refresh", null);

                //刷新制造界面
                pui.craftingResultView.CustomMethod(null, null);
                pui.craftingStuffView.CustomMethod(null, null);
                pui.choseItemTitleText.RefreshUI();

                //完成成就
                if (!Item.Null(item))
                {
                    switch (item.data.id)
                    {
                        case BlockID.Dirt:
                            pui.CompleteTask("ori:get_dirt");
                            break;

                        case ItemID.FeatherWing:
                            pui.CompleteTask("ori:get_feather_wing");
                            break;

                        case BlockID.Grass:
                            pui.CompleteTask("ori:get_grass");
                            break;

                        case ItemID.StrawRope:
                            pui.CompleteTask("ori:get_straw_rope");
                            break;

                        case ItemID.PlantFiber:
                            pui.CompleteTask("ori:get_plant_fiber");
                            break;

                        case BlockID.Gravel:
                            pui.CompleteTask("ori:get_gravel");
                            break;

                        case ItemID.Flint:
                            pui.CompleteTask("ori:get_flint");
                            break;

                        case BlockID.Stone:
                            pui.CompleteTask("ori:get_stone");
                            break;

                        case BlockID.Campfire:
                            pui.CompleteTask("ori:get_campfire");
                            break;

                        case ItemID.FlintKnife:
                            pui.CompleteTask("ori:get_flint_knife");
                            break;

                        case ItemID.FlintSword:
                            pui.CompleteTask("ori:get_flint_sword");
                            break;

                        default:
                            if (item.data.GetTag("ori:log").hasTag)
                                pui.CompleteTask("ori:get_log");

                            if (item.data.GetTag("ori:meat").hasTag)
                                pui.CompleteTask("ori:get_meat");

                            if (item.data.GetTag("ori:feather").hasTag)
                                pui.CompleteTask("ori:get_feather");

                            if (item.data.GetTag("ori:planks").hasTag)
                                pui.CompleteTask("ori:get_planks");

                            if (item.data.GetTag("ori:stick").hasTag)
                                pui.CompleteTask("ori:get_stick");

                            if (item.data.GetTag("ori:bark").hasTag)
                                pui.CompleteTask("ori:get_bark");

                            if (item.data.GetTag("ori:bark_vest").hasTag)
                                pui.CompleteTask("ori:get_bark_vest");

                            break;
                    }
                }
            }
        }





        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("属性"), LabelText("经验")] public int experience;
        [LabelText("控制层"), BoxGroup("状态")] public BlockLayer controllingLayer;
        [BoxGroup("属性"), LabelText("挖掘范围")] public float excavationRadius = 2.75f;
        [BoxGroup("属性"), LabelText("掉落的Y位置")] public float fallenY;
        [BoxGroup("属性"), LabelText("重力")] public float gravity;
        [BoxGroup("属性"), LabelText("死亡计时器"), HideInInspector] public float deathTimer;

        public bool askingForGeneratingSandbox { get; private set; }
        public float askingForGeneratingSandboxTime { get; private set; } = float.NegativeInfinity;
        public bool generatedFirstSandbox;
        [SyncVar] public List<TaskStatusForSave> completedTasks;



        #region 同步变量

        #region 皮肤

        [SyncGetter] Sprite skinHead_get() => default; [SyncSetter] void skinHead_set(Sprite value) { }
        [Sync] public Sprite skinHead { get => skinHead_get(); set => skinHead_set(value); }
        [SyncGetter] Sprite skinBody_get() => default; [SyncSetter] void skinBody_set(Sprite value) { }
        [Sync] public Sprite skinBody { get => skinBody_get(); set => skinBody_set(value); }
        [SyncGetter] Sprite skinLeftArm_get() => default; [SyncSetter] void skinLeftArm_set(Sprite value) { }
        [Sync] public Sprite skinLeftArm { get => skinLeftArm_get(); set => skinLeftArm_set(value); }
        [SyncGetter] Sprite skinRightArm_get() => default; [SyncSetter] void skinRightArm_set(Sprite value) { }
        [Sync] public Sprite skinRightArm { get => skinRightArm_get(); set => skinRightArm_set(value); }
        [SyncGetter] Sprite skinLeftLeg_get() => default; [SyncSetter] void skinLeftLeg_set(Sprite value) { }
        [Sync] public Sprite skinLeftLeg { get => skinLeftLeg_get(); set => skinLeftLeg_set(value); }
        [SyncGetter] Sprite skinRightLeg_get() => default; [SyncSetter] void skinRightLeg_set(Sprite value) { }
        [Sync] public Sprite skinRightLeg { get => skinRightLeg_get(); set => skinRightLeg_set(value); }
        [SyncGetter] Sprite skinLeftFoot_get() => default; [SyncSetter] void skinLeftFoot_set(Sprite value) { }
        [Sync] public Sprite skinLeftFoot { get => skinLeftFoot_get(); set => skinLeftFoot_set(value); }
        [SyncGetter] Sprite skinRightFoot_get() => default; [SyncSetter] void skinRightFoot_set(Sprite value) { }
        [Sync] public Sprite skinRightFoot { get => skinRightFoot_get(); set => skinRightFoot_set(value); }

        #endregion

        #region 属性
        #region 口渴值
        [SyncGetter] float thirstValue_get() => default; [SyncSetter] void thirstValue_set(float value) { }
        [Sync] public float thirstValue { get => thirstValue_get(); set => thirstValue_set(value); }
        public static float defaultThirstValue = 100;
        public static float maxThirstValue = 100;
        #endregion

        #region 饥饿值
        [SyncGetter] float hungerValue_get() => default; [SyncSetter] void hungerValue_set(float value) { }
        [Sync] public float hungerValue { get => hungerValue_get(); set => hungerValue_set(value); }
        public static float defaultHungerValue = 100;
        public static float maxHungerValue = 100;

        #region 幸福值
        [SyncGetter] float happinessValue_get() => default; [SyncSetter] void happinessValue_set(float value) { }
        [Sync] public float happinessValue { get => happinessValue_get(); set => happinessValue_set(value); }
        public static float defaultHappinessValue = 50;
        public static float maxHappinessValue = 100;
        #endregion
        #endregion

        [SyncGetter] string playerName_get() => default; [SyncSetter] void playerName_set(string value) { }
        [Sync(nameof(OnNameChangeMethod)), SyncDefaultValue("")] public string playerName { get => playerName_get(); set => playerName_set(value); }

        void OnNameChangeMethod()
        {
            OnNameChange(playerName);
        }
        #region 物品

        [BoxGroup("属性"), SyncVar(hook = nameof(OnServerInventoryChanged)), LabelText("物品")]
        public Inventory inventory;

        public Inventory GetInventory() => inventory;
        public void SetInventory(Inventory value) => inventory = value;

        private void OnServerInventoryChanged(Inventory oldValue, Inventory newValue)
        {
            inventory.owner = this;
            inventory.slotsBehaviours = new ItemBehaviour[inventory.slots.Length];

            if (!isServer)
            {
                //网络传输 Type 会丢失, 要重新匹配来恢复一些信息
                inventory.ResumeFromNetwork();
            }
        }



        public int usingItemIndex = 0;

        public Item TryGetUsingItem() => inventory.TryGetItem(usingItemIndex);
        public ItemBehaviour TryGetUsingItemBehaviour() => inventory.TryGetItemBehaviour(usingItemIndex);

        #endregion

#if UNITY_EDITOR
        [Button("输出玩家名称")] private void EditorOutputPlayerName() => Debug.Log($"玩家名: {playerName}");
        [Button("输出玩家血量")] private void EditorOutputHealth() => Debug.Log($"血量: {health}");
        [Button("输出沙盒序号")] private void EditorOutputSandboxIndex() => Debug.Log($"沙盒序号: {sandboxIndex}");
        [Button("AutoTest0 传输测试"), ServerRpc] private void EditorAutoTest0TransportationServer(NetworkConnection caller) { EditorAutoTest0TransportationCaller(new() { id = null, num = UnityEngine.Random.Range(0, 10000), testList = new() { Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName() }, /*testArray = new float[] { UnityEngine.Random.Range(-10000f, 100000f), UnityEngine.Random.Range(-10000f, 100000f), UnityEngine.Random.Range(-10000f, 100000f) },*/ testNullable = Tools.randomBool ? UnityEngine.Random.Range(-10000, 100000) : null }, caller); }
        [ConnectionRpc] private void EditorAutoTest0TransportationCaller(AutoTest0 param0, NetworkConnection caller) { Debug.Log($"id:{param0.id}, num:{param0.num}, testList:{param0.testList[0]}-{param0.testList[1]}-{param0.testList[2]}, nullable:{param0.testNullable}"); }
        [Button("AutoTest1 传输测试"), ServerRpc] private void EditorAutoTest1TransportationServer(NetworkConnection caller) { EditorAutoTest1TransportationCaller(new() { index = UnityEngine.Random.Range(0, 10000), self = Path.GetRandomFileName(), t0 = new() { id = Path.GetRandomFileName(), num = UnityEngine.Random.Range(0, 10000), testList = new() { Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName() } } }, caller); }
        [ConnectionRpc] private void EditorAutoTest1TransportationCaller(AutoTest1 param0, NetworkConnection caller) { Debug.Log($"index:{param0.index}, self:{param0.self}            -id:{param0.t0.id}, num:{param0.t0.num}, tests:{param0.t0.testList[0]}-{param0.t0.testList[1]}-{param0.t0.testList[2]}"); }
        [Button("AutoTest2 传输测试"), ServerRpc]
        private void EditorAutoTest2TransportationServer(NetworkConnection caller)
        {
            EditorAutoTest2TransportationCaller(new() { byte_index = (byte)UnityEngine.Random.Range(0, 255), uint_index = (uint)UnityEngine.Random.Range(0, 100000), long_index = UnityEngine.Random.Range(0, int.MaxValue) + UnityEngine.Random.Range(0, int.MaxValue) + UnityEngine.Random.Range(0, int.MaxValue) + UnityEngine.Random.Range(0, int.MaxValue), t1 = new() { index = UnityEngine.Random.Range(0, 10000), self = Path.GetRandomFileName(), t0 = new() { id = Path.GetRandomFileName(), num = UnityEngine.Random.Range(0, 10000), testList = new() { Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName() } } } }, caller);
        }
        [ConnectionRpc] private void EditorAutoTest2TransportationCaller(AutoTest2 param0, NetworkConnection caller) { Debug.Log($"uint-index:{param0.uint_index}, long-index:{param0.long_index}, byte-index:{param0.byte_index}             index:{param0.t1.index}, self:{param0.t1.self}            -id:{param0.t1.t0.id}, num:{param0.t1.t0.num}, tests:{param0.t1.t0.testList[0]}-{param0.t1.t0.testList[1]}-{param0.t1.t0.testList[2]}"); }
        [Button("设置手中物品")]
        private void EditorSetUsingItem(string id = "ori:", ushort count = 1)
        {
            var item = ModConvert.DatumItemBaseToDatumItem(ModFactory.CompareItem(id));
            item.count = count;

            ServerSetItem(usingItemIndex.ToString(), item);
        }
        [Button("快速获取最大-替换手中物品")]
        private void EditorSetUsingItem(string id = BlockID.GrassBlock)
        {
            EditorSetUsingItem(id, ushort.MaxValue);
        }
#endif
        #endregion

        #region 救援
        [SyncGetter] Player savingPlayer_get() => default; [SyncSetter] void savingPlayer_set(Player value) { }
        [Sync, SyncDefaultValue(null)] public Player savingPlayer { get => savingPlayer_get(); set => savingPlayer_set(value); }
        [SyncGetter] Player playerSavingMe_get() => default; [SyncSetter] void playerSavingMe_set(Player value) { }
        [Sync, SyncDefaultValue(null)] public Player playerSavingMe { get => playerSavingMe_get(); set => playerSavingMe_set(value); }
        [SyncGetter] float savingTime_get() => default; [SyncSetter] void savingTime_set(float value) { }
        [Sync, SyncDefaultValue(0f)] public float savingTime { get => savingTime_get(); set => savingTime_set(value); }

        #endregion

        #endregion



        #region 物品属性

        public float excavationStrength => TryGetUsingItem()?.data?.excavationStrength ?? ItemData.defaultExcavationStrength;

        [BoxGroup("属性"), LabelText("使用时间")]
        public float useTime;





        [HideInInspector] public SpriteRenderer usingItemRenderer { get; set; }

        #endregion





        /* -------------------------------------------------------------------------- */
        /*                                    临时数据                                    */
        /* -------------------------------------------------------------------------- */
        private float moveVecLastFrame;





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static int playerLayer { get; private set; }
        public static int playerLayerMask { get; private set; }
        public static int playerOnGroundLayerMask { get; private set; }
        public static float interactionRadius = 3;
        public static int mainInventorySlotCount = 8;   //偶数
        public static int halfMainInventorySlotCount = mainInventorySlotCount / 2;
        public static Func<Player, bool> PlayerCanControl = player => GameUI.page == null || !GameUI.page.ui;
        public const float playerDefaultGravity = 7f;
        public static float fallenDamageHeight = 9;

        public static Quaternion deathQuaternion = Quaternion.Euler(0, 0, 90);
        public static float deathLowestColorFloat = 0.45f;
        public static float oneMinusDeathLowestColorFloat = 1 - deathLowestColorFloat;
        public static Color deathLowestColor = new(deathLowestColorFloat, deathLowestColorFloat, deathLowestColorFloat);

        public static float enoughSavingTime = 5.5f;







        /* -------------------------------------------------------------------------- */
        /*                                     UI                                     */
        /* -------------------------------------------------------------------------- */
        public PlayerUI pui;

        [BoxGroup("组件"), LabelText("UI画布"), SerializeField, ReadOnly]
        private Transform _playerCanvas;
        public Transform playerCanvas => _playerCanvas;

        [BoxGroup("组件")]
        [LabelText("名字文本")]
        [ReadOnly]
        public TextIdentity nameText;

        public static byte inventorySlotCount = 32;
        public static int backpackPanelHeight = 450;





        /* -------------------------------------------------------------------------- */
        /*                                     动态方法                                    */
        /* -------------------------------------------------------------------------- */
        #region Unity 回调
        protected override void Start()
        {
            data = null;
            base.Start();

            if (isServer)
            {
                inventory = new(inventorySlotCount, this);
                //ServerCallAttribute.Do(ServerCallTry);
            }

            if (isClient)
            {
                moveSpeed = 6.5f;
            }

            if (isLocalPlayer)
            {
                //设置相机跟随
                Tools.instance.mainCameraController.lookAt = transform;
                Debug.Log("本地客户端玩家是: " + gameObject.name);

                //if (!isServer)
                //    CmdCheckMods();

                managerGame.weatherParticle.transform.SetParent(transform);
                managerGame.weatherParticle.transform.localPosition = new(0, 40);


                //加载数据
                WhenRegisteredSyncVars(async () =>
                {
                    skinHead = PlayerSkin.skinHead;
                    skinBody = PlayerSkin.skinBody;
                    skinLeftArm = PlayerSkin.skinLeftArm;
                    skinRightArm = PlayerSkin.skinRightArm;
                    skinLeftLeg = PlayerSkin.skinLeftLeg;
                    skinRightLeg = PlayerSkin.skinRightLeg;
                    skinLeftFoot = PlayerSkin.skinLeftFoot;
                    skinRightFoot = PlayerSkin.skinRightFoot;

                    //要等一帧否则 currentSandboxIndex 会被 base:Entity 设为 0
                    await UniTask.NextFrame();

                    //让服务器加载玩家数据
                    LoadPlayerDatumFromFile();
                });
            }

            WhenCorrectedSyncVars(() =>
            {
                MethodAgent.TryRun(() =>
                {
                    body = AddBodyPart("body", skinBody, Vector2.zero, 5, model.transform, BodyPartType.Body);
                    head = AddBodyPart("head", skinHead, new(0, -0.03f), 10, body, BodyPartType.Head, new(-0.03f, -0.04f));
                    rightArm = AddBodyPart("rightArm", skinRightArm, new(0, 0.03f), 8, body, BodyPartType.RightArm);
                    leftArm = AddBodyPart("leftArm", skinLeftArm, new(0, 0.03f), 3, body, BodyPartType.LeftArm);
                    rightLeg = AddBodyPart("rightLeg", skinRightLeg, new(0.02f, 0.04f), 3, body, BodyPartType.RightLeg);
                    leftLeg = AddBodyPart("leftLeg", skinLeftLeg, new(-0.02f, 0.04f), 1, body, BodyPartType.LeftLeg);
                    rightFoot = AddBodyPart("rightFoot", skinRightFoot, Vector2.zero, 3, rightLeg, BodyPartType.RightFoot);
                    leftFoot = AddBodyPart("leftFoot", skinLeftFoot, Vector2.zero, 1, leftLeg, BodyPartType.LeftFoot);

                    //添加双手物品的显示
                    usingItemRenderer = ObjectTools.CreateSpriteObject(rightArm.transform, "item");

                    usingItemRenderer.sortingOrder = 9;

                    usingItemRenderer.transform.localPosition = new(0.1f, -0.5f);
                    usingItemRenderer.transform.SetScale(0.5f, 0.5f);
                }, true);

                BindHumanAnimations(this);

                OnNameChange(playerName);
            });
        }

        protected override void Awake()
        {
            PlayerSkin.SetSkinByName(GFiles.settings.playerSkinName);

            base.Awake();

            PlayerCenter.all.Add(this);

            Func<float> oldValue = moveMultiple;
            moveMultiple = () => oldValue() * (transform.localScale.x.Sign() != rb.velocity.x.Sign() ? 0.75f : 1);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            anim.KillSequences();

            PlayerCenter.all.Remove(this);
            PlayerCenter.allReady.Remove(this);
            backpackSidebarTable.Remove("ori:craft");
        }

        public override void OnReady()
        {
            base.OnReady();

            PlayerCenter.allReady.Add(this);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isLocalPlayer)
                LocalFixedUpdate();

            onGround = RayTools.TryOverlapCircle(mainCollider.DownPoint(), 0.2f, playerOnGroundLayerMask, out _);

            AutoSetPlayerOrientation(this);
        }

        protected void LocalFixedUpdate()
        {
            FallingDamage(this);
        }


        public Action<Player> FallingDamage = caller =>
        {
            if (caller.rb.velocity.y >= 0)
            {
                float oldValue = caller.fallenY;
                caller.fallenY = caller.transform.position.y;
                float delta = oldValue - caller.fallenY;
                float damageValue = (delta - fallenDamageHeight) * 0.75f;

                //generatedFirstSandbox 是防止初始沙盒 y<0 导致直接摔死
                if (damageValue >= 2 && caller.generatedFirstSandbox)
                {
                    caller.TakeDamage(damageValue);
                }
            }
            // //如果下落了 100格 且下方 50格 无方块
            // else if (!caller.isDead && caller.fallenY - caller.transform.position.y > 100 && !RayTools.Hit(caller.transform.position, Vector2.down, 50, Block.blockLayerMask))
            // {
            //     caller.health = 0;
            //     caller.Death();
            // }
        };





        protected override void Update()
        {
            if (correctedSyncVars && !isHurting)
                DeathColor();

            base.Update();

            if (isLocalPlayer)
                LocalUpdate();

            inventory?.DoBehaviours();

            if (pui != null && pui.useItemButtonImage && pui.useItemButtonImage.gameObject.activeInHierarchy)
            {
                pui.useItemButtonImage.image.sprite = TryGetUsingItem()?.data?.texture?.sprite;
                pui.useItemButtonImage.image.color = pui.useItemButtonImage.image.sprite ? Color.white : Color.clear;
            }

            RefreshInventory();
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            //如果需要救的玩家死了那么将 savingPlayer 设为零值
            if (correctedSyncVars && !isDead && playerSavingMe)
            {
                playerSavingMe = null;
            }

            SavePlayer();
        }

        protected virtual void AliveLocalUpdate()
        {
            if (PlayerCanControl(this))
            {
                //如果在地面上并且点跳跃
                if (onGround && PlayerControls.Jump(this))
                    Jump();

                if (PlayerControls.Interaction(this))
                    InteractiveWithObject(this);

                if (PlayerControls.HoldingAttack(this))
                    OnHoldAttack();

                if (PlayerControls.ThrowItem(this))
                    ServerThrowItem(usingItemIndex.ToString(), 1);

                //如果按 L
                if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                {
                    //睡眠
                }

                #region 切换物品

                void FuncSwitchItem(Func<Player, bool> funcCall, int ii)
                {
                    if (!funcCall(this))
                        return;

                    SwitchItem(ii - 1);
                }

                FuncSwitchItem(PlayerControls.SwitchToItem1, 1);
                FuncSwitchItem(PlayerControls.SwitchToItem2, 2);
                FuncSwitchItem(PlayerControls.SwitchToItem3, 3);
                FuncSwitchItem(PlayerControls.SwitchToItem4, 4);
                FuncSwitchItem(PlayerControls.SwitchToItem5, 5);
                FuncSwitchItem(PlayerControls.SwitchToItem6, 6);
                FuncSwitchItem(PlayerControls.SwitchToItem7, 7);
                FuncSwitchItem(PlayerControls.SwitchToItem8, 8);

                if (PlayerControls.SwitchToLastItem(this))
                {
                    int value = usingItemIndex - 1;

                    if (value < 0)
                        value = mainInventorySlotCount - 1;

                    SwitchItem(value);
                }
                else if (PlayerControls.SwitchToNextItem(this))
                {
                    int value = usingItemIndex + 1;

                    if (value > mainInventorySlotCount - 1)
                        value = 0;

                    SwitchItem(value);
                }
                #endregion

                //如果按右键
                if (PlayerControls.UseItem(this))
                {
                    UseItem(this);
                }

                #region 改变操控层
                if (nameText)
                {
                    BlockLayer value = PlayerControls.SwitchControllingLayer(this);

                    if (value != controllingLayer)
                    {
                        ChangeControllingLayer(this, value);
                    }
                }
                #endregion
            }

            //血量低于 10 放心跳声, 死了就不播放声音
            if (health <= 15)
            {
                GAudio.Play(AudioID.Heartbeat, true);
            }
        }

        public static Action<Player> GravitySet = caller =>
        {
            if (!caller.correctedSyncVars)
            {
                caller.gravity = 0;
                return;
            }

            if (!caller.generatedFirstSandbox ||
                GM.instance.generatingExistingSandbox ||
                GM.instance.generatingNewSandboxes.Any(p => p == PosConvert.WorldPosToSandboxIndex(caller.transform.position)))  //获取沙盒序号不用 caller.sandboxIndex 是因为只有沙盒加载成功, caller.sandboxIndex才会正式改编
            {
                caller.gravity = 0;
                return;
            }

            caller.gravity = playerDefaultGravity;

            if (!caller.managerGame.generatedExistingSandboxes.Any(p => p.index == caller.sandboxIndex) || caller.isDead)
            {
                caller.gravity *= 0.05f;
            }
        };

        protected virtual void LocalUpdate()
        {
            GravitySet(this);
            rb.gravityScale = gravity;

            if (correctedSyncVars)
            {
                if (!isDead)
                {
                    AliveLocalUpdate();
                }
                if (!isHurting)
                {
                    DeathRotation();
                }

                AutoGenerateSandbox();

                //刷新状态栏
                RefreshPropertiesBar();
            }


            pui?.Update();





            #region 背包
            if (pui != null && pui.inventoryMask && PlayerControls.Backpack(this))
            {
                ShowHideBackpackMaskAndCraft();
            }
            #endregion
        }

        [ServerRpc]
        public void ServerThrowItem(string index, ushort count, NetworkConnection caller = null)
        {
            //获取位于 index 的物品
            var item = inventory.GetItem(index);

            //如果物品不为空
            if (!Item.Null(item))
            {
                //减少物品的数量
                ClientReduceItemCount(index, (ushort)Mathf.Min(item.count, count));

                //从头上吐出物品
                Vector2 pos = head ? head.transform.position : transform.localPosition;
                GM.instance.SummonItem(pos, item.data.id, count, item.customData.ToString());
            }
        }

        public void SwitchItem(int index)
        {
            usingItemIndex = index;

            //播放切换音效
            GAudio.Play(AudioID.SwitchMainInventorySlot);

            //改变状态文本
            string itemName = GameUI.CompareText(TryGetUsingItem()?.data?.id).text;
            if (itemName.IsNullOrWhiteSpace())
                itemName = GameUI.CompareText("ori:empty_item").text;
            SetStatusText(GameUI.CompareText("ori:switch_item").text.Replace("{item_id}", itemName));
        }

        public void SetStatusText(string text)
        {
            if (pui == null)
                return;

            pui.statusText.AfterRefreshing += t => t.text.text = text;
            pui.statusText.RefreshUI();

            //杀死淡出动画
            Tools.KillTweensOf(pui.statusText.text);

            //播放淡入动画
            if (pui.statusText.text.color.a == 1)
                pui.statusText.text.SetAlpha(0);
            GameUI.FadeIn(pui.statusText.text);

            //准备播放淡出动画
            pui.statusTextFadeOutWaitedTime = 0;

            if (!pui.preparingToFadeOutStatusText)
            {
                StartCoroutine(PrepareToFadeOutStatusText());
            }
        }

        IEnumerator PrepareToFadeOutStatusText()
        {
            pui.preparingToFadeOutStatusText = true;

            //等待淡出间隔
            while (pui.statusTextFadeOutWaitedTime < PlayerUI.statusTextFadeOutTime)
            {
                pui.statusTextFadeOutWaitedTime += Performance.frameTime;

                yield return null;
            }

            //杀死淡入动画
            Tools.KillTweensOf(pui.statusText.text);

            pui.statusText.text.SetAlpha(1);
            GameUI.FadeOut(pui.statusText.text);
            pui.preparingToFadeOutStatusText = false;
        }

        public void AutoGenerateSandbox()
        {
            //缓存优化性能
            Vector2Int currentIndex = sandboxIndex;

            if (generatedFirstSandbox && !askingForGeneratingSandbox && askingForGeneratingSandboxTime + 5 <= Tools.time && !GM.instance.generatingExistingSandbox && !GM.instance.generatingNewSandbox)// && TryGetSandbox(out Sandbox sb))
            {
                Vector2Int newIndex = PosConvert.WorldPosToSandboxIndex(transform.position);

                //如果变了
                if (currentIndex != newIndex)
                {
                    //生成沙盒并刷新时间
                    ServerGenerateSandbox(newIndex, false);
                    askingForGeneratingSandboxTime = Tools.time;

                    //生成完后正式切换沙盒序号
                    if (managerGame.generatedExistingSandboxes.Any(p => p.index == newIndex))
                    {
                        sandboxIndex = newIndex;
                    }
                }
            }
        }

        public static readonly Dictionary<string, (Action, Action)> backpackSidebarTable = new();
        public string usingBackpackSidebar;

        public void SetBackpackSidebar(string id)
        {
            if (backpackSidebarTable.TryGetValue(usingBackpackSidebar, out (Action, Action) value))
            {
                value.Item2();
            }

            if (backpackSidebarTable.TryGetValue(id, out value))
            {
                value.Item1();
                usingBackpackSidebar = id;
                return;
            }


            Debug.LogError($"未找到侧边栏 {id}");
        }

        public void ShowHideBackpackMaskAndCraft()
        {
            SetBackpackSidebar("ori:craft");

            ShowHideBackpackMask();
        }

        public void ShowHideBackpackMask()
        {
            if (pui.inventoryMask.gameObject.activeSelf)
            {
                HideBackpackMask();
            }
            else
            {
                ShowBackpackMask();
            }
        }

        public void ShowBackpackMask()
        {
            GameUI.SetPage(pui.inventoryMask);
            OnInventoryItemChange(null);
            GAudio.Play(AudioID.OpenBackpack);
        }

        public void HideBackpackMask()
        {
            ItemInfoShower.Hide();
            ItemDragger.Hide();
            GameUI.SetPage(null);
            GAudio.Play(AudioID.CloseBackpack);
        }

        public static Action<Player> UseItem = caller =>
        {
            var cursorPos = caller.cursorWorldPos;

            foreach (var entity in EntityCenter.all)
            {
                /* --------------------------------- 筛选出 NPC -------------------------------- */
                if (entity is not NPC)
                    continue;

                NPC npc = (NPC)entity;

                /* ------------------------------- 如果在光标互动范围内 ------------------------------- */ //TODO: Fix (问题是可以在任意地方点击到NPC)
                if ((entity.transform.position.x - cursorPos.x).Abs() < npc.interactionSize.x / 2 && (entity.transform.position.y - cursorPos.y).Abs() < npc.interactionSize.y / 2)
                {
                    npc.PlayerInteraction(caller);
                    return;
                }
            }

            //与方块交互
            if (caller.InUseRadius() &&
                caller.map.TryGetBlock(PosConvert.WorldToMapPos(cursorPos), caller.controllingLayer, out Block block) &&
                block.PlayerInteraction(caller))
            {

            }
            //使用物品
            else
            {
                ItemBehaviour usingItemBehaviour = caller.TryGetUsingItemBehaviour();

                if (usingItemBehaviour != null) usingItemBehaviour.Use();
            }
        };

        public static Action<Player, BlockLayer> ChangeControllingLayer = (caller, value) =>
        {
            if (!BlockLayerHelp.InRange(value))
                return;

            caller.controllingLayer = value;

            caller.SetStatusText(GameUI.CompareText("ori:switch_controlling_layer").text.Replace("{layer}", value.ToString()));
            caller.nameText.RefreshUI();
        };


        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }



#if UNITY_EDITOR

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, excavationRadius);
        }

#endif
        #endregion

        #region Base 覆写


        public override void OnDeathServer() { }
        public override void OnDeathClient()
        {
            deathTimer = Tools.time;//TODO: Fix:   + 20;  

            if (isLocalPlayer)
            {
                GAudio.Play(AudioID.Death);
                MethodAgent.CallUntil(() => pui != null, () => pui.ShowRebornPanel());
            }

            anim.KillSequences();
        }

        public override void OnRebornServer()
        {

        }

        public override void OnRebornClient()
        {
            if (isLocalPlayer)
            {
                MethodAgent.CallUntil(() => pui != null, () => pui.rebornPanel.gameObject.SetActive(false));
            }
        }

        public override void OnGetHurtServer() { }

        public override void OnGetHurtClient()
        {
            if (GControls.mode == ControlMode.Gamepad)
                GControls.GamepadVibrationMediumStrong();
        }


        /*[ClientRpc]*/
        public /*protected*/ override void /*Rpc*/SetOrientation(bool right)
        {
            base./*Rpc*/SetOrientation(right);

            if (nameText)
            {
                if (right)
                    nameText.transform.SetScaleXAbs();
                else
                    nameText.transform.SetScaleXNegativeAbs();
            }
        }

        #endregion



#if UNITY_EDITOR
        [Button, ServerRpc] public static void ServerRpcTry(NetworkConnection caller = null) { Debug.Log(MethodGetter.GetCurrentMethodName()); }
        [Button, ClientRpc] public static void ClientRpcTry(NetworkConnection caller = null) { Debug.Log(MethodGetter.GetCurrentMethodName()); }
        [Button, ServerRpc] public static void ConnectionTry(NetworkConnection caller = null) { Debug.Log(MethodGetter.GetCurrentMethodName()); ConnectionOnClientTry(caller); }
        [ConnectionRpc] public static void ConnectionOnClientTry(NetworkConnection caller) { Debug.Log(MethodGetter.GetCurrentMethodName()); }

        [Button, ServerRpc] public static void ServerRpcTryParam1(Player param0, NetworkConnection caller = null) { Debug.Log($"{MethodGetter.GetCurrentMethodName()} :of {param0.playerName}"); }
        [Button, ClientRpc] public static void ClientRpcTryParam1(Player param0, NetworkConnection caller = null) { if (param0 == null) { Debug.LogError("玩家参数为空"); return; } Debug.Log($"{MethodGetter.GetCurrentMethodName()} :of {param0.playerName}"); }
        [Button, ServerRpc] public static void ConnectionTryParam1(Player param0, NetworkConnection caller = null) { Debug.Log($"{MethodGetter.GetCurrentMethodName()} :of {param0.playerName}"); ConnectionOnClientTryParam1(param0, caller); }
        [ConnectionRpc] public static void ConnectionOnClientTryParam1(Player param0, NetworkConnection caller) { Debug.Log($"{MethodGetter.GetCurrentMethodName()} :of {param0.playerName}"); }

        [Button, ServerRpc] public static void ServerRpcTryParam2(string param0, Player param1, NetworkConnection caller = null) { Debug.Log($"{param0} : {MethodGetter.GetCurrentMethodName()} :of {param1.playerName}"); }
        [Button, ClientRpc] public static void ClientRpcTryParam2(string param0, Player param1, NetworkConnection caller = null) { Debug.Log($"{param0} : {MethodGetter.GetCurrentMethodName()} :of {param1.playerName}"); }
        [Button, ServerRpc] public static void ConnectionTryParam2(string param0, Player param1, NetworkConnection caller = null) { Debug.Log($"{param0} : {MethodGetter.GetCurrentMethodName()} :of {param1.playerName}"); ConnectionOnClientTryParam2(param0, param1, caller); }
        [ConnectionRpc] public static void ConnectionOnClientTryParam2(string param0, Player param1, NetworkConnection caller) { Debug.Log($"{param0} : {MethodGetter.GetCurrentMethodName()} :of {param1.playerName}"); }

        [Button, ServerRpc] public static void ServerRpcTryParam3(string param0, int param1, Player param2, NetworkConnection caller = null) { Debug.Log($"{param0} : {param1.ToString()} : {MethodGetter.GetCurrentMethodName()} :of {param2.playerName}"); }
        [Button, ClientRpc] public static void ClientRpcTryParam3(string param0, int param1, Player param2, NetworkConnection caller = null) { Debug.Log($"{param0} : {param1.ToString()} : {MethodGetter.GetCurrentMethodName()}:of {param2.playerName}"); }
        [Button, ServerRpc] public static void ConnectionTryParam3(string param0, int param1, Player param2, NetworkConnection caller = null) { Debug.Log($"{param0} : {param1.ToString()} : {MethodGetter.GetCurrentMethodName()}:of {param2.playerName}"); ConnectionOnClientTryParam3(param0, param1, param2, caller); }
        [ConnectionRpc] public static void ConnectionOnClientTryParam3(string param0, int param1, Player param2, NetworkConnection caller) { Debug.Log($"{param0} : {param1.ToString()} : {MethodGetter.GetCurrentMethodName()}:of {param2.playerName}"); }
#endif



        public void OnNameChange(string newValue)
        {
            //销毁原先的 nameText
            if (nameText && nameText.gameObject)
                Destroy(nameText.gameObject);

            //初始化新的 nameText
            nameText = GameUI.AddText(UPC.middle, "ori:player_name_" + newValue, playerCanvas);
            nameText.rectTransform.AddLocalPosY(35);
            nameText.text.SetFontSize(10);
            nameText.AfterRefreshing += n =>
            {
                string text = isLocalPlayer ? $"{newValue}:{controllingLayer}" : $"{newValue}";

                n.text.text = text;
            };
        }

        public void RefreshInventory()
        {
            if (usingItemRenderer && head && body && leftArm && rightArm && leftLeg && rightLeg && leftFoot && rightFoot)
            {
                /* --------------------------------- 刷新手部物品 --------------------------------- */
                usingItemRenderer.sprite = TryGetUsingItem()?.data?.texture?.sprite;

                /* --------------------------------- 刷新头盔的贴图 -------------------------------- */
                if (Item.IsHelmet(inventory.helmet))
                {
                    head.armorSr.sprite = inventory.helmet.data.Helmet.head?.sprite;
                }
                else
                {
                    head.armorSr.sprite = null;
                }

                /* --------------------------------- 刷新胸甲的贴图 -------------------------------- */
                if (Item.IsBreastplate(inventory.breastplate))
                {
                    body.armorSr.sprite = inventory.breastplate.data.Breastplate.body?.sprite;
                    leftArm.armorSr.sprite = inventory.breastplate.data.Breastplate.leftArm?.sprite;
                    rightArm.armorSr.sprite = inventory.breastplate.data.Breastplate.rightArm?.sprite;
                }
                else
                {
                    body.armorSr.sprite = null;
                    leftArm.armorSr.sprite = null;
                    rightArm.armorSr.sprite = null;
                }

                /* --------------------------------- 刷新护腿的贴图 -------------------------------- */
                if (Item.IsLegging(inventory.legging))
                {
                    leftLeg.armorSr.sprite = inventory.legging.data.Legging.leftLeg?.sprite;
                    rightLeg.armorSr.sprite = inventory.legging.data.Legging.rightLeg?.sprite;
                }
                else
                {
                    leftLeg.armorSr.sprite = null;
                    rightLeg.armorSr.sprite = null;
                }

                /* --------------------------------- 刷新鞋子的贴图 -------------------------------- */
                if (Item.IsBoots(inventory.boots))
                {
                    leftFoot.armorSr.sprite = inventory.boots.data.Boots.leftFoot?.sprite;
                    rightFoot.armorSr.sprite = inventory.boots.data.Boots.rightFoot?.sprite;
                }
                else
                {
                    leftFoot.armorSr.sprite = null;
                    rightFoot.armorSr.sprite = null;
                }
            }

            RefreshMainInventorySlots();
        }

        public void RefreshMainInventorySlots()
        {
            //只有本地玩家有物品栏
            if (!isLocalPlayer || pui == null)
                return;

            Internal_RefreshInventorySlot(pui.mainInventorySlots);
        }

        void Internal_RefreshInventorySlot(InventorySlotUI[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var val = slots.ElementAt(i);

                if (val == null)
                    return;

                var itemInMainInventorySlots = inventory?.TryGetItem(i);

                //设置物品图标
                val.content.image.sprite = itemInMainInventorySlots?.data?.texture?.sprite;
                val.content.image.color = new(val.content.image.color.r, val.content.image.color.g, val.content.image.color.b, !val.content.image.sprite ? 0 : 1);

                //设置文本
                val.button.buttonText.text.text = itemInMainInventorySlots?.count.ToString();

                //设置栏位图标
                if (usingItemIndex == i)
                    val.button.image.sprite = ModFactory.CompareTexture("ori:using_item_tab")?.sprite;
                else
                    val.button.image.sprite = ModFactory.CompareTexture("ori:item_tab")?.sprite;
            }
        }

        public void OnHoldAttack()
        {
            if (!isLocalPlayer || isDead)
                return;

            //如果还在 攻击CD 就返回
            if (useTime + (TryGetUsingItem()?.data?.useCD ?? ItemData.defaultUseCD) > Tools.time)
            {
                return;
            }

            //如果 鼠标在挖掘范围内 && 在鼠标位置获取到方块 && 方块是激活的
            if (InUseRadius() && map.TryGetBlock(PosConvert.WorldToMapPos(cursorWorldPos), controllingLayer, out Block block) && block.gameObject.activeInHierarchy)
            {
                ExcavateBlock(block);
            }
            else
            {
                OnStartAttack();
            }

            //设置时间
            useTime = Tools.time;
        }

        /// <returns>鼠标是否在使用范围内</returns>
        public bool InUseRadius() => InUseRadius(cursorWorldPos);

        public bool InUseRadius(Vector2 vec) => InUseRadius(transform.position, vec);

        public bool InUseRadius(Vector2 vec1, Vector2 vec2) => Vector2.Distance(vec1, vec2) <= excavationRadius + (TryGetUsingItem()?.data?.extraDistance ?? 0);

        #region 挖掘方块
        private void ExcavateBlock(Block block)
        {
            if (!isLocalPlayer || !block)
                return;

            if (block)
            {
                anim.SetAnim("excavate_rightarm");

                block.TakeDamage(excavationStrength);

                GAudio.Play(AudioID.ExcavatingBlock);

                if (GControls.mode == ControlMode.Gamepad)
                    GControls.GamepadVibrationSlighter(0.1f);
            }

            useTime = Tools.time;
        }
        #endregion

        #region 配置检查
        //[Command]
        //private void CmdCheckMods() => MethodAgent.RunThread(() => ZipTools.Zip(Tools.modsPath, GameFiles.currentWorld.worldPath + "/temp_mods.zip", new(b =>
        //{
        //    string path = GameFiles.currentWorld.worldPath + "/temp_mods.zip";
        //    TargetCheckMods(connectionToClient, Compressor.CompressString(JsonTools.ToJson(IOTools.FileToBytes(path))));
        //    File.Delete(path);
        //})));


        //[TargetRpc]
        //public void TargetCheckMods(NetworkConnection conn, string serverModJson) => MethodAgent.RunThread(() => MethodAgent.TryRun(() =>
        //{
        //    string path = GameFiles.currentWorld.worldPath + "/temp_mods";
        //    IOTools.CreateDirectoryIfNone(path);

        //    ZipTools.UnzipFile(ByteConverter.ToMemoryStream(JsonTools.LoadJsonByString<byte[]>(Compressor.DecompressString(serverModJson))), path, new(b =>
        //    {
        //        string[] modPaths = IOTools.GetFoldersInFolder(path + "/mods", true).Where(p => File.Exists(p + "/mod_info.json")).ToArray();
        //        Dictionary<string, ModDatum_Info> differentMods = new();
        //        List<string> compareModPaths = new();

        //        foreach (var item in modPaths)
        //        {
        //            string infoPath = item + "/mod_info.json";

        //            differentMods.Add(item, JsonTools.LoadJson<ModDatum_Info>(infoPath));
        //        }

        //        for (int i = 0; i < differentMods.Count; i++)
        //        {
        //            var inf = differentMods.ElementAt(i);

        //            foreach (var item in ModFactory.mods)
        //            {
        //                if (item.info.id == inf.Value.id && item.info.version == inf.Value.version)
        //                {
        //                    differentMods.Remove(inf.Key);
        //                    compareModPaths.Add(inf.Key);
        //                }
        //            }
        //        }

        //        if (differentMods.Count > 0)
        //        {
        //            Debug.Log("与服务器模组不同步, 请重进游戏");

        //            foreach (var item in differentMods)
        //                Debug.LogError("不同步的模组: " + item.Value.id);

        //            foreach (var item in compareModPaths)
        //            {
        //                Directory.Delete(item);
        //            }

        //            Tools.MoveFolder(path + "/mods", Tools.modsPath);

        //            Tools.Restart();
        //        }
        //    }));
        //}, true));
        #endregion


        #region 生成沙盒

        //告诉服务器要生成, 并让服务器生成 (隐性), 然后在生成好后传给客户端
        [Button]
        public void ServerGenerateSandbox(Vector2Int index, bool isFirstGeneration, string biomeId = null)
        {
            askingForGeneratingSandbox = true;

            ServerGenerateSandboxCore(index, isFirstGeneration, biomeId);
        }

        //告诉服务器要生成, 并让服务器生成 (隐性), 然后在生成好后传给客户端
        [ServerRpc]
        private void ServerGenerateSandboxCore(Vector2Int index, bool isFirstGeneration, string biomeId, NetworkConnection caller = null)
        {
            MethodAgent.TryRun(() =>
            {
                Debug.Log($"Player={netId} 请求生成沙盒 {index}");

                List<Vector2Int> indexes = new()
            {
                //: x
                //: x
                //: x 
                index + new Vector2Int(-1, -1),
                index + new Vector2Int(0, -1),
                index + new Vector2Int(1, -1),

                //:   x
                //:   x
                //:   x
                index + new Vector2Int(-1, 0),
                index,
                index + new Vector2Int(1, 0),

                //:     x
                //:     x
                //:     x
                index + new Vector2Int(-1, 1),
                index + new Vector2Int(0, 1),
                index + new Vector2Int(1, 1),
            };

                //如果有直接让服务器生成
                foreach (Vector2Int currentIndex in indexes)
                {
                    //如果没有则生成新的沙盒
                    MethodAgent.RunThread(() =>
                    {
                        GM.instance.GenerateNewSandbox(currentIndex, biomeId);

                        MethodAgent.RunOnMainThread(() =>
                        {
                            //如果 index 是中心, 就是首次生成
                            bool isGenerationMiddle = currentIndex == index;
                            bool isFirstGenerationMiddle = isFirstGeneration && isGenerationMiddle;

                            if (isGenerationMiddle)
                            {
                                foreach (Sandbox sb in GFiles.world.sandboxData)
                                {
                                    if (sb.index == currentIndex && sb.generatedAlready)
                                    {
                                        //* 如果是客户端发送的申请: 服务器生成->客户端生成   (如果 服务器和客户端 并行生成, 可能会导致 bug)
                                        if (!isLocalPlayer)
                                            GM.instance.GenerateExistingSandbox(
                                                sb,
                                                () => ConnectionGenerateSandbox(sb, isFirstGenerationMiddle, caller),
                                                () => ConnectionGenerateSandbox(sb, isFirstGenerationMiddle, caller),
                                                (ushort)(GFiles.settings.performanceLevel / 2)
                                            );
                                        //* 如果是服务器发送的申请: 服务器生成
                                        else
                                            ConnectionGenerateSandbox(sb, isFirstGenerationMiddle, caller);

                                        break;
                                    }
                                }
                            }
                        });
                    });
                }
            }, true);
        }

        [ConnectionRpc]
        void ConnectionGenerateSandbox(Sandbox sandbox, bool isFirstGeneration, NetworkConnection caller)
        {
            MethodAgent.TryRun(() =>
            {
                Debug.Log($"收到服务器回调, 正在生成已有沙盒 {sandbox.index}");

                if (isFirstGeneration)
                {
                    //防止因跑出沙盒导致重复生成
                    transform.position = Sandbox.GetMiddle(sandbox.index);
                }

                GM.instance.GenerateExistingSandbox(sandbox, () =>
                {
                    //将玩家的位置恢复到出生点
                    if (isFirstGeneration)
                    {
                        transform.position = sandbox.spawnPoint.To2();
                        generatedFirstSandbox = true;
                    }
                    //下面的参数: 如果是 首次中心生成 就快一点, 否则慢一些防止卡顿
                }, null, (ushort)(GFiles.settings.performanceLevel * (isFirstGeneration ? 3 : 0.8f)));

                askingForGeneratingSandbox = false;
            }, true);
        }

        #endregion


        #region 玩家数据加载
        void LoadPlayerDatumFromFile()
        {
            if (!isLocalPlayer)
            {
                Debug.LogError("必须由客户端申请加载数据");
                return;
            }

            //加载玩家在文件中的数据
            ServerLoadOrCreatePlayerDatum(GFiles.settings.playerName);
        }

        public const string defaultName = "Alan";

        [ServerRpc]
        void ServerLoadOrCreatePlayerDatum(string playerName, NetworkConnection caller = null)
        {
            if (playerName.IsNullOrWhiteSpace())
            {
                playerName = defaultName;
                Debug.Log($"{nameof(playerName)} 为空, 改为了 {defaultName}");
            }

            for (int i = 0; i < GFiles.world.playerData.Count; i++)
            {
                PlayerData data = GFiles.world.playerData[i];

                if (data.playerName == playerName)
                {
                    //加载服务器存档中的数据
                    this.playerName = playerName;
                    sandboxIndex = data.currentSandbox;

                    string oldInventory = JsonTools.ToJson(data.inventory);

                    //恢复物品栏
                    inventory = data.inventory;
                    inventory.owner = this;
                    inventory.SetSlotCount(inventorySlotCount);
                    inventory.ResumeFromNetwork();

                    hungerValue = data.hungerValue;
                    thirstValue = data.thirstValue;
                    happinessValue = data.happinessValue;
                    health = data.health;
                    completedTasks = data.completedTasks;

                    goto finish;
                }
            }

            //如果没找到就新建
            Debug.LogWarning($"未找到匹配的玩家信息 ({playerName})");

            //初始化新的临时数据
            this.playerName = playerName;
            sandboxIndex = Vector2Int.zero;
            inventory = new(inventorySlotCount, this);
            hungerValue = defaultHungerValue;
            thirstValue = defaultThirstValue;
            happinessValue = defaultHappinessValue;
            health = defaultHealth;
            completedTasks = new();


        finish:
            //在服务器存档中添加玩家存档
            ServerSavePlayerDatumToFile();

            MethodAgent.CallNextFrame(() =>
            {
                CallerLoadOrCreatePlayerDatum(caller);
            });
        }

        [ConnectionRpc]
        async void CallerLoadOrCreatePlayerDatum(NetworkConnection caller)
        {
            await UniTask.WaitWhile(() => completedTasks == null);

            pui = new(this);
            
            ServerGenerateSandbox(sandboxIndex, true);
        }

        /// <summary>
        /// 检查玩家数据中有没有指定的玩家名的数据, 如果没有生成一个新的并添加, 否则覆写
        /// </summary>
        [ServerRpc]
        void ServerSavePlayerDatumToFile(NetworkConnection caller = null)
        {
            //初始化新的玩家数据
            PlayerData newPlayerData = new()
            {
                playerName = this.playerName.IsNullOrWhiteSpace() ? defaultName : this.playerName,
                currentSandbox = sandboxIndex,
                inventory = inventory,
                hungerValue = hungerValue,
                thirstValue = thirstValue,
                happinessValue = happinessValue,
                health = health,
                completedTasks = completedTasks,
            };

            //如果检测到已经存在就覆写
            for (int i = 0; i < GFiles.world.playerData.Count; i++)
            {
                if (GFiles.world.playerData[i].playerName == newPlayerData.playerName)
                {
                    GFiles.world.playerData[i] = newPlayerData;
                    return;
                }
            }

            //如果不存在就添加
            GFiles.world.playerData.Add(newPlayerData);
            Debug.LogWarning($"未在存档中匹配到玩家 {(this.playerName.IsNullOrWhiteSpace() ? defaultName : this.playerName)} ({this.playerName}), 已自动添加");

            //保存世界
            GFiles.SaveAllDataToFiles();
        }
        #endregion

        [HideInInspector] public Collider2D[] interactionObjectsDetectedTemp = new Collider2D[100];

        #region 玩家行为
        #region 交互
        public static Action<Player> InteractiveWithObject = caller =>
        {
            if (!caller.isLocalPlayer || caller.isDead)
                return;

            Physics2D.OverlapCircleNonAlloc(caller.transform.position, Player.interactionRadius, caller.interactionObjectsDetectedTemp);
            IPlayerInteraction interaction = null;

            foreach (var obj in caller.interactionObjectsDetectedTemp)
            {
                if (!obj)
                {
                    break;
                }

                if (obj.TryGetComponent(out interaction))
                {
                    interaction.Interactive(caller);
                }
            }
        };



        [ServerRpc]
        public void ServerAddItem(Item item, NetworkConnection caller = null)
        {
            ClientAddItem(item);
        }

        [ClientRpc]
        public void ClientAddItem(Item item, NetworkConnection caller = null)
        {
            Item.StreamResume(ref item);

            inventory.AddItem(item);
        }



        [ServerRpc]
        public void ServerSetItem(string index, Item item, NetworkConnection caller = null)
        {
            ClientSetItem(index, item);
        }

        [ClientRpc]
        public void ClientSetItem(string index, Item item, NetworkConnection caller = null)
        {
            if (Item.Null(item))
            {
                item = null;
            }
            else
            {
                Item.StreamResume(ref item);
            }

            inventory.SetItem(index, item);
        }



        [ServerRpc]
        public void ServerSwapItemOnHand(string index, NetworkConnection caller = null)
        {
            ClientSwapItem(usingItemIndex.ToString(), index);
        }

        [ServerRpc]
        public void ServerSwapItem(string index1, string index2, NetworkConnection caller = null)
        {
            ClientSwapItem(index1, index2);
        }

        [ClientRpc]
        public void ClientSwapItem(string index1, string index2, NetworkConnection caller = null) => inventory.SwapItem(index1, index2);



        [ServerRpc]
        public void ServerReduceItemCount(string index, ushort count, NetworkConnection caller = null)
        {
            ClientReduceItemCount(index, count);
        }

        [ClientRpc]
        public void ClientReduceItemCount(string index, ushort count, NetworkConnection caller = null) => inventory.ReduceItemCount(index, count);
        #endregion

        #region 移动和转向
        public override void Movement()
        {
            if (!correctedSyncVars)
                return;

            float move;

            if (!CanMove(this) || !PlayerCanControl(this))
                move = 0;
            else
                move = PlayerControls.Move(this);

            if (isLocalPlayer && !isDead)
            {
                //设置速度
                rb.velocity = GetMovementVelocity(new(move, 0));

                //执行 移动的启停
                if (move == 0 && moveVecLastFrame != 0)
                {
                    OnStopMovement();
                }
                else if (move != 0 && moveVecLastFrame == 0)
                {
                    OnStartMovement();
                }
            }

            if (isServer)
            {
                if (isDead)
                    isMoving = false;
            }

            //用于在下一帧检测是不是刚刚停止或开始移动
            moveVecLastFrame = move;
        }

        public Action<Player> AutoSetPlayerOrientation = (p) =>
        {
            if (!p.isLocalPlayer || !p.correctedSyncVars)
                return;

            ////诸如 && transform.localScale.x.Sign() == 1 之类的检测是为了减缓服务器压力
            if (p.isDead)
            {
                p.SetOrientation(true);
                return;
            }

            switch (GControls.mode)
            {
                //* 如果是键鼠, 则检测鼠标和玩家的相对位置
                case ControlMode.KeyboardAndMouse:
                    float delta = GControls.mousePos.ToWorldPos().x - p.transform.position.x;

                    if (delta < 0)
                        p.SetOrientation(false);
                    else if (delta > 0)
                        p.SetOrientation(true);

                    break;

                //* 如果是触摸屏, 则检测光标和玩家的相对位置
                case ControlMode.Touchscreen:
                    if (p.pui != null && p.pui.moveJoystick && p.pui.cursorJoystick)
                    {
                        if (p.pui.cursorImage.rt.localPosition.x < p.transform.position.x)
                            p.SetOrientation(false);
                        else if (p.pui.cursorImage.rt.localPosition.x > p.transform.position.x)
                            p.SetOrientation(true);
                    }
                    break;

                //* 如果是手柄, 则检测左摇杆
                case ControlMode.Gamepad:
                    float x = PlayerControls.Move(p);

                    if (x < 0)
                        p.SetOrientation(false);
                    else if (x > 0)
                        p.SetOrientation(true);

                    break;
            }
        };
        #endregion

        #region 攻击
        public Vector2 cursorWorldPos
        {
            get
            {
                return GControls.mode switch
                {
                    //* 如果是触摸屏, 返回光标位置
                    ControlMode.Touchscreen => pui.cursorImage.rectTransform.anchoredPosition,

                    //* 如果是键鼠, 返回鼠标位置
                    ControlMode.KeyboardAndMouse => Tools.instance.GetMouseWorldPos(),

                    //* 如果是手柄, 返回虚拟光标位置
                    ControlMode.Gamepad => (Vector2)Tools.instance.mainCamera.ScreenToWorldPoint(VirtualCursor.instance.image.ap),

                    _ => Vector2.zero,
                };
            }
        }

        protected override void OnStartAttack() => OnStartAttack(false, true);

        protected void OnStartAttack(bool left, bool right)
        {
            if (!isLocalPlayer || isDead)
                return;

            base.OnStartAttack();

            //向光标位置发射射线
            if (RayTools.TryHitAll(transform.position, Tools.GetAngleVector2(transform.position, cursorWorldPos), excavationRadius, out var rays))
            {
                foreach (var ray in rays)
                {
                    //排除自己
                    if (ray.transform.GetInstanceID() == transform.GetInstanceID())
                        continue;

                    //这里的作用是获得攻击目标  [先尝试获取 Creature, 再尝试获取 CreatureBodyPart]
                    if (!ray.transform.TryGetComponent<Creature>(out var target) || target.hurtable)
                    {
                        if (ray.transform.TryGetComponent<CreatureBodyPart>(out var cbp) && target.hurtable)
                        {
                            target = cbp.mainBody;
                        }
                    }

                    //如果成功获取了目标
                    if (target)
                    {
                        float damage = TryGetUsingItem()?.data?.damage ?? ItemData.defaultDamage;
                        target.TakeDamage(damage, 0.3f, transform.position, transform.position.x < target.transform.position.x ? Vector2.right * 12 : Vector2.left * 12);

                        //如果使用手柄就震动一下
                        if (GControls.mode == ControlMode.Gamepad)
                            GControls.GamepadVibrationMedium();

                        break;
                    }
                }
            }

            useTime = Tools.time;
            ServerOnStartAttack(left, right);
        }

        [ServerRpc]
        public void ServerOnStartAttack(bool leftArm, bool rightArm, NetworkConnection caller = null)
        {
            ClientStartAttack(leftArm, rightArm);
        }

        [ClientRpc]
        public void ClientStartAttack(bool leftArm, bool rightArm, NetworkConnection caller = null)
        {
            if (leftArm)
                anim.SetAnim("attack_leftarm");
            if (rightArm)
                anim.SetAnim("attack_rightarm");

            GAudio.Play("ori:stick_attack");
        }

        #endregion

        #region 死亡
        private void DeathColor()
        {
            if (isDead)
            {
                if (playerSavingMe)
                {
                    //根据救援进度的不同, 结果也会不同
                    float progress = playerSavingMe.savingTime / enoughSavingTime;

                    //result 决定显示的颜色   [若最 lowest 为 0.45, 则运算为   0.45+(1-0.45)pro = 0.45+0.55pro   当 pro 为最大值 1 时, result 也为最大值 1, 即颜色和救起来后一致]
                    float result = deathLowestColorFloat + oneMinusDeathLowestColorFloat * progress;

                    //设置颜色
                    foreach (var sr in spriteRenderers)
                    {
                        sr.color = new(result, result, result);
                    }
                }
                else
                {
                    //设置颜色
                    foreach (var sr in spriteRenderers)
                    {
                        sr.color = deathLowestColor;
                    }
                }
            }
        }

        private void DeathRotation()
        {
            if (isDead)
            {
                //Debug.Log("Death");
                transform.localRotation = deathQuaternion;
            }
            else
            {
                transform.localRotation = Quaternion.identity;
            }
        }
        #endregion

        #region 救援
        private void SavePlayer()
        {
            if (!correctedSyncVars || !savingPlayer)
                return;

            if (savingPlayer.playerSavingMe != this)
            {
                savingPlayer = null;
                return;
            }

            //如果死了取消救援
            if (isDead)
            {
                savingTime = 0;
                savingPlayer = null;
                return;
            }

            //增加累计救援时间
            savingTime += Performance.frameTime;

            //检测是否达到救援时间
            if (savingTime >= enoughSavingTime)
            {
                savingTime = 0;
                savingPlayer.Reborn(10, transform.position);
            }

            //如果需要救的玩家死了那么将 savingPlayer 设为零值
            if (!savingPlayer.isDead)
            {
                savingPlayer = null;
            }
        }
        #endregion

        #region 消耗

        public void RefreshPropertiesBar()
        {
            if (pui == null)
                return;

            if (pui.thirstBarFull)
                pui.thirstBarFull.image.fillAmount = thirstValue / maxThirstValue;

            if (pui.hungerBarFull)
                pui.hungerBarFull.image.fillAmount = hungerValue / maxHungerValue;

            if (pui.happinessBarFull)
                pui.happinessBarFull.image.fillAmount = happinessValue / maxHappinessValue;

            if (pui.healthBarFull)
                pui.healthBarFull.image.fillAmount = health / maxHealth;
        }

        #endregion
        #endregion





        /* -------------------------------------------------------------------------- */
        /*                                    静态方法                                    */
        /* -------------------------------------------------------------------------- */
        public static Dictionary<CraftingRecipe, List<Dictionary<int, ushort>>> GetCraftingRecipesThatCanBeCrafted(Item[] items)
        {
            Dictionary<CraftingRecipe, List<Dictionary<int, ushort>>> results = new();

            ModFactory.mods.For(mod => mod.craftingRecipes.For(cr =>
            {
                //如果全部原料都可以匹配就添加
                if (WhetherCanBeCrafted(cr, items, out var stuff))
                {
                    results.Add(cr, stuff);
                }
            }));

            return results;
        }

        public static bool WhetherCanBeCrafted<TItem>(Recipe<TItem> recipe, Item[] items, out List<Dictionary<int, ushort>> comparedStuff) where TItem : RecipeItem<TItem>
        {
            if (recipe == null)
            {
                comparedStuff = null;
                return false;
            }

            comparedStuff = new();

            foreach (var crItem in recipe.items)
            {
                ushort comparedCount = 0;
                Dictionary<int, ushort> itemsToUse = new();

                for (int i = 0; i < items.Length; i++)
                {
                    if (comparedCount == crItem.count)
                        break;

                    var bpItem = items[i];

                    //如果物品为空则返回
                    if (Item.Null(bpItem))
                        continue;

                    //如果 ID 匹配则添加匹配数
                    if (crItem.id == bpItem.data.id)
                    {
                        ushort count = Convert.ToUInt16(Mathf.Min(bpItem.count, crItem.count - comparedCount));
                        comparedCount += count;
                        itemsToUse.Add(i, count);

                        continue;
                    }
                    //如果 标签 匹配也添加匹配数
                    else if (crItem.tags.Count > 0)
                    {
                        foreach (var crItemTag in crItem.tags)
                        {
                            foreach (var bpItemTag in bpItem.data.tags)
                            {
                                //如果标签一致
                                if (bpItemTag == crItemTag)
                                {
                                    ushort count = Convert.ToUInt16(Mathf.Min(bpItem.count, crItem.count - comparedCount));
                                    comparedCount += count;
                                    itemsToUse.Add(i, count);

                                    goto nextItem;
                                }
                            }
                        }
                    }

                nextItem: continue;
                }

                //如果匹配到的合格物品数量超过要求, 例如
                // * 一个配方: 5鸡羽毛
                // * 背包中: 1.一个, 两个, 三个   2.五个   3.三个, 一个, 九个
                // * 无论是哪一种, 都可以合成, 因为 comparedCount 会 >= 5
                if (comparedCount == crItem.count)
                {
                    comparedStuff.Add(itemsToUse);
                }
            }

            //如果全部原料都可以匹配就添加
            return comparedStuff.Count >= recipe.items.Count;
        }

        [RuntimeInitializeOnLoadMethod]
        private async static void BindMethods()
        {
            NetworkCallbacks.OnTimeToServerCallback += () =>
            {

            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {

            };

            await UniTask.WaitUntil(() => Block.blockLayerMask != 0);

            playerLayer = LayerMask.NameToLayer("Player");
            playerLayerMask = playerLayer.LayerMaskOnly();
            playerOnGroundLayerMask = Block.blockLayerMask;



            GM.OnUpdate += PlayerCenter.Update;
        }

        public static Player local => ManagerNetwork.instance.localPlayer;

        public static bool GetLocal(out Player p)
        {
            p = local;

            return p;
        }
    }





    /* -------------------------------------------------------------------------- */
    /*                                    接口/拓展                                   */
    /* -------------------------------------------------------------------------- */
    public sealed class TempItemContainer : IItemContainer
    {
        public Item[] items { get; set; }

        public Item GetItem(string index) => items[Convert.ToInt32(index)];
        public void SetItem(string index, Item value) => items[Convert.ToInt32(index)] = value;
    }

    public interface IItemContainer
    {
        Item[] items { get; set; }

        Item GetItem(string index);
        void SetItem(string index, Item value);
    }

    public static class ItemContainerExtensions
    {
        public static void LoadItemsFromCustomData(this IItemContainer container, JObject jo, int defaultItemCount)
        {
            /* -------------------------------------------------------------------------- */
            /*                                //修正 JObject                                */
            /* -------------------------------------------------------------------------- */
            jo ??= new();

            if (jo["ori:container"] == null)
                jo.AddObject("ori:container");
            if (jo["ori:container"]["items"] == null)
                jo["ori:container"].AddObject("items");
            if (jo["ori:container"]["items"]["array"] == null)
            {
                JToken[] tokens = new JToken[defaultItemCount];
                jo["ori:container"]["items"].AddArray("array", tokens);
            }

            /* -------------------------------------------------------------------------- */
            /*                                    缓存数据                                    */
            /* -------------------------------------------------------------------------- */
            var array = (JArray)jo["ori:container"]["items"]["array"];

            /* -------------------------------------------------------------------------- */
            /*                                    读取数据                                    */
            /* -------------------------------------------------------------------------- */
            if (array.Count != 0)
            {
                container.items = array.ToObject<Item[]>();

                for (int i = 0; i < container.items.Length; i++)
                {
                    Item.StreamResume(ref container.items[i]);
                }
            }
            else
            {
                container.items = new Item[defaultItemCount];
            }
        }

        public static void WriteItemsToCustomData(this IItemContainer container, JObject jo)
        {
            var array = (JArray)jo["ori:container"]["items"]["array"];

            //清除数据
            array.Clear();

            //写入数据
            foreach (var item in container.items)
            {
                if (Item.Null(item))
                {
                    array.Add(null);
                    continue;
                }

                array.Add(JToken.FromObject(item));
            }
        }
    }

    public static class EntityBehaviourExtensions
    {
        public static void LoadInventoryFromCustomData<T>(this T entity, byte slotCount) where T : Entity, IInventoryOwner
        {
            /* -------------------------------------------------------------------------- */
            /*                                //修正 JObject                                */
            /* -------------------------------------------------------------------------- */
            entity.customData ??= new();

            if (entity.customData["ori:inventory"] == null)
                entity.customData.AddObject("ori:inventory");
            if (entity.customData["ori:inventory"]["data"] == null)
            {
                entity.customData["ori:inventory"].AddProperty("data", JsonTools.ToJToken(new Inventory(slotCount, entity)));
            }

            /* -------------------------------------------------------------------------- */
            /*                                    缓存数据                                    */
            /* -------------------------------------------------------------------------- */
            var data = entity.customData["ori:inventory"]["data"];

            /* -------------------------------------------------------------------------- */
            /*                                    读取数据                                    */
            /* -------------------------------------------------------------------------- */
            Inventory inventory = data.ToObject<Inventory>();
            inventory.Init(slotCount, entity);
            inventory.ResumeFromNetwork();
            entity.SetInventory(inventory);
        }

        public static void WriteInventoryToCustomData<T>(this T entity) where T : Entity, IInventoryOwner
        {
            entity.customData["ori:inventory"]["data"] = JsonTools.ToJson(entity.GetInventory());
        }
    }

    public static class ItemDragger
    {
        public class ItemDraggerUI
        {
            public ImageIdentity image;

            public ItemDraggerUI(ImageIdentity image)
            {
                this.image = image;
            }
        }

        public class ItemDraggerItem
        {
            public Item item;
            public Action<Item> placement;
            public Action cancel;

            public ItemDraggerItem(Item item, Action<Item> placement, Action cancel)
            {
                this.item = item;
                this.placement = placement;
                this.cancel = cancel;
            }
        }

        public static ItemDraggerItem draggingItem;

        private static ItemDraggerUI uiInstance;

        public static void DoneSwap(ItemDraggerItem oldDragger, Item item, Action<Item> placement, Action cancel)
        {
            var draggingTemp = oldDragger.item;
            var oldTemp = item;

            /* ------------------------------- 如果物品不同直接交换 ------------------------------- */
            if (Item.Null(draggingTemp) || Item.Null(oldTemp) || !Item.Same(draggingTemp, oldTemp))
            {
                oldDragger.placement(oldTemp);
                placement(draggingTemp);

                ItemDragger.Hide();
            }
            /* ------------------------------- 如果物品相同且数量未满 ------------------------------- */
            else if (oldTemp.count < oldTemp.data.maxCount)
            {
                //如果可以数量直接添加
                if (draggingTemp.count + oldTemp.count <= oldTemp.data.maxCount)
                {
                    oldTemp.count += draggingTemp.count;

                    placement(oldTemp);
                    oldDragger.placement(null);

                    ItemDragger.Hide();
                }
                else
                {
                    //如果数量过多先添加
                    ushort countToExe = (ushort)Mathf.Min(draggingTemp.count, oldTemp.data.maxCount - draggingTemp.count);

                    draggingTemp.count -= countToExe;
                    oldTemp.count += countToExe;

                    placement(oldTemp);   //把容器2物品设为容器1的
                    oldDragger.placement(draggingTemp);   //把容器1物品设为容器2的
                }
            }
        }

        public static ItemDraggerUI GetUI()
        {
            if (uiInstance == null || !uiInstance.image)
            {
                ImageIdentity image = GameUI.AddImage(UPC.middle, "ori:image.item_dragger", "ori:square_button_flat");
                image.OnUpdate += i =>
                {
                    i.ap = GControls.cursorPosInMainCanvas;
                };

                image.image.raycastTarget = false;

                uiInstance = new(image);
            }

            return uiInstance;
        }

        public static void Drag(Item item, Vector2 iconSize, Action<Item> placement, Action onCancel)
        {
            ItemDraggerUI ui = GetUI();

            /* ------------------------------- 先去掉原本在拖拽的物品 ------------------------------- */
            if (draggingItem != null)
            {
                if (ItemDragger.draggingItem.item == item)
                {
                    ItemDragger.Hide();
                    return;
                }
                else
                {
                    ItemDragger.DoneSwap(ItemDragger.draggingItem, item, placement, onCancel);
                    return;
                }
            }

            /* ------------------------------- 如果物品不为空就拖拽 ------------------------------- */
            if (!Item.Null(item))
            {
                ui.image.image.sprite = item.data.texture.sprite;
                ui.image.sd = iconSize;

                ui.image.gameObject.SetActive(true);
                draggingItem = new(item, placement, onCancel);
            }
            /* ------------------------------- 如果物品为空就不拖拽 ------------------------------- */
            else
            {
                Hide();
            }
        }

        public static void Hide()
        {
            ItemDraggerUI ui = GetUI();
            ui.image.gameObject.SetActive(false);

            if (draggingItem != null)
            {
                draggingItem.cancel();

                draggingItem = null;
            }
        }
    }

    public class PlayerSkin
    {
        public string name { get; private set; }
        public string path { get; private set; }
        public string headPath { get; private set; }
        public string bodyPath { get; private set; }
        public string leftArmPath { get; private set; }
        public string rightArmPath { get; private set; }
        public string leftLegPath { get; private set; }
        public string rightLegPath { get; private set; }
        public string leftFootPath { get; private set; }
        public string rightFootPath { get; private set; }

        public Sprite head;
        public Sprite body;
        public Sprite leftArm;
        public Sprite rightArm;
        public Sprite leftLeg;
        public Sprite rightLeg;
        public Sprite leftFoot;
        public Sprite rightFoot;

        public static Sprite skinHead;
        public static Sprite skinBody;
        public static Sprite skinLeftArm;
        public static Sprite skinRightArm;
        public static Sprite skinLeftLeg;
        public static Sprite skinRightLeg;
        public static Sprite skinLeftFoot;
        public static Sprite skinRightFoot;

        public static void SetSkinByName(string skinName)
        {
            SetSkin(new PlayerSkin(Path.Combine(GInit.playerSkinPath, skinName ?? string.Empty)));
        }

        public static void SetSkin(PlayerSkin skin)
        {
            skinHead = skin.head ?? ModFactory.CompareTexture("ori:player_head").sprite;
            skinBody = skin.body ?? ModFactory.CompareTexture("ori:player_body").sprite;
            skinLeftArm = skin.leftArm ?? ModFactory.CompareTexture("ori:player_left_arm").sprite;
            skinRightArm = skin.rightArm ?? ModFactory.CompareTexture("ori:player_right_arm").sprite;
            skinLeftLeg = skin.leftLeg ?? ModFactory.CompareTexture("ori:player_left_leg").sprite;
            skinRightLeg = skin.rightLeg ?? ModFactory.CompareTexture("ori:player_right_leg").sprite;
            skinLeftFoot = skin.leftFoot ?? ModFactory.CompareTexture("ori:player_left_foot").sprite;
            skinRightFoot = skin.rightFoot ?? ModFactory.CompareTexture("ori:player_right_foot").sprite;
        }

        public void Modify()
        {
            if (!head) head = ModFactory.CompareTexture("ori:player_head").sprite;
            if (!body) body = ModFactory.CompareTexture("ori:player_body").sprite;
            if (!leftArm) leftArm = ModFactory.CompareTexture("ori:player_left_arm").sprite;
            if (!rightArm) rightArm = ModFactory.CompareTexture("ori:player_right_arm").sprite;
            if (!leftLeg) leftLeg = ModFactory.CompareTexture("ori:player_left_leg").sprite;
            if (!rightLeg) rightLeg = ModFactory.CompareTexture("ori:player_right_leg").sprite;
            if (!leftFoot) leftFoot = ModFactory.CompareTexture("ori:player_left_foot").sprite;
            if (!rightFoot) rightFoot = ModFactory.CompareTexture("ori:player_right_foot").sprite;
        }

        public PlayerSkin(string skinPath)
        {
            try
            {
                name = IOTools.GetDirectoryName(skinPath);
            }
            catch
            {
                name = string.Empty;
            }

            path = skinPath;

            headPath = Path.Combine(skinPath, "head.png");
            bodyPath = Path.Combine(skinPath, "body.png");
            leftArmPath = Path.Combine(skinPath, "left_arm.png");
            rightArmPath = Path.Combine(skinPath, "right_arm.png");
            leftLegPath = Path.Combine(skinPath, "left_leg.png");
            rightLegPath = Path.Combine(skinPath, "right_leg.png");
            leftFootPath = Path.Combine(skinPath, "left_foot.png");
            rightFootPath = Path.Combine(skinPath, "right_foot.png");

            head = File.Exists(headPath) ? Tools.LoadSpriteByPath(headPath, FilterMode.Point, 16) : null;
            body = File.Exists(bodyPath) ? Tools.LoadSpriteByPath(bodyPath, FilterMode.Point, 16) : null;
            leftArm = File.Exists(leftArmPath) ? Tools.LoadSpriteByPath(leftArmPath, FilterMode.Point, 16) : null;
            rightArm = File.Exists(rightArmPath) ? Tools.LoadSpriteByPath(rightArmPath, FilterMode.Point, 16) : null;
            leftLeg = File.Exists(leftLegPath) ? Tools.LoadSpriteByPath(leftLegPath, FilterMode.Point, 16) : null;
            rightLeg = File.Exists(rightLegPath) ? Tools.LoadSpriteByPath(rightLegPath, FilterMode.Point, 16) : null;
            leftFoot = File.Exists(leftFootPath) ? Tools.LoadSpriteByPath(leftFootPath, FilterMode.Point, 16) : null;
            rightFoot = File.Exists(rightFootPath) ? Tools.LoadSpriteByPath(rightFootPath, FilterMode.Point, 16) : null;
        }
    }

#if UNITY_EDITOR
    [AutoByteConverter]
    public struct AutoTest0
    {
        public string id;
        public int num;
        public List<string> testList;
        public int? testNullable;
    }

    [AutoByteConverter]
    public struct AutoTest1
    {
        public AutoTest0 t0;
        public string self;
        public int index;
    }

    [AutoByteConverter]
    public struct AutoTest2
    {
        public AutoTest1 t1;
        public uint uint_index;
        public long long_index;
        public byte byte_index;
    }
#endif



















    public struct Timer
    {
        public float time;

        public readonly bool HasFinished()
        {
            return Tools.time >= time;
        }

        public void Finish()
        {
            time = Tools.time;
        }

        public void Close()
        {
            time = float.PositiveInfinity;
        }

        public void Start(float time)
        {
            this.time = Tools.time + time;
        }

        public void More(float delta)
        {
            time += delta;
        }

        public void Less(float delta)
        {
            time -= delta;
        }

        public readonly float Remainder() => time - Tools.time;
    }

    public interface IUseRadius
    {
        public bool InUseRadius();
        public bool InUseRadius(Vector2 vec);
        public bool InUseRadius(Vector2 vec1, Vector2 vec2);
    }

    public static class PlayerCenter
    {
        public static List<Player> all = new();
        public static List<Player> allReady = new();

        public static void Update()
        {
            if (Server.isServer)
            {
                lock (allReady)
                {
                    NativeArray<bool> isMovingIn = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> thirstValueIn = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> thirstValueOut = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> hungerValueIn = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> hungerValueOut = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> happinessValueOut = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> healthIn = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> healthOut = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                    //填入数据
                    for (int i = 0; i < allReady.Count; i++)
                    {
                        var player = allReady[i];

                        isMovingIn[i] = player.isMoving;
                        thirstValueIn[i] = player.thirstValue;
                        hungerValueIn[i] = player.hungerValue;
                        healthIn[i] = player.health;
                    }

                    int batchCount = allReady.Count / 4;
                    if (batchCount == 0) batchCount = 1;
                    new PropertiesComputingJob()
                    {
                        isMovingIn = isMovingIn,
                        thirstValueIn = thirstValueIn,
                        thirstValueOut = thirstValueOut,
                        hungerValueIn = hungerValueIn,
                        hungerValueOut = hungerValueOut,
                        happinessValueOut = happinessValueOut,
                        healthIn = healthIn,
                        healthOut = healthOut,
                        frameTime = Performance.frameTime
                    }.ScheduleParallel(allReady.Count, batchCount, default).Complete();  //除以4代表由四个 Job Thread 执行

                    //填入数据
                    for (int i = 0; i < allReady.Count; i++)
                    {
                        var player = allReady[i];

                        player.thirstValue -= thirstValueOut[i];
                        player.hungerValue -= hungerValueOut[i];
                        player.happinessValue -= happinessValueOut[i];

                        var healthOutCurrent = healthOut[i];
                        if (healthOutCurrent != 0) player.health -= healthOutCurrent;
                    }

                    isMovingIn.Dispose();
                    thirstValueIn.Dispose();
                    thirstValueOut.Dispose();
                    hungerValueIn.Dispose();
                    hungerValueOut.Dispose();
                    happinessValueOut.Dispose();
                    healthIn.Dispose();
                    healthOut.Dispose();
                }
            }
        }

        [BurstCompile]
        public struct PropertiesComputingJob : IJobFor
        {
            [ReadOnly] public NativeArray<bool> isMovingIn;
            [ReadOnly] public NativeArray<float> thirstValueIn;
            [WriteOnly] public NativeArray<float> thirstValueOut;
            [ReadOnly] public NativeArray<float> hungerValueIn;
            [WriteOnly] public NativeArray<float> hungerValueOut;
            [WriteOnly] public NativeArray<float> happinessValueOut;
            [ReadOnly] public NativeArray<float> healthIn;
            [WriteOnly] public NativeArray<float> healthOut;
            [ReadOnly] public float frameTime;

            [BurstCompile]
            public void Execute(int index)
            {
                bool isMovingTemp = isMovingIn[index];
                float thirstValueTemp = thirstValueIn[index];
                float hungerValueTemp = hungerValueIn[index];

                float thirstValueDelta = frameTime / 40;
                if (isMovingTemp) thirstValueDelta += frameTime / 40;
                thirstValueOut[index] = thirstValueDelta;

                float hungerValueDelta = frameTime / 30;
                if (isMovingTemp) hungerValueDelta += frameTime / 30;
                hungerValueOut[index] = hungerValueDelta;

                float happinessValueDelta = frameTime / 25;
                if (isMovingTemp) happinessValueDelta -= frameTime / 10;
                if (healthIn[index] <= 10) happinessValueDelta += frameTime / 5;
                if (thirstValueTemp <= 30) happinessValueDelta += frameTime / 20;
                if (hungerValueTemp <= 30) happinessValueDelta += frameTime / 20;
                happinessValueOut[index] = happinessValueDelta;

                if (thirstValueTemp <= 0 || hungerValueTemp <= 0)
                    healthOut[index] = frameTime * 15;
                else
                    healthOut[index] = 0;
            }
        }
    }
}
