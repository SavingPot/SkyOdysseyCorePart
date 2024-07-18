using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.High;
using GameCore.UI;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;
using static GameCore.UI.PlayerUI;
using GameCore.Network;
using ClientRpcAttribute = GameCore.Network.ClientRpcAttribute;

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

        public int TotalDefense
        {
            get
            {
                int totalDefense = 0;
                totalDefense += inventory?.helmet?.data?.Helmet?.defense ?? 0;
                totalDefense += inventory?.breastplate?.data?.Breastplate?.defense ?? 0;
                totalDefense += inventory?.legging?.data?.Legging?.defense ?? 0;
                totalDefense += inventory?.boots?.data?.Boots?.defense ?? 0;
                return totalDefense;
            }
        }





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
                    //先通过 ID 查询任务
                    if (TaskNameTable.ContainsKey(item.data.id))
                    {
                        pui.CompleteTask(TaskNameTable[item.data.id]);
                    }
                    else
                    {
                        //如果没有符合的 ID，就通过标签查询任务
                        item.data.tags.ForEach(tag =>
                        {
                            if (TaskTagTable.ContainsKey(tag))
                            {
                                pui.CompleteTask(TaskTagTable[tag]);
                            }
                        });
                    }
                }
            }
        }











        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        [LabelText("是否控制背景"), BoxGroup("状态")] public bool isControllingBackground;
        [BoxGroup("属性"), LabelText("重力")] public float gravity;
        [BoxGroup("属性"), LabelText("重生计时器"), HideInInspector] public float rebornTimer;
        [BoxGroup("属性"), LabelText("方块摩擦力")] public float blockFriction = 0.96f;
        public bool isAttacking { get; private set; }
        public float playerCameraScale { get; private set; } = 1;





        public bool isUnlockingRegion { get; private set; } = false;
        public (SpriteRenderer sr, TextImageIdentity ti)[] unlockedRegionColorRenderers { get; private set; }
        public Transform unlockedRegionCameraFollowTarget;
        void FadeRegionUnlockingRenderer(SpriteRenderer sr)
        {
            sr.DOFade(0, 0.5f).OnComplete(() => sr.gameObject.SetActive(false));
        }
        void RefreshRegionUnlockingRenderers()
        {
            //渲染器
            Render(0, Vector2Int.up);
            Render(1, Vector2Int.down);
            Render(2, Vector2Int.left);
            Render(3, Vector2Int.right);

            void Render(int index, Vector2Int regionIndexDelta)
            {
                var targetIndex = regionIndex + regionIndexDelta;
                var (sr, ti) = unlockedRegionColorRenderers[index];

                //TODO: 客户端的世界为空，这里需要进行一些处理
                //如果这个区域已经解锁了，就直接返回
                if (GFiles.world.TryGetRegion(targetIndex, out _) || GM.instance.generatingNewRegions.Contains(targetIndex))
                {
                    FadeRegionUnlockingRenderer(sr);
                }
                else
                {
                    sr.color = new(1, 1, 1, 0.8f);
                    sr.gameObject.SetActive(true);
                    sr.transform.position = Region.GetMiddle(targetIndex);
                    ti.SetText(GM.GetRegionUnlockingCost(targetIndex));
                }
            }
        }
        internal void RefreshUnlockingRegion()
        {
            //渲染器
            RefreshRegionUnlockingRenderers();

            //相机跟随
            unlockedRegionCameraFollowTarget.position = Region.GetMiddle(regionIndex);
            Tools.instance.mainCameraController.lookAt = unlockedRegionCameraFollowTarget;
        }









        #region 区域生成

        public bool hasGeneratedFirstRegion;
        private bool hasSetPosBySave;
        Coroutine coroutineWaitingForRegionSegments;
        public bool isAskingForGeneratingRegion { get; private set; }
        bool regionGeneration_isFirstGeneration;
        List<BlockSave> regionGeneration_blockSaves = null;
        int regionGeneration_blocksCount;
        Region regionGeneration_region = null;

        #endregion



        #region 同步变量

        [Sync] public int coin;
        [Sync] public Sprite skinHead;
        [Sync] public Sprite skinBody;
        [Sync] public Sprite skinLeftArm;
        [Sync] public Sprite skinRightArm;
        [Sync] public Sprite skinLeftLeg;
        [Sync] public Sprite skinRightLeg;
        [Sync] public Sprite skinLeftFoot;
        [Sync] public Sprite skinRightFoot;
        [Sync] public float hungerValue;
        public static float defaultHungerValue = 100;
        public static float maxHungerValue = 100;
        [Sync] public float mana;
        public static float defaultMana = 50;
        public static float maxMana = 100;

        #region 任务
        [Sync] public List<TaskStatusForSave> completedTasks;

        public void AddCompletedTasks(TaskStatusForSave task)
        {
            var tasksTemp = completedTasks;
            tasksTemp.Add(task);
            completedTasks = tasksTemp;
        }
        #endregion

        #region 技能树
        [Sync] public List<SkillStatusForSave> unlockedSkills;
        [Sync] public uint skillPoints;

        public void AddUnlockedSkills(SkillStatusForSave task)
        {
            var skillsTemp = unlockedSkills;
            skillsTemp.Add(task);
            unlockedSkills = skillsTemp;
        }
        #endregion

        #region 玩家名
        [Sync(nameof(OnNameChangeMethod)), SyncDefaultValue("")] public string playerName;

        void OnNameChangeMethod(byte[] _) => OnNameChange(playerName);

        public void OnNameChange(string newName)
        {
            //销毁原先的 nameText
            if (nameText && nameText.gameObject)
                Destroy(nameText.gameObject);

            //初始化新的 nameText
            nameText = GameUI.AddText(UIA.Middle, "ori:player_name_" + newName, playerCanvas);
            nameText.rectTransform.AddLocalPosY(30f);
            nameText.text.SetFontSize(7);
            nameText.text.text = newName;
            nameText.autoCompareText = false;
        }
        #endregion

        #region 物品栏
        [Sync(nameof(TempInventory))] public Inventory inventory;

        void TempInventory(byte[] _)
        {
            inventory = Inventory.ResumeFromStreamTransport(inventory, this);

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

        public bool TryGetUsingItem(out Item item)
        {
            item = GetUsingItemChecked();

            return !Item.Null(item);
        }
        public Item GetUsingItemChecked() => inventory.GetItemChecked(usingItemIndex);
        public ItemBehaviour GetUsingItemBehaviourChecked() => inventory.GetItemBehaviourChecked(usingItemIndex);

        #endregion

        #endregion



        #region 物品属性

        public float excavationStrength => TryGetUsingItem(out var item) ? item.data.excavationStrength : ItemData.defaultExcavationStrength;

        [BoxGroup("属性"), LabelText("使用时间")]
        public float itemUseTime;
        public float previousAttackTime;
        public float interactiveRadius => defaultInteractiveRadius + (TryGetUsingItem(out var item) ? item.data.extraDistance : 0);





        [HideInInspector] public SpriteRenderer usingItemRenderer { get; set; }
        [HideInInspector] public BoxCollider2D usingItemCollider { get; set; }
        [HideInInspector] public InventoryItemRendererCollision usingItemCollisionComponent { get; set; }

        public void ModifyUsingItemRendererTransform(Vector2 localPosition, Vector2 localScale, int localRotation)
        {
            localScale *= 0.5f;
            localPosition += new Vector2(0.1f, -0.5f);

            usingItemRenderer.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0, 0, localRotation));
            usingItemRenderer.transform.SetScale(localScale);

            //碰撞体
            usingItemCollider.size = localScale;
        }

        #endregion




        /* -------------------------------------------------------------------------- */
        /*                                    临时数据                                    */
        /* -------------------------------------------------------------------------- */
        private readonly Collider2D[] itemPickUpObjectsDetectedTemp = new Collider2D[40];
        public PlayerController playerController;
        private void UpdateController(ControlMode newMode)
        {
            playerController = ControlModeToController(this, newMode);
        }





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static int playerLayer { get; private set; }
        public static int playerLayerMask { get; private set; }
        public static float itemPickUpRadius = 1.8f;
        public static int quickInventorySlotCount = 8;   //偶数
        public static int halfQuickInventorySlotCount = quickInventorySlotCount / 2;
        public static Func<Player, bool> PlayerCanControl = player => GameUI.page == null || !GameUI.page.ui && player.hasGeneratedFirstRegion && Application.isFocused;
        public const float playerDefaultGravity = 6f;
        public const float defaultInteractiveRadius = 2.8f;
        public const float REBORN_WAIT_TIME = 20;

        public static Quaternion deathQuaternion = Quaternion.Euler(0, 0, 90);
        public static float deathLowestColorFloat = 0.45f;
        public static float oneMinusDeathLowestColorFloat = 1 - deathLowestColorFloat;
        public static Color deathLowestColor = new(deathLowestColorFloat, deathLowestColorFloat, deathLowestColorFloat);

        public static float enoughSavingTime = 5.5f;



        public static readonly Dictionary<string, string> TaskNameTable = new()
        {
            { BlockID.Dirt, "ori:get_dirt" },
            { ItemID.FeatherWing, "ori:get_feather_wing" },
            { BlockID.Grass, "ori:get_grass" },
            { ItemID.StrawRope, "ori:get_straw_rope" },
            { ItemID.PlantFiber, "ori:get_plant_fiber" },
            { BlockID.Gravel, "ori:get_gravel" },
            { ItemID.Flint, "ori:get_flint" },
            { BlockID.Stone, "ori:get_stone" },
            { BlockID.Campfire, "ori:get_campfire" },
            { ItemID.FlintKnife, "ori:get_flint_knife" },
            { ItemID.FlintHoe, "ori:get_flint_hoe" },
            { ItemID.FlintSword, "ori:get_flint_sword" },
            { ItemID.IronKnife, "ori:get_iron_knife" },
            { ItemID.IronHoe, "ori:get_iron_hoe" },
            { ItemID.IronSword, "ori:get_iron_sword" },
            { ItemID.Bark, "ori:get_bark" },
            { ItemID.BarkVest, "ori:get_bark_vest" },
            { ItemID.Stick, "ori:get_stick" },
            { ItemID.Potato, "ori:get_potato" },
            { ItemID.Onion, "ori:get_onion" },
            { ItemID.Watermelon, "ori:get_watermelon" },
        };

        public static readonly Dictionary<string, string> TaskTagTable = new()
        {
            { "ori:log", "ori:get_log" },
            { "ori:meat", "ori:get_meat" },
            { "ori:egg", "ori:get_egg" },
            { "ori:feather", "ori:get_feather" },
            { "ori:planks", "ori:get_planks" },
        };







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











        #region 生命周期

        protected override void Awake()
        {
            base.Awake();

            //计算移动速度因数
            Func<float> oldFactor = velocityFactor;
            velocityFactor = () => oldFactor() * (transform.localScale.x.Sign() != rb.velocity.x.Sign() ? 0.75f : 1);

            playerCanvas = transform.Find("Canvas");



            //控制器
            GControls.OnModeChanged += UpdateController;
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
                    regionIndex = PosConvert.WorldPosToRegionIndex(Init.save.pos); //这里必须立刻设置 regionIndex，否则会导致 OnRegionIndexChanged 被调用
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

            #endregion



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
                Tools.instance.mainCameraController.cameraScale.Add(() => playerCameraScale);


                unlockedRegionColorRenderers = new (SpriteRenderer, TextImageIdentity)[] {
                    GenerateUnlockedRegionColorRenderers(),
                    GenerateUnlockedRegionColorRenderers(),
                    GenerateUnlockedRegionColorRenderers(),
                    GenerateUnlockedRegionColorRenderers(),
                };
                unlockedRegionCameraFollowTarget = new GameObject("DO_NOT_Modify-UnlockedRegionCameraFollowTarget").transform;

                static (SpriteRenderer, TextImageIdentity) GenerateUnlockedRegionColorRenderers()
                {
                    //渲染器
                    var sr = new GameObject().AddComponent<SpriteRenderer>();
                    var scale = Region.chunkCount * Chunk.blockCountPerAxis * 0.9f;
                    sr.sprite = ModFactory.CompareTexture("ori:unlocked_region_color").sprite;
                    sr.color = new(1, 1, 1, 0.8f);
                    sr.sortingOrder = 100;
                    sr.transform.localScale = new(scale, scale);
                    sr.gameObject.SetActive(false);

                    //画布
                    var canvas = GameUI.AddWorldSpaceCanvas(sr.transform);
                    canvas.GetComponent<RectTransform>().sizeDelta = Vector3.zero;
                    canvas.sortingOrder = 101;
                    canvas.transform.localScale = new(1 / scale, 1 / scale, 0);

                    var textImage = GameUI.AddTextImage(UIA.Middle, $"ori:text_image.unlocked_region_color.{Tools.randomGUID}", "ori:coin", canvas.transform);
                    textImage.SetSizeDeltaBoth(50, 50);
                    textImage.SetTextAttach(TextImageIdentity.TextAttach.Right);
                    textImage.text.doRefresh = false;
                    textImage.SetAPosX(-textImage.sd.x / 2);

                    return (sr, textImage);
                }



                Debug.Log("本地客户端玩家是: " + gameObject.name, gameObject);

                managerGame.weatherParticle.transform.SetParent(transform);
                managerGame.weatherParticle.transform.localPosition = new(0, 40);




                //初始化控制
                UpdateController(GControls.mode);
            }



            BindHumanAnimations(this);


            //显示名称
            OnNameChange(playerName);

            //防止玩家在生成区域前就掉落到很低的地方
            GravitySet(this);
        }

        public override void AfterInitialization()
        {
            base.AfterInitialization();

            PlayerCenter.AddPlayer(this);

            //生成地图
            GenerateRegion(regionIndex, true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            PlayerUI.instance = null;
            animWeb.Stop();

            PlayerCenter.RemovePlayer(this);


            //控制器
            GControls.OnModeChanged -= UpdateController;
        }




        protected override void Update()
        {
            base.Update();

            if (isLocalPlayer)
                LocalUpdate();

            EntityInventoryOwnerBehaviour.OnUpdate(this);

#if DEBUG
            if (Keyboard.current?.spaceKey?.wasPressedThisFrame ?? false)
                rebornTimer = 0;
#endif
        }

        private void AliveLocalUpdate()
        {
            /* ----------------------------------- 背包 (不能放在 PlayerCanControl 里是因为打开背包后 PlayerCanControl 返回 false, 然后就关不掉背包了) ----------------------------------- */
            if (playerController.Backpack())
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
                if (isOnGround && playerController.Jump())
                    rb.SetVelocityY(GetJumpVelocity(30));


                /* ----------------------------------- 挖掘与攻击 ----------------------------------- */
                if (playerController.HoldingAttack())  //检测物品的使用CD
                {
                    OnHoldAttack();
                }


                /* ---------------------------------- 抛弃物品 ---------------------------------- */
                if (playerController.ThrowItem())
                    ServerThrowItem(usingItemIndex.ToString(), 1);

#if DEBUG
                /* --------------------------------- 摄像机调试器 --------------------------------- */
                if (Keyboard.current.uKey.wasPressedThisFrame)
                {
                    playerCameraScale = 1;
                }
                if (Keyboard.current.iKey.wasPressedThisFrame)
                {
                    playerCameraScale *= 0.5f;
                }
                if (Keyboard.current.oKey.wasPressedThisFrame)
                {
                    playerCameraScale *= 2;
                }
#endif

                /* ---------------------------------- 检查房屋 ---------------------------------- */
                if (Keyboard.current.gKey.wasPressedThisFrame)
                {
                    var pos = PosConvert.WorldToMapPos(cursorWorldPos);
                    var roomCheck = new MapUtils.RoomCheck(pos);
                    Debug.Log(roomCheck.IsValidConstruction() + "   Score: " + roomCheck.ScoreRoom());
                }

                /* ---------------------------------- 解锁区域 ---------------------------------- */
                if (Keyboard.current.pKey.wasPressedThisFrame)
                {
                    if (!isUnlockingRegion)
                    {
                        isUnlockingRegion = true;

                        //刷新解锁区域的渲染器
                        RefreshUnlockingRegion();

                        //相机缩放
                        DOTween.To(() => playerCameraScale, v => playerCameraScale = v, 0.06f, 0.7f).SetEase(Ease.InOutSine);
                    }
                    else
                    {
                        isUnlockingRegion = false;

                        //渲染器
                        foreach (var (sr, _) in unlockedRegionColorRenderers) FadeRegionUnlockingRenderer(sr);

                        //相机跟随
                        Tools.instance.mainCameraController.lookAt = transform;

                        //相机缩放
                        DOTween.To(() => playerCameraScale, v => playerCameraScale = v, 1, 0.7f).SetEase(Ease.InOutSine);
                    }
                }
                //TODO: 左下角显示技能点数
                if (isUnlockingRegion)
                {
                    void UnlockRegion(Vector2Int targetIndex)
                    {
                        var cost = GM.GetRegionUnlockingCost(targetIndex);

                        if (coin < cost)
                        {
                            InternalUIAdder.instance.SetStatusText("金币不足!");
                            return;
                        }

                        coin -= cost;
                        GenerateRegion(targetIndex, false);
                        RefreshRegionUnlockingRenderers();
                        ServerDestroyRegionBarriers(targetIndex);
                    }

                    if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    {
                        UnlockRegion(regionIndex + Vector2Int.up);
                    }
                    if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                    {
                        UnlockRegion(regionIndex + Vector2Int.down);
                    }
                    if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    {
                        UnlockRegion(regionIndex + Vector2Int.left);
                    }
                    if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    {
                        UnlockRegion(regionIndex + Vector2Int.right);
                    }
                }


                /* ----------------------------------- 睡眠 ----------------------------------- */
                if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                {
                    //TODO: 睡眠
                }

                #region 切换物品

                void FuncSwitchItem(Func<bool> funcCall, int ii)
                {
                    if (!funcCall())
                        return;

                    SwitchItem(ii - 1);
                }

                FuncSwitchItem(playerController.SwitchToItem1, 1);
                FuncSwitchItem(playerController.SwitchToItem2, 2);
                FuncSwitchItem(playerController.SwitchToItem3, 3);
                FuncSwitchItem(playerController.SwitchToItem4, 4);
                FuncSwitchItem(playerController.SwitchToItem5, 5);
                FuncSwitchItem(playerController.SwitchToItem6, 6);
                FuncSwitchItem(playerController.SwitchToItem7, 7);
                FuncSwitchItem(playerController.SwitchToItem8, 8);

                if (playerController.SwitchToPreviousItem())
                {
                    int value = usingItemIndex - 1;

                    if (value < 0)
                        value = quickInventorySlotCount - 1;

                    SwitchItem(value);
                }
                else if (playerController.SwitchToNextItem())
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

                        Interact(touchWorldPos);
                    }
                }

                //如果按右键
                if (playerController.Interact())
                {
                    Interact(cursorWorldPos);
                }

                //在脚底下放方块
                if (playerController.PlaceBlockUnderPlayer())
                {
                    var usingItem = GetUsingItemChecked();
                    var behaviour = GetUsingItemBehaviourChecked();

                    if (usingItem != null && behaviour != null && usingItem.data.isBlock)
                    {
                        var downPoint = mainCollider.DownPoint();
                        behaviour.UseAsBlock(PosConvert.WorldToMapPos(new(downPoint.x, downPoint.y - 1)), false);
                    }
                }

                isControllingBackground = playerController.IsControllingBackground();
            }

            //血量低于 15 就播放心跳声 (死了不播放声音)
            if (health <= 15)
            {
                GAudio.Play(AudioID.Heartbeat, true);
            }
        }

        private void LocalUpdate()
        {
            GravitySet(this);
            rb.gravityScale = gravity;
            SetOrientationByControl();
            isAttacking = Tools.time <= previousAttackTime + attackAnimTime;

            if (!isDead)
            {
                AliveLocalUpdate();

                transform.localRotation = Quaternion.identity;
            }
            else
            {
                transform.localRotation = deathQuaternion;
            }

            //刷新状态栏
            pui?.Update();

            //如果当前区域不存在就生成
            if (!GM.instance.generatedExistingRegions.Exists(p => p.index == regionIndex))
            {
                GenerateExistingRegion(regionIndex);
            }
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();




            /* -------------------------------------------------------------------------- */
            /*                                    拾取物品                                    */
            /* -------------------------------------------------------------------------- */
            Physics2D.OverlapCircleNonAlloc(transform.position, itemPickUpRadius, itemPickUpObjectsDetectedTemp);

            foreach (var other in itemPickUpObjectsDetectedTemp)
            {
                if (other == null)
                    break;

                if (other.TryGetComponent<Drop>(out var drop) && drop.CanBePickedUp())
                {
                    //检测有没有栏位存放物品
                    if (!Inventory.GetIndexesToPutItemIntoItems(inventory.slots, drop.item, out _))
                        return;

                    ServerAddItem(drop.item);
                    GAudio.Play(AudioID.PickUpItem);

                    drop.Death();
                }
                else if (other.TryGetComponent<CoinEntity>(out var coinEntity) && coinEntity.CanBePickedUp())
                {
                    ServerAddCoin(coinEntity.coinCount);
                    GAudio.Play(AudioID.PickUpItem);

                    coinEntity.Death();
                }
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
            Gizmos.DrawWireSphere(transform.position, interactiveRadius);
        }

#endif
        #endregion






        #region Base 覆写


        public override void OnDeathServer()
        {
            //如果有其他玩家存活
            if (Server.playerCount > 1 && !PlayerCenter.all.Any(p => !p.isDead))
            {
                Debug.Log("有别人");
                return;
            }

            Debug.Log("没别人了");
        }

        public override void OnDeathClient()
        {
            rebornTimer = Tools.time + REBORN_WAIT_TIME;

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






        /* -------------------------------------------------------------------------- */
        /*                                     重生逻辑                                     */
        /* -------------------------------------------------------------------------- */
        public void Reborn(int newHealth, Vector2? newPos)
        {
            ServerReborn(newHealth, newPos ?? new(float.PositiveInfinity, float.NegativeInfinity), null);
        }

        [ServerRpc]
        void ServerReborn(int newHealth, Vector2 newPos, NetworkConnection caller)
        {
            //刷新属性
            health = newHealth;
            isDead = false;

            OnRebornServer(newHealth, newPos, caller);

            //如果数值无效, 则使用默认的重生点
            if (float.IsInfinity(newPos.x) || float.IsInfinity(newPos.y))
                newPos = GFiles.world.GetRegion(regionIndex)?.spawnPoint ?? Vector2Int.zero;

            ClientReborn(newHealth, newPos, caller);
        }

        [ClientRpc]
        void ClientReborn(int newHealth, Vector2 newPos, NetworkConnection caller)
        {
            OnRebornClient(newHealth, newPos, caller);

            //玩家重生时, 由对应客户端设置位置
            transform.position = newPos;
        }

        void OnRebornServer(float newHealth, Vector2 newPos, NetworkConnection caller)
        {
            hungerValue = 30;
        }

        void OnRebornClient(float newHealth, Vector2 newPos, NetworkConnection caller)
        {
            //设置颜色
            foreach (var sr in spriteRenderers)
            {
                sr.color = Color.white;
            }

            if (isLocalPlayer)
            {
                //防止玩家重生时摔死
                fallenY = newPos.y;

                //关闭 UI
                MethodAgent.CallUntil(() => pui != null, () => pui.rebornPanel.gameObject.SetActive(false));
            }
        }






        public override void OnGetHurtServer(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }

        public override void OnGetHurtClient(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            if (GControls.mode == ControlMode.Gamepad)
                GControls.GamepadVibrationMediumStrong();
        }






        public override void WriteToEntitySave(EntitySave save)
        {
            base.WriteToEntitySave(save);

            if (save is PlayerSave trueSave)
            {
                trueSave.WriteFromPlayer(this);
            }
            else
            {
                throw new();
            }
        }

        #endregion





        public static Action<Player> GravitySet = caller =>
        {
            if (!caller.hasGeneratedFirstRegion ||
                !GM.instance.generatedExistingRegions.Any(p => p.index == PosConvert.WorldPosToRegionIndex(caller.transform.position)))  //获取区域序号不用 caller.regionIndex 是因为只有区域加载成功, caller.regionIndex才会正式改编
            {
                caller.gravity = 0;
                return;
            }

            caller.gravity = playerDefaultGravity;
        };

        [ServerRpc, Button]
        public void ServerAddCoin(int count, NetworkConnection caller = null)
        {
            coin += count;

            Debug.Log("ADD COIN " + count);
        }

        /// <returns>一个点是否在交互范围内</returns>
        public bool IsPointInteractable(Vector2 vec) => IsPointInteractable(transform.position, vec);

        public bool IsPointInteractable(Vector2 vec1, Vector2 vec2) => Vector2.Distance(vec1, vec2) <= interactiveRadius;





        #region 挖掘方块

        private void ExcavateBlock(Block block)
        {
            if (!isLocalPlayer || block == null)
                return;

            if (block != null)
            {
                //播放挖掘动画
                if (!animWeb.GetAnim("attack_rightarm", 0).isPlaying)
                    animWeb.SwitchPlayingTo("attack_rightarm", 0);

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
        public void GenerateExistingRegion(Vector2Int index)
        {
            //检查是否正在生成
            if (regionGeneration_blockSaves != null || regionGeneration_region != null)
            {
                return;
            }

            //初始化资源
            isAskingForGeneratingRegion = true;
            regionGeneration_blockSaves = new();

            //向服务器发送请求
            ServerGenerateExistingRegion(index);

            //等待服务器发回所有资源切片
            coroutineWaitingForRegionSegments = StartCoroutine(IEWaitForRegionSegments());
        }

        /// <summary>
        /// 告诉服务器要生成, 并让服务器生成 (隐性), 然后在生成好后传给客户端
        /// </summary>
        [Button]
        public void GenerateRegion(Vector2Int index, bool isFirstGeneration, string specificBiome = null)
        {
            //检查是否正在生成
            if (regionGeneration_blockSaves != null || regionGeneration_region != null)
            {
                Debug.LogError("正在生成区域, 请等待");
                return;
            }

            //初始化资源
            isAskingForGeneratingRegion = true;
            regionGeneration_blockSaves = new();

            //向服务器发送请求
            ServerGenerateRegion(index, isFirstGeneration, specificBiome);

            //等待服务器发回所有资源切片
            coroutineWaitingForRegionSegments = StartCoroutine(IEWaitForRegionSegments());
        }


        IEnumerator IEWaitForRegionSegments()
        {
            yield return new WaitUntil(() => regionGeneration_region != null);
            yield return new WaitUntil(() => regionGeneration_blockSaves.Count == regionGeneration_blocksCount);


            //恢复方块数据 (浅拷贝时方块数据被清除了)
            regionGeneration_region.blocks = regionGeneration_blockSaves;


            //如果是服务器的话要恢复实体数据 (浅拷贝时实体数据被清除了)
            if (isServer)
                regionGeneration_region.entities = GetRegionToGenerate(regionGeneration_region.index).entities;


            Debug.Log($"收到服务器回调, 正在生成已有区域 {regionGeneration_region.index}");


            //生成出区域
            GM.instance.GenerateExistingRegion(regionGeneration_region, () =>
            {
                if (regionGeneration_isFirstGeneration)
                {
                    //如果先前没获取过位置, 则将玩家的位置设置到该区域出生点
                    if (regionGeneration_region.spawnPoint != Vector2Int.zero)
                        transform.position = regionGeneration_region.spawnPoint.To2();

                    fallenY = transform.position.y;
                    hasGeneratedFirstRegion = true;
                }

                //清理资源
                isAskingForGeneratingRegion = false;
                regionGeneration_region = null;
                regionGeneration_blockSaves = null;
                //下面的参数: 如果是 首次中心生成 就快一点, 否则慢一些防止卡顿
            }, null, (ushort)(GFiles.settings.performanceLevel * (regionGeneration_isFirstGeneration ? 4 : 0.8f)));
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

        /// <summary>
        /// 服务器会检测指定的区域是否存在，存在就回调
        /// </summary>
        /// <param name="index"></param>
        /// <param name="caller"></param>
        [ServerRpc]
        private void ServerGenerateExistingRegion(Vector2Int index, NetworkConnection caller = null)
        {
            //如果正在生成就不要回调
            if (GM.instance.generatingNewRegions.Contains(index))
            {
                ConnectionStopWaitingForRegionData(caller);
                return;
            }

            if (GFiles.world.TryGetRegion(index, out Region region))
            {
                ConnectionGenerateDataSend(region, false, caller);
            }
        }

        [ConnectionRpc]
        private void ConnectionStopWaitingForRegionData(NetworkConnection caller = null)
        {
            StopCoroutine(coroutineWaitingForRegionSegments);
        }

        [ServerRpc]
        private void ServerGenerateRegion(Vector2Int index, bool isFirstGeneration, string specificBiome, NetworkConnection caller = null)
        {
            Debug.Log($"Player={netId} 请求生成区域 {index}");

            //TODO: 如果旧的区域没有玩家了，就回收区域
            // if (!PlayerCenter.all.Any(currentPlayer => currentPlayer.regionIndex == currentPlayer.regionIndex))
            // {
            //     GM.instance.RecycleRegion(index);
            // }

            MethodAgent.RunThread(() =>
            {
                //如果没有则生成新的区域
                GM.instance.GenerateNewRegion(index, specificBiome);

                //获取生成好的区域
                var regionToGenerate = GetRegionToGenerate(index) ?? throw new();

                //这些代码必须在主线程里执行
                MethodAgent.RunOnMainThread(() =>
                {
                    //* 如果是服务器发送的申请: 服务器生成
                    if (isLocalPlayer)
                    {
                        ConnectionGenerateDataSend(regionToGenerate, isFirstGeneration, caller);
                    }
                    //* 如果是客户端发送的申请: 服务器先生成->客户端生成   (如果 服务器和客户端 并行生成, 可能会导致 bug)
                    else
                    {
                        GM.instance.GenerateExistingRegion(
                                regionToGenerate,
                                () => ConnectionGenerateDataSend(regionToGenerate, isFirstGeneration, caller),
                                () => ConnectionGenerateDataSend(regionToGenerate, isFirstGeneration, caller),
                                (ushort)(GFiles.settings.performanceLevel / 2)
                            );
                    }
                });
            });
        }

        //? 因为直接发送一个 region 实在太大了，所以我们不得不将他们分成一个个的 BlockSave
        void ConnectionGenerateDataSend(Region regionToGenerate, bool isFirstGeneration, NetworkConnection caller)
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
                ConnectionGenerateRegionSegment(segment, caller);
            }
        }


        [ConnectionRpc]
        void ConnectionGenerateRegionTotal(Region regionToGenerate, int regionToGenerateBlocksCount, bool isFirstGeneration, NetworkConnection caller)
        {
            regionGeneration_region = regionToGenerate;
            regionGeneration_blocksCount = regionToGenerateBlocksCount;
            regionGeneration_isFirstGeneration = isFirstGeneration;
        }

        [ConnectionRpc]
        void ConnectionGenerateRegionSegment(BlockSave block, NetworkConnection caller)
        {
            regionGeneration_blockSaves.Add(block);
        }





        /// <summary>
        /// 删除区块的屏障方块，这个方法在服务器调用，作用是在存档中删除，而不是在地图中删除！
        /// </summary>
        /// <param name="targetRegionIndex"></param>
        /// <param name="caller"></param>
        [ServerRpc]
        void ServerDestroyRegionBarriers(Vector2Int targetRegionIndex, NetworkConnection caller = null)
        {
            StartCoroutine(IEServerDestroyRegionBarriers(targetRegionIndex));
        }
        IEnumerator IEServerDestroyRegionBarriers(Vector2Int targetRegionIndex)
        {
            //等待目标区域生成
            yield return new WaitUntil(() => GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex));

            //获取原区域
            var targetRegion = GFiles.world.GetRegion(targetRegionIndex);

            //删除上面的屏障方块
            if (GFiles.world.TryGetRegion(targetRegionIndex + Vector2Int.up, out var neighborRegion))
            {
                for (int x = targetRegion.minPoint.x + 1; x <= targetRegion.maxPoint.x - 1; x++)
                {
                    targetRegion.RemovePos(x, targetRegion.maxPoint.y, false);
                    neighborRegion.RemovePos(x, targetRegion.minPoint.y, false);
                }
            }
            //删除下面的屏障方块
            if (GFiles.world.TryGetRegion(targetRegionIndex + Vector2Int.down, out neighborRegion))
            {
                for (int x = targetRegion.minPoint.x + 1; x <= targetRegion.maxPoint.x - 1; x++)
                {
                    targetRegion.RemovePos(x, targetRegion.minPoint.y, false);
                    neighborRegion.RemovePos(x, targetRegion.maxPoint.y, false);
                }
            }
            //删除左边的屏障方块
            if (GFiles.world.TryGetRegion(targetRegionIndex + Vector2Int.left, out neighborRegion))
            {
                for (int y = targetRegion.minPoint.y + 1; y <= targetRegion.maxPoint.y - 1; y++)
                {
                    targetRegion.RemovePos(targetRegion.minPoint.x, y, false);
                    neighborRegion.RemovePos(targetRegion.maxPoint.x, y, false);
                }
            }
            //删除右边的屏障方块
            if (GFiles.world.TryGetRegion(targetRegionIndex + Vector2Int.right, out neighborRegion))
            {
                for (int y = targetRegion.minPoint.y + 1; y <= targetRegion.maxPoint.y - 1; y++)
                {
                    targetRegion.RemovePos(targetRegion.maxPoint.x, y, false);
                    neighborRegion.RemovePos(targetRegion.minPoint.x, y, false);
                }
            }

            //让客户端删除屏障方块
            var minPoint = targetRegion.RegionToMapPos(targetRegion.minPoint);
            var maxPoint = targetRegion.RegionToMapPos(targetRegion.maxPoint);
            ClientDestroyRegionBarriers(targetRegionIndex, minPoint, maxPoint);
        }
        /// <summary>
        /// 这个方法在客户端调用，作用是在地图中删除，而不是在存档中删除！
        /// </summary>
        /// <param name="targetRegionIndex"></param>
        /// <param name="minPoint"></param>
        /// <param name="maxPoint"></param>
        /// <param name="caller"></param>
        [ClientRpc]
        void ClientDestroyRegionBarriers(Vector2Int targetRegionIndex, Vector2Int minPoint, Vector2Int maxPoint, NetworkConnection caller = null)
        {
            //删除上面的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.up))
            {
                int bottomY = maxPoint.y;
                int topY = maxPoint.y + 1;

                for (int x = minPoint.x + 1; x <= maxPoint.x - 1; x++)
                {
                    Map.instance.RemoveBlock(new(x, bottomY), false, false, true);
                    Map.instance.RemoveBlock(new(x, topY), false, false, true);
                }
            }
            //删除下面的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.down))
            {
                int bottomY = minPoint.y - 1;
                int topY = minPoint.y;

                for (int x = minPoint.x + 1; x <= maxPoint.x - 1; x++)
                {
                    Map.instance.RemoveBlock(new(x, bottomY), false, false, true);
                    Map.instance.RemoveBlock(new(x, topY), false, false, true);
                }
            }
            //删除左边的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.left))
            {
                int leftX = minPoint.x - 1;
                int rightX = minPoint.x;

                for (int y = minPoint.y + 1; y <= maxPoint.y - 1; y++)
                {
                    Map.instance.RemoveBlock(new(leftX, y), false, false, true);
                    Map.instance.RemoveBlock(new(rightX, y), false, false, true);
                }
            }
            //删除右边的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.right))
            {
                int leftX = maxPoint.x;
                int rightX = maxPoint.x + 1;

                for (int y = minPoint.y + 1; y <= maxPoint.y - 1; y++)
                {
                    Map.instance.RemoveBlock(new(leftX, y), false, false, true);
                    Map.instance.RemoveBlock(new(rightX, y), false, false, true);
                }
            }
        }


        #endregion





        #region 物品操作

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





        [ServerRpc] public void ServerReduceUsingItemCount(ushort count, NetworkConnection caller = null) => ServerReduceItemCount(usingItemIndex.ToString(), count);
        [ServerRpc] public void ServerReduceItemCount(string index, ushort count, NetworkConnection caller = null) => ClientReduceItemCount(index, count);

        [ClientRpc]
        public void ClientReduceItemCount(string index, ushort count, NetworkConnection caller = null) => inventory.ReduceItemCount(index, count);





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

                //处理数据
                Vector2 pos = head ? head.transform.position : transform.localPosition;
                JObject customData = item.customData != null ? new(item.customData) : new();
                customData.AddObjectIfNone("ori:drop", new JProperty("is_thrown_by_player", true));

                //生成掉落物
                GM.instance.SummonDrop(pos, item.data.id, count, customData.ToString());
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
            string itemName = GameUI.CompareText(GetUsingItemChecked()?.data?.id).text;
            if (itemName.IsNullOrWhiteSpace())
                itemName = GameUI.CompareText("ori:empty_item").text;

            InternalUIAdder.instance.SetStatusText(GameUI.CompareText("ori:switch_item").text.Replace("{item_id}", itemName));
        }





        public void Interact(Vector2 point)
        {
            //实体交互
            foreach (var entity in EntityCenter.all)
            {
                //筛选出可以交互的实体
                if (entity is not IInteractableEntity interactable)
                    continue;

                //检测是否在互动范围内
                if ((entity.transform.position.x - transform.position.x).Abs() <= interactable.interactionSize.x &&
                    (entity.transform.position.y - transform.position.y).Abs() <= interactable.interactionSize.y &&
                    entity.mainCollider.IsInCollider(point))
                {
                    interactable.PlayerInteraction(this);
                    return;
                }
            }

            //与方块交互
            if (IsPointInteractable(point) &&
                map.TryGetBlock(PosConvert.WorldToMapPos(point), isControllingBackground, out Block block) &&
                block.PlayerInteraction(this))
            {

            }
            //使用物品
            else
            {
                ItemBehaviour usingItemBehaviour = GetUsingItemBehaviourChecked();

                if (usingItemBehaviour != null) usingItemBehaviour.Use(point);
            }
        }





        public bool HasUseCDPast() => Tools.time >= itemUseTime + (TryGetUsingItem(out var item) ? item.data.useCD : ItemData.defaultUseCD);

        #endregion





        #region 移动和转向

        public override Vector2 GetMovementDirection()
        {
            //获取移动方向
            var playerCanMove = PlayerCanControl(this) && !isDead;
            var move = playerCanMove ? playerController.Move() : 0;

            //更新移动状态
            bool isMovingThisFrame = (move != 0);
            if (isMovingThisFrame != isMoving)
            {
                isMoving = isMovingThisFrame;
            }


            float resultX = move;
            float resultY = 0;

            //地面摩擦
            if (isOnGround && move == 0 && rb.velocity.x != 0)
            {
                resultX -= rb.velocity.x * blockFriction;
            }

            return new(resultX, resultY);
        }

        public override void SetOrientation(bool right)
        {
            base.SetOrientation(right);

            //更改名字的朝向
            if (nameText)
            {
                if (right)
                    nameText.transform.SetScaleXAbs();
                else
                    nameText.transform.SetScaleXNegativeAbs();
            }
        }

        public void SetOrientationByControl()
        {
            if (isDead)
            {
                SetOrientation(true);
                return;
            }

            //如果玩家没在玩就返回
            if (!Application.isFocused)
                return;

            switch (playerController.SetPlayerOrientation())
            {
                case PlayerController.PlayerOrientation.Left:
                    SetOrientation(false);
                    break;

                case PlayerController.PlayerOrientation.Right:
                    SetOrientation(true);
                    break;

                case PlayerController.PlayerOrientation.Previous:
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
            if (IsPointInteractable(cursorWorldPos) &&
                map.TryGetBlock(PosConvert.WorldToMapPos(cursorWorldPos), isControllingBackground, out Block block) &&
                block.gameObject.activeInHierarchy &&
                !block.data.HasTag("ori:liquid"))
            {
                ExcavateBlock(block);
            }
            //如果是刚刚点的攻击或者是触摸屏模式（触摸屏摇杆）
            else if (playerController.ClickingAttack() || GControls.mode == ControlMode.Touchscreen)
            {
                OnStartAttack();

                //设置时间
                itemUseTime = Tools.time;
                previousAttackTime = Tools.time;
            }
        }

        public override void OnStartAttack() => OnStartAttack(cursorWorldPos, false, true);

        public void OnStartAttack(Vector2 point, bool left, bool right)
        {
            if (!isLocalPlayer || isDead)
                return;

            base.OnStartAttack();

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

        public void AttackEntity(Entity entity)
        {
            int damage = GetUsingItemChecked()?.data?.damage ?? ItemData.defaultDamage;
            entity.TakeDamage(damage, 0.3f, transform.position, transform.localScale.x.Sign() * Vector2.right * 12);

            //如果使用手柄就震动一下
            if (GControls.mode == ControlMode.Gamepad)
                GControls.GamepadVibrationMedium();
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


        public static PlayerController ControlModeToController(Player player, ControlMode mode) => mode switch
        {
            ControlMode.Touchscreen => new TouchscreenController(player),
            ControlMode.KeyboardAndMouse => new KeyboardAndMouseController(player),
            ControlMode.Gamepad => new GamepadController(player),
            _ => throw new()
        };









#if UNITY_EDITOR
        [Button("输出玩家名称")] private void EditorOutputPlayerName() => Debug.Log($"玩家名: {playerName}");
        [Button("输出玩家血量")] private void EditorOutputHealth() => Debug.Log($"血量: {health}");
        [Button("输出区域序号")] private void EditorOutputRegionIndex() => Debug.Log($"区域序号: {regionIndex}");
        [Button("设置手中物品")]
        private void EditorSetUsingItem(string id = "ori:", ushort count = 1)
        {
            var data = ModFactory.CompareItem(id);

            if (data == null)
            {
                Debug.LogWarning($"获取物品失败: 无法匹配到 id {id}");
                return;
            }

            var item = data.DataToItem();
            item.count = count;

            ServerSetItem(usingItemIndex.ToString(), item);
        }
        [Button("快速获取最大-替换手中物品")] private void EditorSetUsingItem(string id = BlockID.GrassBlock) => EditorSetUsingItem(id, ushort.MaxValue);
        [Button("刷新物品栏")] private void EditorRefreshInventory() => EntityInventoryOwnerBehaviour.RefreshInventory(this);
#endif
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
}
