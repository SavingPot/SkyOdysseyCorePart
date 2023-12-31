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
    public class Player : Creature, IHumanBodyParts<CreatureBodyPart>, IHumanUsingItemRenderer, IOnInventoryItemChange, IItemContainer, IInventoryOwner
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





        public void OnInventoryItemChange(Inventory newValue, string index)
        {
            //* 不能读取 this.inventory, 因为 this.inventory 返回的是正式应用 inventory 的新数值前的值, 这里的代码就是在正式应用 inventory 的新数值
            //* 如果是客户端的话, 正常情况下应当是服务器先调用了 OnInventoryItemChange, 然后客户端才调用 OnInventoryItemChange
            if (isServer)
                SetInventory(newValue);

            var item = inventory.GetItem(index);

            if (isLocalPlayer)
            {
                //刷新背包物品显示
                pui.inventoryItemView.CustomMethod("refresh", null);

                //刷新制造界面
                pui.craftingResultView.CustomMethod(null, null);
                pui.craftingStuffView.CustomMethod(null, null);
                pui.craftingSelectedItemTitleText.RefreshUI();

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

                        case ItemID.FlintHoe:
                            pui.CompleteTask("ori:get_flint_hoe");
                            break;

                        case ItemID.FlintSword:
                            pui.CompleteTask("ori:get_flint_sword");
                            break;

                        case ItemID.IronKnife:
                            pui.CompleteTask("ori:get_iron_knife");
                            break;

                        case ItemID.IronHoe:
                            pui.CompleteTask("ori:get_iron_hoe");
                            break;

                        case ItemID.IronSword:
                            pui.CompleteTask("ori:get_iron_sword");
                            break;

                        case ItemID.Bark:
                            pui.CompleteTask("ori:get_bark");
                            break;

                        case ItemID.BarkVest:
                            pui.CompleteTask("ori:get_bark_vest");
                            break;

                        case ItemID.Stick:
                            pui.CompleteTask("ori:get_stick");
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

                            break;
                    }
                }
            }
        }





        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("属性"), LabelText("经验")] public int experience;
        [LabelText("是否控制背景"), BoxGroup("状态")] public bool isControllingBackground;
        [BoxGroup("属性"), LabelText("挖掘范围")] public float excavationRadius = 2.75f;
        [BoxGroup("属性"), LabelText("掉落的Y位置")] public float fallenY;
        [BoxGroup("属性"), LabelText("重力")] public float gravity;
        [BoxGroup("属性"), LabelText("死亡计时器"), HideInInspector] public float deathTimer;
        [BoxGroup("属性"), LabelText("移动方块阻力")] public float movementBlockResistance = 0.95f;
        public bool onGround;

        public bool askingForGeneratingRegion { get; private set; }
        public float askingForGeneratingRegionTime { get; private set; } = float.NegativeInfinity;
        public bool generatedFirstRegion;
        private bool hasSetPosBySave;



        #region 同步变量

        #region 任务
        [SyncGetter] List<TaskStatusForSave> completedTasks_get() => default; [SyncSetter] void completedTasks_set(List<TaskStatusForSave> value) { }
        [Sync] public List<TaskStatusForSave> completedTasks { get => completedTasks_get(); set => completedTasks_set(value); }

        public void AddCompletedTasks(TaskStatusForSave task)
        {
            var tasksTemp = completedTasks;
            tasksTemp.Add(task);
            completedTasks = tasksTemp;
        }
        #endregion

        #region 皮肤

        [SyncGetter] Sprite skinHead_get() => default; [SyncSetter] void skinHead_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinHead { get => skinHead_get(); set => skinHead_set(value); }
        [SyncGetter] Sprite skinBody_get() => default; [SyncSetter] void skinBody_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinBody { get => skinBody_get(); set => skinBody_set(value); }
        [SyncGetter] Sprite skinLeftArm_get() => default; [SyncSetter] void skinLeftArm_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinLeftArm { get => skinLeftArm_get(); set => skinLeftArm_set(value); }
        [SyncGetter] Sprite skinRightArm_get() => default; [SyncSetter] void skinRightArm_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinRightArm { get => skinRightArm_get(); set => skinRightArm_set(value); }
        [SyncGetter] Sprite skinLeftLeg_get() => default; [SyncSetter] void skinLeftLeg_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinLeftLeg { get => skinLeftLeg_get(); set => skinLeftLeg_set(value); }
        [SyncGetter] Sprite skinRightLeg_get() => default; [SyncSetter] void skinRightLeg_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinRightLeg { get => skinRightLeg_get(); set => skinRightLeg_set(value); }
        [SyncGetter] Sprite skinLeftFoot_get() => default; [SyncSetter] void skinLeftFoot_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinLeftFoot { get => skinLeftFoot_get(); set => skinLeftFoot_set(value); }
        [SyncGetter] Sprite skinRightFoot_get() => default; [SyncSetter] void skinRightFoot_set(Sprite value) { }
        [Sync, SyncDefaultValue(null)] public Sprite skinRightFoot { get => skinRightFoot_get(); set => skinRightFoot_set(value); }

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

        [SyncGetter] Inventory inventory_get() => default; [SyncSetter] void inventory_set(Inventory value) { }
        [Sync(nameof(TempInventory))] public Inventory inventory { get => _inventory; set => inventory_set(value); }
        private Inventory _inventory;

        void TempInventory()
        {
            _inventory = Inventory.ResumeFromStreamTransport(inventory_get(), this);
        }

        public Inventory GetInventory() => inventory;

        //? 当设置物品栏时, 实际上设置的是 _sync_inventory, 也就是修改同步变量
        //? 当读取物品栏时, 实际上读取的是 _inventory, 也就是读取缓存
        //? 当物品栏内的物品被修改时, OnInventoryItemChange 会被调用, 最终会调用 SetInventory 以同步 Inventory 到每一个端
        public void SetInventory(Inventory value) => inventory = value;



        public int usingItemIndex = 0;

        public Item TryGetUsingItem() => inventory.TryGetItem(usingItemIndex);
        public ItemBehaviour TryGetUsingItemBehaviour() => inventory.TryGetItemBehaviour(usingItemIndex);

        #endregion

