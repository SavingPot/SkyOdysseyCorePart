using Cysharp.Threading.Tasks;
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
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using static GameCore.PlayerUI;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace GameCore
{
    /// <summary>
    /// 玩家的逻辑脚本
    /// </summary>
    [ChineseName("玩家"), EntityBinding(EntityID.Player), NotSummonable, RequireComponent(typeof(Rigidbody2D))]
    public sealed class Player : Creature, IHumanBodyParts<CreatureBodyPart>, IInventoryOwner
    {
        /* -------------------------------------------------------------------------- */
        /*                                     接口                                     */
        /* -------------------------------------------------------------------------- */
        public const int inventorySlotCountConst = 32;
        int IInventoryOwner.inventorySlotCount => inventorySlotCountConst;

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

            //刷新物品栏
            EntityInventoryOwnerBehaviour.RefreshInventory(this);

            var item = inventory.GetItem(index);

            if (isLocalPlayer)
            {
                //刷新背包面板
                pui.RefreshCurrentBackpackPanel();

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

                        case ItemID.Potato:
                            pui.CompleteTask("ori:get_potato");
                            break;

                        case ItemID.Onion:
                            pui.CompleteTask("ori:get_onion");
                            break;

                        case ItemID.Watermelon:
                            pui.CompleteTask("ori:get_watermelon");
                            break;

                        default:
                            if (item.data.GetTag("ori:log").hasTag)
                                pui.CompleteTask("ori:get_log");

                            if (item.data.GetTag("ori:meat").hasTag)
                                pui.CompleteTask("ori:get_meat");

                            if (item.data.GetTag("ori:egg").hasTag)
                                pui.CompleteTask("ori:get_egg");

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
        [BoxGroup("属性"), LabelText("挖掘范围")] public float excavationRadius = 2.8f;
        [BoxGroup("属性"), LabelText("重力")] public float gravity;
        [BoxGroup("属性"), LabelText("死亡计时器"), HideInInspector] public float deathTimer;
        [BoxGroup("属性"), LabelText("方块摩擦力")] public float blockFriction = 0.96f;

        #region 区域生成

        public bool askingForGeneratingRegion { get; private set; }
        public float askingForGeneratingRegionTime { get; private set; } = float.NegativeInfinity;
        public bool generatedFirstRegion;
        private bool hasSetPosBySave;
        bool regionGenerationIsFirstGeneration;
        List<BlockSave> regionGenerationSaves = null;
        int regionGenerationBlocksCount;
        Region regionGenerationRegion = null;

        #endregion



        #region 同步变量

        #region 任务
        List<TaskStatusForSave> completedTasks_temp; void completedTasks_set(List<TaskStatusForSave> value) { }
        [Sync] public List<TaskStatusForSave> completedTasks { get => completedTasks_temp; set => completedTasks_set(value); }

        public void AddCompletedTasks(TaskStatusForSave task)
        {
            var tasksTemp = completedTasks;
            tasksTemp.Add(task);
            completedTasks = tasksTemp;
        }
        #endregion

        #region 皮肤

        Sprite skinHead_temp; void skinHead_set(Sprite value) { }
        [Sync] public Sprite skinHead { get => skinHead_temp; set => skinHead_set(value); }
        Sprite skinBody_temp; void skinBody_set(Sprite value) { }
        [Sync] public Sprite skinBody { get => skinBody_temp; set => skinBody_set(value); }
        Sprite skinLeftArm_temp; void skinLeftArm_set(Sprite value) { }
        [Sync] public Sprite skinLeftArm { get => skinLeftArm_temp; set => skinLeftArm_set(value); }
        Sprite skinRightArm_temp; void skinRightArm_set(Sprite value) { }
        [Sync] public Sprite skinRightArm { get => skinRightArm_temp; set => skinRightArm_set(value); }
        Sprite skinLeftLeg_temp; void skinLeftLeg_set(Sprite value) { }
        [Sync] public Sprite skinLeftLeg { get => skinLeftLeg_temp; set => skinLeftLeg_set(value); }
        Sprite skinRightLeg_temp; void skinRightLeg_set(Sprite value) { }
        [Sync] public Sprite skinRightLeg { get => skinRightLeg_temp; set => skinRightLeg_set(value); }
        Sprite skinLeftFoot_temp; void skinLeftFoot_set(Sprite value) { }
        [Sync] public Sprite skinLeftFoot { get => skinLeftFoot_temp; set => skinLeftFoot_set(value); }
        Sprite skinRightFoot_temp; void skinRightFoot_set(Sprite value) { }
        [Sync] public Sprite skinRightFoot { get => skinRightFoot_temp; set => skinRightFoot_set(value); }

        #endregion

        #region 属性

        #region 饥饿值
        float hungerValue_temp; void hungerValue_set(float value) { }
        [Sync] public float hungerValue { get => hungerValue_temp; set => hungerValue_set(value); }
        public static float defaultHungerValue = 100;
        public static float maxHungerValue = 100;
        #endregion

        #region 幸福值
        float happinessValue_temp; void happinessValue_set(float value) { }
        [Sync] public float happinessValue { get => happinessValue_temp; set => happinessValue_set(value); }
        public static float defaultHappinessValue = 50;
        public static float maxHappinessValue = 100;
        #endregion

        #region 玩家名
        string playerName_temp; void playerName_set(string value) { }
        [Sync(nameof(OnNameChangeMethod)), SyncDefaultValue("")] public string playerName { get => playerName_temp; set => playerName_set(value); }

        void OnNameChangeMethod(byte[] _)
        {
            OnNameChange(playerName);
        }
        #endregion

        #region 物品栏
        Inventory inventory_temp; void inventory_set(Inventory value) { }
        [Sync(nameof(TempInventory))] public Inventory inventory { get => inventory_temp; set => inventory_set(value); }

        void TempInventory(byte[] _)
        {
            inventory_temp = Inventory.ResumeFromStreamTransport(inventory_temp, this);

            //刷新物品栏
            if (Init.isServerCompletelyReady)
                EntityInventoryOwnerBehaviour.RefreshInventory(this);
        }

        public Inventory GetInventory() => inventory;

        //? 当设置物品栏时, 实际上设置的是 _sync_inventory, 也就是修改同步变量
        //? 当读取物品栏时, 实际上读取的是 _inventory, 也就是读取缓存
        //? 当物品栏内的物品被修改时, OnInventoryItemChange 会被调用, 最终会调用 SetInventory 以同步 Inventory 到每一个端
        public void SetInventory(Inventory value) => inventory = value;



        public int usingItemIndex { get; set; } = 0;

        public Item TryGetUsingItem() => inventory.GetItemChecked(usingItemIndex);
        public ItemBehaviour TryGetUsingItemBehaviour() => inventory.GetItemBehaviourChecked(usingItemIndex);

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
        public float useRadius => excavationRadius + (TryGetUsingItem()?.data?.extraDistance ?? 0);





        [HideInInspector] public SpriteRenderer usingItemRenderer { get; set; }

        public void SetUsingItemRendererLocalPositionAndScale(Vector2 localPosition, Vector2 localScale)
        {
            usingItemRenderer.transform.localPosition = new(0.1f + localPosition.x, -0.5f + localPosition.y);
            usingItemRenderer.transform.SetScale(0.5f * localScale.x, 0.5f * localScale.y);
        }

        #endregion




        /* -------------------------------------------------------------------------- */
        /*                                    临时数据                                    */
        /* -------------------------------------------------------------------------- */
        private float moveVecLastFrame;
        private readonly Collider2D[] itemPickUpObjectsDetectedTemp = new Collider2D[40];





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static int playerLayer { get; private set; }
        public static int playerLayerMask { get; private set; }
        public static float itemPickUpRadius = 1.75f;
        public static int quickInventorySlotCount = 8;   //偶数
        public static int halfQuickInventorySlotCount = quickInventorySlotCount / 2;
        public static Func<Player, bool> PlayerCanControl = player => GameUI.page == null || !GameUI.page.ui && player.generatedFirstRegion;
        public const float playerDefaultGravity = 6f;

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

        public static int backpackPanelHeight = 450;










        #region Unity 回调

        protected override void Awake()
        {
            base.Awake();

            Func<float> oldValue = velocityFactor;
            velocityFactor = () => oldValue() * (transform.localScale.x.Sign() != rb.velocity.x.Sign() ? 0.75f : 1);

            playerCanvas = transform.Find("Canvas");
        }

        public override void Initialize()
        {
            base.Initialize();

            //加载服务器存档中的位置
            if (isServer)
            {
                if (Init.save.pos != Vector2.zero)
                {
                    transform.position = Init.save.pos;
                    fallenY = Init.save.pos.y;
                    hasSetPosBySave = true;
                }
            }



            #region 初始化模型
            CreateModel();
            body = AddBodyPart("body", skinBody, Vector2.zero, 5, model.transform, BodyPartType.Body);
            head = AddBodyPart("head", skinHead, new(0, -0.03f), 10, body, BodyPartType.Head, new(-0.03f, -0.04f));
            rightArm = AddBodyPart("rightArm", skinRightArm, new(0, 0.03f), 8, body, BodyPartType.RightArm);
            leftArm = AddBodyPart("leftArm", skinLeftArm, new(0, 0.03f), 3, body, BodyPartType.LeftArm);
            rightLeg = AddBodyPart("rightLeg", skinRightLeg, new(0.02f, 0.04f), 3, body, BodyPartType.RightLeg);
            leftLeg = AddBodyPart("leftLeg", skinLeftLeg, new(-0.02f, 0.04f), 1, body, BodyPartType.LeftLeg);
            rightFoot = AddBodyPart("rightFoot", skinRightFoot, Vector2.zero, 3, rightLeg, BodyPartType.RightFoot);
            leftFoot = AddBodyPart("leftFoot", skinLeftFoot, Vector2.zero, 1, leftLeg, BodyPartType.LeftFoot);

            //添加手持物品的渲染器
            EntityInventoryOwnerBehaviour.CreateUsingItemRenderer(this, rightArm.transform, 9);

            //把 inventory 的初始值缓存下来
            TempInventory(null);

            //这一行不是必要的, inventory 通常不会为空, 但是我们要保证代码 100% 正常运行
            if (isServer)
                inventory ??= new(((IInventoryOwner)this).inventorySlotCount, this);




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



            BindHumanAnimations(this);


            //显示名称
            OnNameChange(playerName);
            #endregion
        }

        public override void AfterInitialization()
        {
            base.AfterInitialization();

            PlayerCenter.AddPlayer(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            PlayerUI.instance = null;
            animWeb.Stop();

            PlayerCenter.RemovePlayer(this);
        }

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

            EntityInventoryOwnerBehaviour.OnUpdate(this);

#if DEBUG
            if (Keyboard.current?.spaceKey?.wasPressedThisFrame ?? false)
                deathTimer = 0;
#endif
        }

        public bool HasUseCDPast() => Tools.time >= itemUseTime + (TryGetUsingItem()?.data?.useCD ?? ItemData.defaultUseCD);

        private void AliveLocalUpdate()
        {
            /* ----------------------------------- 背包 (不能放在 PlayerCanControl 里是因为打开背包后 PlayerCanControl 返回 false, 然后就关不掉背包了) ----------------------------------- */
            if (PlayerControls.Backpack(this))
            {
                if (pui != null && GameUI.page?.ui != pui.dialogPanel)
                {
                    pui.ShowOrHideBackpackAndSetPanelToInventory();
                }
            }

            if (PlayerCanControl(this))
            {
                Tools.instance.mainCameraController.shakeLevel = isHurting ? 6 : 0;

                /* ------------------------------- 如果在地面上并且点跳跃 ------------------------------ */
                if (isOnGround && PlayerControls.Jump(this))
                    rb.SetVelocityY(GetJumpVelocity(30));

                /* ----------------------------------- 挖掘与攻击 ----------------------------------- */
                if (PlayerControls.HoldingAttack(this))  //检测物品的使用CD
                {
                    OnHoldAttack();
                }

                /* ---------------------------------- 抛弃物品 ---------------------------------- */
                if (PlayerControls.ThrowItem(this))
                    ServerThrowItem(usingItemIndex.ToString(), 1);


                /* ----------------------------------- 睡眠 ----------------------------------- */
                if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                {
                    //TODO: 睡眠
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

                if (PlayerControls.SwitchToPreviousItem(this))
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

                //通过触屏点击来使用物品
                if (GControls.mode == ControlMode.Touchscreen && Touchscreen.current != null)
                {
                    TouchControl tc = Touchscreen.current.touches[0];

                    if (tc.press.wasPressedThisFrame)
                    {
                        var touchWorldPos = Tools.instance.mainCamera.ScreenToWorldPoint(tc.position.ReadValue());

                        UseItem(touchWorldPos);
                    }
                }

                //如果按右键
                if (PlayerControls.UseItem(this))
                {
                    UseItem(cursorWorldPos);
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

                isControllingBackground = PlayerControls.IsControllingBackground(this);
            }

            //血量低于 10 就播放心跳声, 死了就不播放声音
            if (health <= 15)
            {
                GAudio.Play(AudioID.Heartbeat, true);
            }
        }

        public static Action<Player> GravitySet = caller =>
        {
            if (!caller.generatedFirstRegion ||
                !GM.instance.generatedExistingRegions.Any(p => p.index == PosConvert.WorldPosToRegionIndex(caller.transform.position)))  //获取区域序号不用 caller.regionIndex 是因为只有区域加载成功, caller.regionIndex才会正式改编
            {
                caller.gravity = 0;
                return;
            }

            caller.gravity = playerDefaultGravity;
        };

        private void LocalUpdate()
        {
            GravitySet(this);
            AutoSetPlayerOrientation();
            rb.gravityScale = gravity;

            if (!isDead)
            {
                AliveLocalUpdate();

                transform.localRotation = Quaternion.identity;
            }
            else
            {
                transform.localRotation = deathQuaternion;
            }

            ////AutoGenerateRegion();

            //刷新状态栏
            pui?.Update();
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();

            Physics2D.OverlapCircleNonAlloc(transform.position, itemPickUpRadius, itemPickUpObjectsDetectedTemp);

            foreach (var other in itemPickUpObjectsDetectedTemp)
            {
                if (other == null)
                    break;

                if (other.TryGetComponent<Drop>(out var drop) && !drop.isDead)
                {
                    //检测有没有栏位存放物品
                    if (!Inventory.GetIndexesToPutItemIntoItems(inventory.slots, drop.item, out _))
                        return;

                    //TODO: 改成吸引 Drop 到玩家身旁
                    ServerAddItem(drop.item);
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
                GM.instance.SummonDrop(pos, item.data.id, count, item.customData?.ToString());
            }
        }

        public void SwitchItem(int index)
        {
            usingItemIndex = index;

            //刷新物品栏
            EntityInventoryOwnerBehaviour.RefreshUsingItemRenderer(this);

            //播放切换音效
            GAudio.Play(AudioID.SwitchQuickInventorySlot);

            //改变状态文本
            string itemName = GameUI.CompareText(TryGetUsingItem()?.data?.id).text;
            if (itemName.IsNullOrWhiteSpace())
                itemName = GameUI.CompareText("ori:empty_item").text;

            InternalUIAdder.instance.SetStatusText(GameUI.CompareText("ori:switch_item").text.Replace("{item_id}", itemName));
        }

        public override void WriteDataToSaveObject(EntitySave save)
        {
            base.WriteDataToSaveObject(save);

            if (save is PlayerSave trueSave)
            {
                trueSave.inventory = inventory;
                trueSave.hungerValue = hungerValue;
                trueSave.happinessValue = happinessValue;
                trueSave.completedTasks = completedTasks;
            }
            else
            {
                throw new();
            }
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


        public void UseItem(Vector2 point)
        {
            foreach (var entity in EntityCenter.all)
            {
                /* --------------------------------- 筛选出 NPC -------------------------------- */
                if (entity is not NPC)
                    continue;

                NPC npc = (NPC)entity;

                /* ------------------------------- 如果在两倍的互动范围内  ------------------------------- */
                if ((npc.transform.position.x - transform.position.x).Abs() < npc.interactionSize.x &&
                    (npc.transform.position.y - transform.position.y).Abs() < npc.interactionSize.y &&
                    npc.mainCollider.IsInCollider(point))
                {
                    npc.PlayerInteraction(this);
                    return;
                }
            }

            //与方块交互
            if (InUseRadius(point) &&
                map.TryGetBlock(PosConvert.WorldToMapPos(point), isControllingBackground, out Block block) &&
                block.PlayerInteraction(this))
            {

            }
            //使用物品
            else
            {
                ItemBehaviour usingItemBehaviour = TryGetUsingItemBehaviour();

                if (usingItemBehaviour != null) usingItemBehaviour.Use(point);
            }
        }


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
            deathTimer = Tools.time + 25;

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




        public void OnNameChange(string newValue)
        {
            //销毁原先的 nameText
            if (nameText && nameText.gameObject)
                Destroy(nameText.gameObject);

            //初始化新的 nameText
            nameText = GameUI.AddText(UPC.Middle, "ori:player_name_" + newValue, playerCanvas);
            nameText.rectTransform.AddLocalPosY(30f);
            nameText.text.SetFontSize(7);
            nameText.text.text = newValue;
            nameText.autoCompareText = false;
        }

        /// <returns>鼠标是否在使用范围内</returns>
        public bool InUseRadius(Vector2 vec) => InUseRadius(transform.position, vec);

        public bool InUseRadius(Vector2 vec1, Vector2 vec2) => Vector2.Distance(vec1, vec2) <= useRadius;

        #region 挖掘方块
        private void ExcavateBlock(Block block)
        {
            if (!isLocalPlayer || block == null)
                return;

            if (block != null)
            {
                //播放挖掘动画
                if (!animWeb.GetAnim("attack_rightarm", 0).isPlaying)
                    animWeb.SwitchPlayingTo("attack_rightarm");

                //让方块扣血
                block.TakeDamage(excavationStrength);

                //播放音效
                GAudio.Play(AudioID.ExcavatingBlock);

                //手柄震动
                if (GControls.mode == ControlMode.Gamepad)
                    GControls.GamepadVibrationSlighter(0.1f);
            }

            //设置时间
            itemUseTime = Tools.time;
        }
        #endregion


        #region 生成区域

        /// <summary>
        /// 告诉服务器要生成, 并让服务器生成 (隐性), 然后在生成好后传给客户端
        /// </summary>
        [Button]
        public void ServerGenerateRegion(Vector2Int index, bool isFirstGeneration, string specificTheme = null)
        {
            //检查是否正在生成
            if (regionGenerationSaves != null || regionGenerationRegion != null)
            {
                Debug.LogError("正在生成区域, 请等待");
                return;
            }

            //初始化资源
            askingForGeneratingRegion = true;
            regionGenerationSaves = new();

            //向服务器发送请求
            ServerGenerateRegionCore(index, isFirstGeneration, specificTheme);

            //等待服务器发回所有资源
            StartCoroutine(IEWaitForRegionSegments());
        }

        IEnumerator IEWaitForRegionSegments()
        {
            yield return new WaitUntil(() => regionGenerationRegion != null);
            yield return new WaitUntil(() => regionGenerationSaves.Count == regionGenerationBlocksCount);


            //恢复方块数据 (浅拷贝时方块数据被清除了)
            regionGenerationRegion.blocks = regionGenerationSaves;


            //如果是服务器的话要恢复实体数据 (浅拷贝时实体数据被清除了)
            if (isServer)
                regionGenerationRegion.entities = GetRegionToGenerate(regionGenerationRegion.index).entities;


            Debug.Log($"收到服务器回调, 正在生成已有区域 {regionGenerationRegion.index}");


            //生成出区域
            GM.instance.GenerateExistingRegion(regionGenerationRegion, () =>
            {
                if (regionGenerationIsFirstGeneration)
                {
                    //如果先前没获取过位置, 则将玩家的位置设置到该区域出生点
                    if (regionGenerationRegion.spawnPoint != Vector2Int.zero)
                        transform.position = regionGenerationRegion.spawnPoint.To2();

                    fallenY = transform.position.y;
                    generatedFirstRegion = true;
                }

                //清理资源
                askingForGeneratingRegion = false;
                regionGenerationRegion = null;
                regionGenerationSaves = null;
                //下面的参数: 如果是 首次中心生成 就快一点, 否则慢一些防止卡顿
            }, null, (ushort)(GFiles.settings.performanceLevel * (regionGenerationIsFirstGeneration ? 4 : 0.8f)));
        }

        public Region GetRegionToGenerate(Vector2Int index)
        {
            lock (GFiles.world.regionData)
            {
                foreach (Region region in GFiles.world.regionData)
                {
                    if (region.index == index && region.generatedAlready)
                    {
                        return region;
                    }
                }
            }

            return null;
        }

        [ServerRpc]
        private void ServerGenerateRegionCore(Vector2Int index, bool isFirstGeneration, string specificTheme, NetworkConnection caller = null)
        {
            Debug.Log($"Player={netId} 请求生成区域 {index}");

            MethodAgent.RunThread(() =>
            {
                //如果没有则生成新的区域
                GM.instance.GenerateNewRegion(index, specificTheme);

                //获取生成好的区域
                var regionToGenerate = GetRegionToGenerate(index) ?? throw new();

                //这些代码必须在主线程里执行
                MethodAgent.RunOnMainThread(() =>
                {
                    //? 因为直接发送一个 region 实在太大了，所以我们不得不将他们分成一个个的 BlockSave
                    void ConnectionGenerate()
                    {
                        //? 浅拷贝区域
                        var copy = regionToGenerate.ShallowCopy();
                        copy.blocks = null;
                        copy.entities = new();
                        if (hasSetPosBySave)
                            copy.spawnPoint = Vector2Int.zero;

                        ConnectionGenerateRegionTotal(copy, regionToGenerate.blocks.Count, isFirstGeneration, caller);

                        foreach (var segment in regionToGenerate.blocks)
                        {
                            ConnectionGenerateRegion(segment, caller);
                        }
                    }

                    //* 如果是服务器发送的申请: 服务器生成
                    if (isLocalPlayer)
                    {
                        ConnectionGenerate();
                    }
                    //* 如果是客户端发送的申请: 服务器先生成->客户端生成   (如果 服务器和客户端 并行生成, 可能会导致 bug)
                    else
                    {
                        GM.instance.GenerateExistingRegion(
                                regionToGenerate,
                                () => ConnectionGenerate(),
                                () => ConnectionGenerate(),
                                (ushort)(GFiles.settings.performanceLevel / 2)
                            );
                    }
                });
            });
        }


        [ConnectionRpc]
        void ConnectionGenerateRegionTotal(Region regionToGenerate, int regionToGenerateBlocksCount, bool isFirstGeneration, NetworkConnection caller)
        {
            regionGenerationRegion = regionToGenerate;
            regionGenerationBlocksCount = regionToGenerateBlocksCount;
            regionGenerationIsFirstGeneration = isFirstGeneration;
        }

        [ConnectionRpc]
        void ConnectionGenerateRegion(BlockSave block, NetworkConnection caller)
        {
            regionGenerationSaves.Add(block);
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



        #region 移动和转向
        public override Vector2 GetMovementDirection()
        {
            float move = (!PlayerCanControl(this) || isDead) ? 0 : PlayerControls.Move(this);

            //执行 移动的启停
            if (move == 0 && moveVecLastFrame != 0)
            {
                isMoving = false;
            }
            else if (move != 0 && moveVecLastFrame == 0)
            {
                isMoving = true;
            }

            //用于在下一帧检测是不是刚刚停止或开始移动
            moveVecLastFrame = move;

            Vector2 result = new(move, 0);

            //地面摩擦
            if (isOnGround && move == 0 && rb.velocity.x != 0)
            {
                result = new(result.x - rb.velocity.x * blockFriction, result.y);
            }

            return result;
        }

        public void AutoSetPlayerOrientation()
        {
            ////诸如 && transform.localScale.x.Sign() == 1 之类的检测是为了减缓服务器压力
            if (isDead)
            {
                SetOrientation(true);
                return;
            }

            switch (GControls.mode)
            {
                //* 如果是键鼠, 则检测鼠标和玩家的相对位置
                case ControlMode.KeyboardAndMouse:
                    float delta = GControls.mousePos.ToWorldPos().x - transform.position.x;

                    if (delta < 0)
                        SetOrientation(false);
                    else if (delta > 0)
                        SetOrientation(true);

                    break;

                //* 如果是触摸屏, 则检测光标和玩家的相对位置
                case ControlMode.Touchscreen:
                    if (pui.touchScreenMoveJoystick && pui.touchScreenCursorJoystick)
                    {
                        if (pui.touchScreenCursorImage.rt.localPosition.x < transform.position.x)
                            SetOrientation(false);
                        else if (pui.touchScreenCursorImage.rt.localPosition.x > transform.position.x)
                            SetOrientation(true);
                    }
                    break;

                //* 如果是手柄, 则检测左摇杆
                case ControlMode.Gamepad:
                    float x = PlayerControls.Move(this);

                    if (x < 0)
                        SetOrientation(false);
                    else if (x > 0)
                        SetOrientation(true);

                    break;
            }
        }
        #endregion

        #region 攻击
        public Vector2 cursorWorldPos
        {
            get
            {
                return GControls.mode switch
                {
                    //* 如果是触摸屏, 返回光标位置
                    ControlMode.Touchscreen => pui.touchScreenCursorImage.rectTransform.position,

                    //* 如果是键鼠, 返回鼠标位置
                    ControlMode.KeyboardAndMouse => Tools.instance.GetMouseWorldPos(),

                    //* 如果是手柄, 返回虚拟光标位置
                    ControlMode.Gamepad => (Vector2)Tools.instance.mainCamera.ScreenToWorldPoint(VirtualCursor.instance.image.ap),

                    _ => Vector2.zero,
                };
            }
        }

        public void OnHoldAttack()
        {
            if (!HasUseCDPast())
                return;

            //如果 鼠标在挖掘范围内 && 在鼠标位置获取到方块 && 方块是激活的 && 方块不是液体
            if (InUseRadius(cursorWorldPos) &&
                map.TryGetBlock(PosConvert.WorldToMapPos(cursorWorldPos), isControllingBackground, out Block block) &&
                block.gameObject.activeInHierarchy &&
                !block.data.GetTag("ori:liquid").hasTag)
            {
                ExcavateBlock(block);
            }
            //如果是刚刚点的攻击或者是触摸屏模式（触摸屏摇杆）
            else if (PlayerControls.ClickingAttack(this) || GControls.mode == ControlMode.Touchscreen)
            {
                OnStartAttack();

                itemUseTime = Tools.time;
            }
        }

        public override void OnStartAttack() => OnStartAttack(cursorWorldPos, false, true);

        public void OnStartAttack(Vector2 point, bool left, bool right)
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

                    //这里的作用是获得攻击目标  [先尝试获取 Entity, 再尝试获取 CreatureBodyPart]
                    if (!ray.transform.TryGetComponent<Entity>(out var target) || target.hurtable)
                    {
                        if (ray.transform.TryGetComponent<CreatureBodyPart>(out var cbp) && target.hurtable)
                        {
                            target = cbp.mainBody;
                        }
                    }

                    //如果成功获取了目标
                    if (target)
                    {
                        int damage = TryGetUsingItem()?.data?.damage ?? ItemData.defaultDamage;
                        target.TakeDamage(damage, 0.3f, transform.position, transform.position.x < target.transform.position.x ? Vector2.right * 12 : Vector2.left * 12);

                        //如果使用手柄就震动一下
                        if (GControls.mode == ControlMode.Gamepad)
                            GControls.GamepadVibrationMedium();

                        break;
                    }
                }
            }

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





        /* -------------------------------------------------------------------------- */
        /*                                    静态方法                                    */
        /* -------------------------------------------------------------------------- */

        public sealed class RecipeIngredientsTable
        {
            public CraftingRecipe recipe;
            public List<Dictionary<int, ushort>> ingredients;

            public RecipeIngredientsTable(CraftingRecipe recipe, List<Dictionary<int, ushort>> ingredients)
            {
                this.recipe = recipe;
                this.ingredients = ingredients;
            }
        }

        public static List<RecipeIngredientsTable> GetCraftingRecipesThatCanBeCrafted(Item[] items)
        {
            List<RecipeIngredientsTable> results = new();

            ModFactory.mods.For(mod => mod.craftingRecipes.For(recipe =>
            {
                if (recipe.WhetherCanBeCrafted(items, out var ingredients))
                {
                    results.Add(new(recipe, ingredients));
                }
            }));

            return results;
        }

        public static List<CraftingRecipe> GetCraftingRecipesThatCannotBeCrafted(Item[] items)
        {
            List<CraftingRecipe> results = new();

            ModFactory.mods.For(mod => mod.craftingRecipes.For(recipe =>
            {
                if (!recipe.WhetherCanBeCrafted(items, out var ingredients))
                {
                    results.Add(recipe);
                }
            }));

            return results;
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

    public sealed class PlayerSkin
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
        public static float playerHealthUpTimer;
        public static float playerHungerHurtTimer;

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
                var frameTime = Performance.frameTime;

                foreach (var player in all)
                {
                    if (player.isDead)
                        continue;

                    bool isMoving = player.isMoving;
                    float hungerValue = player.hungerValue;
                    float happinessValue = player.happinessValue;
                    int health = player.health;

                    float hungerValueDelta = frameTime / 100;
                    if (isMoving) hungerValueDelta += frameTime / 40;
                    player.hungerValue = hungerValue - hungerValueDelta;

                    float happinessValueDelta = frameTime / 25;
                    if (isMoving) happinessValueDelta += frameTime / 10;
                    if (hungerValue <= 30) happinessValueDelta += frameTime / 20;
                    player.happinessValue = happinessValue - happinessValueDelta;

                    //一秒回一次血
                    if (Tools.time >= playerHealthUpTimer)
                    {
                        //受伤的八秒内不回血
                        if (health < 100 && Tools.time > player.previousHurtTime + 8)
                        {
                            playerHealthUpTimer = Tools.time + 1f;
                            player.health = health + 1;
                        }
                    }

                    //每三秒扣一次血
                    if (Tools.time >= playerHungerHurtTimer)
                    {
                        playerHungerHurtTimer = Tools.time + 5;

                        if (hungerValue <= 0)
                            player.TakeDamage(5);
                    }
                }
            }
        }
    }
}