#if UNITY_EDITOR
        [Button("输出玩家名称")] private void EditorOutputPlayerName() => Debug.Log($"玩家名: {playerName}");
        [Button("输出玩家血量")] private void EditorOutputHealth() => Debug.Log($"血量: {health}");
        [Button("输出区域序号")] private void EditorOutputRegionIndex() => Debug.Log($"区域序号: {regionIndex}");
        [Button("设置手中物品")]
        private void EditorSetUsingItem(string id = "ori:", ushort count = 1)
        {
            var item = ModConvert.ItemDataToItem(ModFactory.CompareItem(id));
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

        #endregion



        #region 物品属性

        public float excavationStrength => TryGetUsingItem()?.data?.excavationStrength ?? ItemData.defaultExcavationStrength;

        [BoxGroup("属性"), LabelText("使用时间")]
        public float itemUseTime;





        [HideInInspector] public SpriteRenderer usingItemRenderer { get; set; }

        #endregion





        /* -------------------------------------------------------------------------- */
        /*                                    临时数据                                    */
        /* -------------------------------------------------------------------------- */
        private float moveVecLastFrame;
        private Collider2D[] itemPickUpObjectsDetectedTemp = new Collider2D[50];





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static int playerLayer { get; private set; }
        public static int playerLayerMask { get; private set; }
        public static int playerOnGroundLayerMask { get; private set; }
        public static float itemPickUpRadius = 2.5f;
        public static int quickInventorySlotCount = 8;   //偶数
        public static int halfQuickInventorySlotCount = quickInventorySlotCount / 2;
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

        [BoxGroup("组件"), LabelText("UI画布"), ReadOnly]
        public Transform playerCanvas;

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
        [ServerRpc] static void Mtd1(Creature c, NetworkConnection caller) { Debug.Log("Mtd1"); }
        [ServerRpc] static void Mtd2(Player c, NetworkConnection caller) { Debug.Log("Mtd2"); }



        public override void InitAfterAwake()
        {
            base.InitAfterAwake();

            //把 inventory 的初始值缓存下来
            TempInventory();

            //加载服务器存档中的位置
            if (isServer)
            {
                if (Init.save.pos != Vector2.zero)
                {
                    transform.position = Init.save.pos;
                    hasSetPosBySave = true;
                }

                //这一行不是必要的, inventory 通常不会为空, 但是我们要保证代码 100% 正常运行
                inventory ??= new(inventorySlotCount, this);
            }
        }

        protected override void Start()
        {
            Debug.Log(this == null);
            Mtd2(this, null);
            Mtd1(this, null);

            base.Start();

            if (isLocalPlayer)
            {
                //初始化玩家的 UI 界面
                pui = new(this);

                //设置相机跟随
                Tools.instance.mainCameraController.lookAt = transform;
                Tools.instance.mainCameraController.lookAtDelta = new(0, 2);
                Debug.Log("本地客户端玩家是: " + gameObject.name);

                //if (!isServer)
                //    CmdCheckMods();

                managerGame.weatherParticle.transform.SetParent(transform);
                managerGame.weatherParticle.transform.localPosition = new(0, 40);

                ServerGenerateRegion(regionIndex, true);
            }



            CreateModel();
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

            BindHumanAnimations(this);
            animWeb.CreateConnectionFromTo("slight_rightarm_lift", "idle", () => true, 0.15f * 2, 0);



            OnNameChange(playerName);
        }

        protected override void Awake()
        {
            base.Awake();

            Func<float> oldValue = velocityFactor;
            velocityFactor = () => oldValue() * (transform.localScale.x.Sign() != rb.velocity.x.Sign() ? 0.75f : 1);

            playerCanvas = transform.Find("Canvas");

            PlayerCenter.AddPlayer(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            animWeb.Stop();

            PlayerCenter.RemovePlayer(this);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isLocalPlayer)
                LocalFixedUpdate();

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

                //generatedFirstRegion 是防止初始区域 y<0 导致直接摔死
                if (damageValue >= 2 && caller.generatedFirstRegion)
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

        public override void SetOrientation(bool right)
        {
            base.SetOrientation(right);

            if (nameText)
            {
                if (right)
                    nameText.transform.SetScaleXAbs();
                else
                    nameText.transform.SetScaleXNegativeAbs();
            }
        }




        protected override void Update()
        {
            base.Update();

            if (isLocalPlayer)
                LocalUpdate();

            inventory?.DoBehaviours();

            RefreshInventory();
        }

        protected virtual void AliveLocalUpdate()
        {
            if (PlayerCanControl(this))
            {
                Tools.instance.mainCameraController.shakeLevel = isHurting ? 6 : 0;

                //如果在地面上并且点跳跃
                if (onGround && PlayerControls.Jump(this))
                    Jump();

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
                        value = quickInventorySlotCount - 1;

                    SwitchItem(value);
                }
                else if (PlayerControls.SwitchToNextItem(this))
                {
                    int value = usingItemIndex + 1;

                    if (value > quickInventorySlotCount - 1)
                        value = 0;

                    SwitchItem(value);
                }
                #endregion

                //如果按右键
                if (PlayerControls.UseItem(this))
                {
                    UseItem(this);
                }

                //如果按S
                if (PlayerControls.PlaceBlockUnderPlayer(this))
                {
                    var usingItem = TryGetUsingItem();
                    var behaviour = TryGetUsingItemBehaviour();

                    if (usingItem != null && behaviour != null && usingItem.data.isBlock)
                    {
                        var downPoint = mainCollider.DownPoint();
                        behaviour.UseAsBlock(PosConvert.WorldToMapPos(new(downPoint.x, downPoint.y - 1)), false);
                    }
                }

                if (nameText)
                {
                    isControllingBackground = PlayerControls.IsControllingBackground(this);
                }
            }

            //血量低于 10 放心跳声, 死了就不播放声音
            if (health <= 15)
            {
                GAudio.Play(AudioID.Heartbeat, true);
            }
        }

        public static Action<Player> GravitySet = caller =>
        {
            if (!caller.generatedFirstRegion ||
                GM.instance.generatingExistingRegion ||
                GM.instance.generatingNewRegions.Any(p => p == PosConvert.WorldPosToRegionIndex(caller.transform.position)))  //获取区域序号不用 caller.regionIndex 是因为只有区域加载成功, caller.regionIndex才会正式改编
            {
                caller.gravity = 0;
                return;
            }

            caller.gravity = playerDefaultGravity;

            if (!caller.managerGame.generatedExistingRegions.Any(p => p.index == caller.regionIndex) || caller.isDead)
            {
                caller.gravity *= 0.05f;
            }
        };

        protected virtual void LocalUpdate()
        {
            GravitySet(this);
            rb.gravityScale = gravity;

            if (!isDead)
            {
                AliveLocalUpdate();
            }
            if (!isHurting)
            {
                DeathRotation();
            }

            ////AutoGenerateRegion();

            //刷新状态栏
            RefreshPropertiesBar();


            pui?.Update();





            #region 背包
            if (PlayerControls.Backpack(this))
            {
                if (pui != null)
                {
                    ShowOrHideBackpackAndSetSidebarToCrafting();
                }
            }
            #endregion
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            Physics2D.OverlapCircleNonAlloc(transform.position, itemPickUpRadius, itemPickUpObjectsDetectedTemp);

            foreach (var other in itemPickUpObjectsDetectedTemp)
            {
                if (other == null)
                    break;

                if (other.TryGetComponent<Drop>(out var drop))
                {
                    //TODO 不要检测整个背包，而是检测这个物品能否放入
                    if (inventory.IsFull())
                        return;

                    ServerAddItem(drop.itemData);
                    GAudio.Play(AudioID.PickUpItem);

                    drop.Death();
                }
            }
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
                GM.instance.SummonDrop(pos, item.data.id, count, item.customData.ToString());
            }
        }

        public void SwitchItem(int index)
        {
            usingItemIndex = index;

            //播放切换音效
            GAudio.Play(AudioID.SwitchQuickInventorySlot);

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

        //// public void AutoGenerateRegion()
        //// {
        ////     //缓存优化性能
        ////     Vector2Int currentIndex = regionIndex;
        //
        ////     if (generatedFirstRegion && !askingForGeneratingRegion && askingForGeneratingRegionTime + 5 <= Tools.time && !GM.instance.generatingExistingRegion && !GM.instance.generatingNewRegion)// && TryGetRegion(out Region region))
        ////     {
        ////         Vector2Int newIndex = PosConvert.WorldPosToRegionIndex(transform.position);
        //
        ////         //如果变了
        ////         if (currentIndex != newIndex)
        ////         {
        ////             //生成区域并刷新时间
        ////             ServerGenerateRegion(newIndex, false);
        ////             askingForGeneratingRegionTime = Tools.time;
        //
        ////             //生成完后正式切换区域序号
        ////             if (managerGame.generatedExistingRegions.Any(p => p.index == newIndex))
        ////             {
        ////                 regionIndex = newIndex;
        ////             }
        ////         }
        ////     }
        //// }

        public readonly Dictionary<string, (Action Appear, Action Disappear)> backpackSidebarTable = new();
        public string usingBackpackSidebar = string.Empty;

        public void SetBackpackSidebar(string id)
        {
            if (backpackSidebarTable.TryGetValue(usingBackpackSidebar, out var value))
            {
                value.Disappear();
            }

            if (backpackSidebarTable.TryGetValue(id, out value))
            {
                value.Appear();
                usingBackpackSidebar = id;
                return;
            }


            Debug.LogError($"未找到侧边栏 {id}");
        }

        public void ShowOrHideBackpackAndSetSidebarToCrafting()
        {
            //Backpack 是整个界面
            //Inventory 是中间的所有物品
            //QuickInventory 是不打开背包时看到的几格物品栏

            ShowOrHideBackpackAndSetSideBarTo("ori:craft");
        }

        public void ShowOrHideBackpackAndSetSideBarTo(string sidebarId)
        {
            if (!pui.backpackMask.gameObject.activeSelf)
                SetBackpackSidebar(sidebarId);

            ShowOrHideBackpack();
        }

        public void ShowOrHideBackpack()
        {
            //启用状态 -> 禁用
            if (pui.backpackMask.gameObject.activeSelf)
            {
                ItemInfoShower.Hide();
                ItemDragger.CancelDragging();
                GameUI.SetPage(null);
                GAudio.Play(AudioID.CloseBackpack);
            }
            //禁用状态 -> 启用
            else
            {
                GameUI.SetPage(pui.backpackMask);
                OnInventoryItemChange(inventory, null);
                GAudio.Play(AudioID.OpenBackpack);
            }
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
                caller.map.TryGetBlock(PosConvert.WorldToMapPos(cursorPos), caller.isControllingBackground, out Block block) &&
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

            //设置颜色
            foreach (var sr in spriteRenderers)
            {
                sr.color = deathLowestColor;
            }

            if (isLocalPlayer)
            {
                GAudio.Play(AudioID.Death);
                MethodAgent.CallUntil(() => pui != null, () => pui.ShowRebornPanel());
            }

            animWeb.Stop();
        }

        public override void OnRebornServer(float newHealth, Vector2 newPos, NetworkConnection caller)
        {
            hungerValue = 20;
            thirstValue = 20;
        }

        public override void OnRebornClient(float newHealth, Vector2 newPos, NetworkConnection caller)
        {
            //设置颜色
            foreach (var sr in spriteRenderers)
            {
                sr.color = Color.white;
            }

            if (isLocalPlayer)
            {
                MethodAgent.CallUntil(() => pui != null, () => pui.rebornPanel.gameObject.SetActive(false));
            }
        }

        public override void OnGetHurtServer(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }

        public override void OnGetHurtClient(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            if (GControls.mode == ControlMode.Gamepad)
                GControls.GamepadVibrationMediumStrong();
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
            nameText.rectTransform.AddLocalPosY(30f);
            nameText.text.SetFontSize(7);
            nameText.text.text = newValue;
            nameText.autoCompareText = false;
        }

        public void RefreshInventory()
        {
            //缓存物品栏以保证性能
            var inventoryTemp = inventory;

            /* --------------------------------- 刷新手部物品 --------------------------------- */
            usingItemRenderer.sprite = inventoryTemp.TryGetItem(usingItemIndex)?.data?.texture?.sprite;

            /* --------------------------------- 刷新头盔的贴图 -------------------------------- */
            if (Item.IsHelmet(inventoryTemp.helmet))
            {
                head.armorSr.sprite = inventoryTemp.helmet.data.Helmet.head?.sprite;
            }
            else
            {
                head.armorSr.sprite = null;
            }

            /* --------------------------------- 刷新胸甲的贴图 -------------------------------- */
            if (Item.IsBreastplate(inventoryTemp.breastplate))
            {
                body.armorSr.sprite = inventoryTemp.breastplate.data.Breastplate.body?.sprite;
                leftArm.armorSr.sprite = inventoryTemp.breastplate.data.Breastplate.leftArm?.sprite;
                rightArm.armorSr.sprite = inventoryTemp.breastplate.data.Breastplate.rightArm?.sprite;
            }
            else
            {
                body.armorSr.sprite = null;
                leftArm.armorSr.sprite = null;
                rightArm.armorSr.sprite = null;
            }

            /* --------------------------------- 刷新护腿的贴图 -------------------------------- */
            if (Item.IsLegging(inventoryTemp.legging))
            {
                leftLeg.armorSr.sprite = inventoryTemp.legging.data.Legging.leftLeg?.sprite;
                rightLeg.armorSr.sprite = inventoryTemp.legging.data.Legging.rightLeg?.sprite;
            }
            else
            {
                leftLeg.armorSr.sprite = null;
                rightLeg.armorSr.sprite = null;
            }

            /* --------------------------------- 刷新鞋子的贴图 -------------------------------- */
            if (Item.IsBoots(inventoryTemp.boots))
            {
                leftFoot.armorSr.sprite = inventoryTemp.boots.data.Boots.leftFoot?.sprite;
                rightFoot.armorSr.sprite = inventoryTemp.boots.data.Boots.rightFoot?.sprite;
            }
            else
            {
                leftFoot.armorSr.sprite = null;
                rightFoot.armorSr.sprite = null;
            }
        }

        public void OnHoldAttack()
        {
            if (!isLocalPlayer || isDead)
                return;

            //如果还在 攻击CD 就返回
            if (itemUseTime + (TryGetUsingItem()?.data?.useCD ?? ItemData.defaultUseCD) > Tools.time)
            {
                return;
            }

            //如果 鼠标在挖掘范围内 && 在鼠标位置获取到方块 && 方块是激活的
            if (InUseRadius() && map.TryGetBlock(PosConvert.WorldToMapPos(cursorWorldPos), isControllingBackground, out Block block) && block.gameObject.activeInHierarchy)
            {
                ExcavateBlock(block);
            }
            else
            {
                OnStartAttack();
            }

            //设置时间
            itemUseTime = Tools.time;
        }

        /// <returns>鼠标是否在使用范围内</returns>
        public bool InUseRadius() => InUseRadius(cursorWorldPos);

        public bool InUseRadius(Vector2 vec) => InUseRadius(transform.position, vec);

        public bool InUseRadius(Vector2 vec1, Vector2 vec2) => Vector2.Distance(vec1, vec2) <= excavationRadius + (TryGetUsingItem()?.data?.extraDistance ?? 0);

        #region 挖掘方块
        private void ExcavateBlock(Block block)
        {
            if (!isLocalPlayer || block == null)
                return;

            if (block != null)
            {
                if (!animWeb.GetAnim("slight_rightarm_lift", 0).isPlaying)
                    animWeb.SwitchPlayingTo("slight_rightarm_lift");

                block.TakeDamage(excavationStrength);

                GAudio.Play(AudioID.ExcavatingBlock);

                if (GControls.mode == ControlMode.Gamepad)
                    GControls.GamepadVibrationSlighter(0.1f);
            }

            itemUseTime = Tools.time;
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


        #region 生成区域

        //告诉服务器要生成, 并让服务器生成 (隐性), 然后在生成好后传给客户端
        [Button]
        public void ServerGenerateRegion(Vector2Int index, bool isFirstGeneration, string specificTheme = null)
        {
            askingForGeneratingRegion = true;

            ServerGenerateRegionCore(index, isFirstGeneration, specificTheme);
        }

        //告诉服务器要生成, 并让服务器生成 (隐性), 然后在生成好后传给客户端
        [ServerRpc]
        private void ServerGenerateRegionCore(Vector2Int index, bool isFirstGeneration, string specificTheme, NetworkConnection caller = null)
        {
            MethodAgent.TryRun(() =>
            {
                Debug.Log($"Player={netId} 请求生成区域 {index}");

                MethodAgent.RunThread(() =>
                {
                    //如果没有则生成新的区域
                    GM.instance.GenerateNewRegion(index, specificTheme);

                    //如果有直接让服务器生成
                    lock (GFiles.world.regionData)
                    {
                        foreach (Region region in GFiles.world.regionData)
                        {
                            if (region.index == index && region.generatedAlready)
                            {
                                MethodAgent.RunOnMainThread(() =>
                                {
                                    //* 如果是客户端发送的申请: 服务器生成->客户端生成   (如果 服务器和客户端 并行生成, 可能会导致 bug)
                                    if (!isLocalPlayer)
                                        GM.instance.GenerateExistingRegion(
                                            region,
                                            () => ConnectionGenerateRegion(region, isFirstGeneration, caller),
                                            () => ConnectionGenerateRegion(region, isFirstGeneration, caller),
                                            (ushort)(GFiles.settings.performanceLevel / 2)
                                        );
                                    //* 如果是服务器发送的申请: 服务器生成
                                    else
                                        ConnectionGenerateRegion(region, isFirstGeneration, caller);
                                });

                                break;
                            }
                        }
                    }
                });
            }, true);
        }

        [ConnectionRpc]
        void ConnectionGenerateRegion(Region region, bool isFirstGeneration, NetworkConnection caller)
        {
            MethodAgent.TryRun(() =>
            {
                Debug.Log($"收到服务器回调, 正在生成已有区域 {region.index}");

                GM.instance.GenerateExistingRegion(region, () =>
                {
                    if (isFirstGeneration)
                    {
                        //如果先前没获取过位置, 则将玩家的位置设置到该区域出生点
                        if (!hasSetPosBySave)
                            transform.position = region.spawnPoint.To2();

                        generatedFirstRegion = true;
                    }
                    //下面的参数: 如果是 首次中心生成 就快一点, 否则慢一些防止卡顿
                }, null, (ushort)(GFiles.settings.performanceLevel * (isFirstGeneration ? 4 : 0.8f)));

                askingForGeneratingRegion = false;
            }, true);
        }

        #endregion


        #region 玩家行为



        [ServerRpc]
        public void ServerAddItem(Item item, NetworkConnection caller = null)
        {
            ClientAddItem(item);
        }

        [ClientRpc]
        public void ClientAddItem(Item item, NetworkConnection caller = null)
        {
            Item.ResumeFromStreamTransport(ref item);

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
                Item.ResumeFromStreamTransport(ref item);
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

        readonly Collider2D[] OnGroundHits = new Collider2D[10];


        #region 移动和转向
        public override void Movement()
        {
            float move;

            if (!PlayerCanControl(this))
                move = 0;
            else
                move = PlayerControls.Move(this);

            onGround = false;
            for (int i = 0; i < OnGroundHits.Length; i++)
            {
                OnGroundHits[i] = default;
            }
            var position = transform.position;
            Physics2D.OverlapAreaNonAlloc(
                    new(
                        position.x + mainCollider.offset.x - mainCollider.size.x * 0.5f,
                        position.y + mainCollider.offset.y - mainCollider.size.y * 0.5f
                    ),
                    new(
                        position.x + mainCollider.offset.x + mainCollider.size.x * 0.5f,
                        position.y + mainCollider.offset.y - mainCollider.size.y * 0.5f - 0.2f
                    ),
                    OnGroundHits,
                    playerOnGroundLayerMask);

            foreach (var item in OnGroundHits)
            {
                if (item == null)
                    break;

                if (!item.isTrigger && Block.TryGetBlockFromCollider(item, out _))
                {
                    onGround = true;
                    break;
                }
            }

            if (isLocalPlayer && !isDead)
            {
                //设置速度
                rb.velocity = GetMovementVelocity(new(move, 0));

                if (onGround && move == 0 && rb.velocity.x != 0)
                {
                    if (Mathf.Abs(rb.velocity.x) <= 0.1f)
                    {
                        rb.velocity = Vector2.zero;
                    }
                    else
                    {
                        rb.velocity = GetMovementVelocity(new(-rb.velocity.x * movementBlockResistance, 0));
                    }
                }

                //执行 移动的启停
                if (move == 0 && moveVecLastFrame != 0)
                {
                    ServerOnStopMovement();
                }
                else if (move != 0 && moveVecLastFrame == 0)
                {
                    ServerOnStartMovement();
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
            if (!p.isLocalPlayer)
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
                    if (p.pui.moveJoystick && p.pui.cursorJoystick)
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

            itemUseTime = Tools.time;
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
                animWeb.SwitchPlayingTo("attack_leftarm", 0);
            if (rightArm)
                animWeb.SwitchPlayingTo("attack_rightarm", 0);
        }

        #endregion

        #region 死亡

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

        public void RefreshPropertiesBar()
        {
            if (pui == null)
                return;

            pui.thirstBarFull.image.fillAmount = thirstValue / maxThirstValue;
            pui.hungerBarFull.image.fillAmount = hungerValue / maxHungerValue;
            pui.happinessBarFull.image.fillAmount = happinessValue / maxHappinessValue;
            pui.healthBarFull.image.fillAmount = health / maxHealth;
        }





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

        public static bool TryGetLocal(out Player p)
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
                    Item.ResumeFromStreamTransport(ref container.items[i]);
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
            inventory.SetSlotCount(slotCount);
            entity.SetInventory(Inventory.ResumeFromStreamTransport(inventory, entity));
        }

        public static void WriteInventoryToCustomData<T>(this T entity) where T : Entity, IInventoryOwner
        {
            entity.customData["ori:inventory"]["data"] = JsonTools.ToJson(entity.GetInventory());
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














    public static class PlayerCenter
    {
        public static List<Player> all = new();
        public static Action<Player> OnAddPlayer = _ => { };
        public static Action<Player> OnRemovePlayer = _ => { };

        public static void AddPlayer(Player player)
        {
            all.Add(player);
            OnAddPlayer(player);
        }

        public static void RemovePlayer(Player player)
        {
            all.Remove(player);
            OnRemovePlayer(player);
        }

        public static void Update()
        {
            if (Server.isServer)
            {
                foreach (var player in all)
                {
                    bool isMoving = player.isMoving;
                    float thirstValue = player.thirstValue;
                    float hungerValue = player.hungerValue;
                    float happinessValue = player.happinessValue;

                    float thirstValueDelta = Performance.frameTime / 40;
                    if (isMoving) thirstValueDelta += Performance.frameTime / 40;
                    player.thirstValue = thirstValue - thirstValueDelta;

                    float hungerValueDelta = Performance.frameTime / 30;
                    if (isMoving) hungerValueDelta += Performance.frameTime / 40;
                    player.hungerValue = hungerValue - hungerValueDelta;

                    float happinessValueDelta = Performance.frameTime / 25;
                    if (isMoving) happinessValueDelta += Performance.frameTime / 10;
                    if (thirstValue <= 30) happinessValueDelta += Performance.frameTime / 30;
                    if (hungerValue <= 30) happinessValueDelta += Performance.frameTime / 20;
                    player.happinessValue = happinessValue - happinessValueDelta;

                    if (thirstValue <= 0 || hungerValue <= 0)
                        player.health -= Performance.frameTime * 3; //TODO：定时受伤，而非一直扣血
                }
            }
        }
    }
}
