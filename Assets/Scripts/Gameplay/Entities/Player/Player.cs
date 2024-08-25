using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.Network;
using GameCore.UI;
using Mirror;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;
using static GameCore.UI.PlayerUI;
using ClientRpcAttribute = GameCore.Network.ClientRpcAttribute;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;
using Random = UnityEngine.Random;

namespace GameCore
{
    /// <summary>
    /// 玩家的逻辑脚本
    /// </summary>
    [EntityBinding(EntityID.Player), NotSummonable, RequireComponent(typeof(Rigidbody2D))]
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
                            var tagName = tag.GetTagName();
                            if (TaskTagTable.ContainsKey(tagName))
                            {
                                pui.CompleteTask(TaskTagTable[tagName]);
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
        /// <returns> 对应区域是否未解锁且纵深层存在 </returns>
        bool IsRegionUnlocked(Vector2Int regionIndex)
        {
            return !GFiles.world.TryGetRegion(regionIndex, out _) &&
                   !GM.instance.generatingNewRegions.Contains(regionIndex) &&
                    RegionGeneration.IslandGenerationTable.ContainsKey(regionIndex.y) &&
                    regionIndex.y != ChallengeRoomGeneration.challengeRoomIndexY;
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
                if (IsRegionUnlocked(targetIndex))
                {
                    sr.color = new(1, 1, 1, 0.8f);
                    sr.gameObject.SetActive(true);
                    sr.transform.position = Region.GetMiddle(targetIndex);
                    ti.SetText(GM.GetRegionUnlockingCost(targetIndex));
                }
                else
                {
                    FadeRegionUnlockingRenderer(sr);
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
        [Sync] public float skillPoints;

        public void AddUnlockedSkills(SkillStatusForSave task)
        {
            var skillsTemp = unlockedSkills;
            skillsTemp.Add(task);
            unlockedSkills = skillsTemp;
        }

        [ServerRpc, Button]
        public void ServerAddSkillPoint(float count, NetworkConnection caller = null)
        {
            skillPoints += count;
            pui.skillPointText.RefreshUI();

            //显示文本（扣除技能点时不显示）
            if (count > 0)
                InternalUIAdder.instance.SetTitleText($"获取 {count} 个技能点");

            Debug.Log("ADD Skill Point " + count);
        }

        public bool IsSkillUnlocked(string id) => unlockedSkills.Any(p => p.id == id);

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

        [BoxGroup("属性"), LabelText("物品使用时间")] public float itemUseTime;
        [BoxGroup("属性"), LabelText("物品使用时间")] public float itemCDEndTime;
        public float previousAttackTime;
        public float interactiveRadius => defaultInteractiveRadius + (TryGetUsingItem(out var item) ? item.data.extraDistance : 0);
        public float parryEndTime { get; private set; }
        public float parryCDEndTime { get; private set; }





        [HideInInspector] public SpriteRenderer usingShieldRenderer { get; set; }
        [HideInInspector] public SpriteRenderer usingItemRenderer { get; set; }
        [HideInInspector] public BoxCollider2D usingItemCollider { get; set; }
        [HideInInspector] public InventoryItemRendererCollision usingItemCollisionComponent { get; set; }

        public void ModifyUsingShieldRendererTransform()
        {
            usingShieldRenderer.transform.localPosition = new(0.04f, -0.7f);
            usingShieldRenderer.transform.localScale = new(0.5f, 0.5f);
        }

        public void ModifyUsingItemRendererTransform(Vector2 localPosition, Vector2 localScale, int localRotation)
        {
            localScale *= 0.5f; //物品拿在手上时缩小一半
            localPosition += new Vector2(0.1f, -0.5f); //使物品在手掌上

            usingItemRenderer.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0, 0, localRotation));
            usingItemRenderer.transform.SetScale(localScale);

            //碰撞体
            usingItemCollider.size = localScale;
        }

        #endregion




        /* -------------------------------------------------------------------------- */
        /*                                    临时数据                                    */
        /* -------------------------------------------------------------------------- */
        private readonly Collider2D[] boundaryDetectionTemp = new Collider2D[10];
        private readonly Collider2D[] itemPickUpObjectsDetectedTemp = new Collider2D[40];
        public PlayerController playerController;
        float rushTimer;
        public Enemy lockOnTarget { get; private set; }
        readonly Collider2D[] lockOnOverlapTemp = new Collider2D[10];





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public static int playerLayer { get; private set; }
        public static int playerLayerMask { get; private set; }
        public static float itemPickUpRadius = 1.8f;
        public static int quickInventorySlotCount = 8;   //偶数
        public static int halfQuickInventorySlotCount = quickInventorySlotCount / 2;
        public static Func<Player, bool> PlayerCanControl = player => (GameUI.page == null || !GameUI.page.ui) && player.hasGeneratedFirstRegion && Application.isFocused && player.GetTemperatureEffectState() != TemperatureEffectState.Frozen && !player.isDead;
        public const float playerDefaultGravity = 6f;
        public const float defaultInteractiveRadius = 2.8f;





        /* -------------------------------------------------------------------------- */
        /*                                    死亡与重生                                   */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("属性"), LabelText("重生计时器"), HideInInspector] public float respawnTimer;
        public static Quaternion deathQuaternion = Quaternion.Euler(0, 0, 90);
        public static float deathLowestColorFloat = 0.45f;
        public static Color deathLowestColor = new(deathLowestColorFloat, deathLowestColorFloat, deathLowestColorFloat);
        public const float RESPAWN_WAIT_TIME = 35;




        public static readonly Dictionary<string, string> TaskNameTable = new()
        {
            { ItemID.FeatherWing, "ori:get_feather_wing" },
            { ItemID.StrawRope, "ori:get_straw_rope" },
            { BlockID.Campfire, "ori:get_campfire" },
            { ItemID.FlintKnife, "ori:get_flint_knife" },
            { ItemID.FlintHoe, "ori:get_flint_hoe" },
            { ItemID.FlintSword, "ori:get_flint_sword" },
            { ItemID.Bark, "ori:get_bark" },
        };

        public static readonly Dictionary<string, string> TaskTagTable = new()
        {
            { "ori:log", "ori:get_log" },
            { "ori:meat", "ori:get_meat" },
            { "ori:planks", "ori:get_planks" },
            { "ori:ore", "ori:get_ore" },
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
            velocityFactor = () =>
            {
                return oldFactor() * (transform.localScale.x.Sign() != rb.velocity.x.Sign() ? 0.75f : 1) * (IsSkillUnlocked(SkillID.Exploration_Run) ? 1.2f : 1);
            };

            playerCanvas = transform.Find("Canvas");



            //控制器
            GControls.OnModeChanged += UpdateController;
        }

        public override void Initialize()
        {
            base.Initialize();

            //让拥有者请求加载服务器存档中的位置
            if (isOwned)
                RestorePlayerPositionFromSave();



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
            EntityInventoryOwnerBehaviour.CreateItemRenderers(this, leftArm.transform, rightArm.transform, 2, 9);

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

                GM.instance.weatherParticle.transform.SetParent(transform);
                GM.instance.weatherParticle.transform.localPosition = new(0, 40);




                //初始化控制
                UpdateController(GControls.mode);
            }



            BindHumanAnimations(this);


            //显示名称
            OnNameChange(playerName);

            //防止玩家在生成区域前就掉落到很低的地方
            GravitySet(this);
        }

        [ServerRpc]
        void RestorePlayerPositionFromSave(NetworkConnection caller = null)
        {
            if (Init.save.pos != Vector2.zero)
                regionIndex = PosConvert.WorldPosToRegionIndex(Init.save.pos);

            ConnectionRestorePlayerPositionFromSave(Init.save.pos, caller);
        }

        [ConnectionRpc]
        void ConnectionRestorePlayerPositionFromSave(Vector2 pos, NetworkConnection caller)
        {
            SetPosition(pos);
            var regionIndexToGenerate = PosConvert.WorldPosToRegionIndex(pos);

            //生成地图
            if (regionIndexToGenerate.y == ChallengeRoomGeneration.challengeRoomIndexY || !RegionGeneration.IslandGenerationTable.ContainsKey(regionIndexToGenerate.y))
                GenerateRegion(PosConvert.WorldPosToRegionIndex(((PlayerSave)Init.save).respawnPoint), true);
            else
                GenerateRegion(regionIndexToGenerate, Init.save.pos != Vector2.zero);
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
                respawnTimer = 0;
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
                ControlPlayer();

            //血量低于 15 就播放心跳声 (死了不播放声音)
            if (health <= 15)
            {
                GAudio.Play(AudioID.Heartbeat, null, true);
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
                //旋转以倒在地面上
                transform.localRotation = deathQuaternion;
            }

            //设置相机抖动
            Tools.instance.mainCameraController.shakeLevel = isHurting ? 6 : 0;

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
            Array.Clear(itemPickUpObjectsDetectedTemp, 0, itemPickUpObjectsDetectedTemp.Length);
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
                    GAudio.Play(AudioID.PickUpItem, null);

                    drop.Death();
                }
                else if (other.TryGetComponent<CoinEntity>(out var coinEntity) && coinEntity.CanBePickedUp())
                {
                    ServerAddCoin(coinEntity.coinCount);
                    GAudio.Play(AudioID.PickUpItem, null);

                    coinEntity.Death();
                }
            }



            //检测屏障
            DetectBoundaries();
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


        public override Color DecideColorOfSpriteRenderers()
        {
            //死亡颜色
            if (isDead)
                return deathLowestColor;

            return base.DecideColorOfSpriteRenderers();
        }


        void DetectBoundaries()
        {
            Array.Clear(boundaryDetectionTemp, 0, boundaryDetectionTemp.Length);
            RayTools.OverlapCircleNonAlloc(transform.position, 1.5f, boundaryDetectionTemp, Block.blockLayerMask);

            foreach (var other in boundaryDetectionTemp)
            {
                if (other == null)
                    break;

                if (Map.instance.TryGetBlock(PosConvert.WorldToMapPos(other.transform.position), false, out var otherBlock) && otherBlock.data.id == BlockID.Boundary)
                {
                    TakeDamage(3, 0.5f, transform.position, Vector2.zero);
                    break;
                }
            }
        }

        public override EntitySave GetEntitySaveObjectFromWorld()
        {
            //将玩家数据写入
            foreach (PlayerSave save in GFiles.world.playerSaves)
            {
                if (save.id == playerName)
                {
                    return save;
                }
            }

            return null;
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




        #region 受伤、死亡、复活

        /* -------------------------------------------------------------------------- */
        /*                                     受伤                                     */
        /* -------------------------------------------------------------------------- */
        public override void OnGetHurtServer(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }

        public override void OnGetHurtClient(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            if (GControls.mode == ControlMode.Gamepad)
                GControls.GamepadVibrationMediumStrong();
        }


        /* -------------------------------------------------------------------------- */
        /*                                    死亡逻辑                                    */
        /* -------------------------------------------------------------------------- */
        public override void OnDeathServer()
        {
            //通知客户端等待重生
            ClientWaitForRespawn();
        }

        public override void OnDeathClient()
        {
            //播放音效、显示重生界面
            if (isLocalPlayer)
            {
                GAudio.Play(AudioID.Death, null);
            }

            animWeb.Stop();
        }








        /* -------------------------------------------------------------------------- */
        /*                                     重生逻辑                                     */
        /* -------------------------------------------------------------------------- */


        [ClientRpc]
        public void ClientWaitForRespawn(NetworkConnection caller = null)
        {
            respawnTimer = Tools.time + RESPAWN_WAIT_TIME;

            MethodAgent.CallUntil(() => pui != null, () => pui.ShowRespawnPanel());
        }

        public void Respawn(int newHealth, Vector2? newPos)
        {
            ServerRespawn(newHealth, newPos ?? new(float.PositiveInfinity, float.NegativeInfinity), null);
        }

        [ServerRpc]
        void ServerRespawn(int newHealth, Vector2 newPos, NetworkConnection caller)
        {
            //刷新属性
            health = newHealth;
            isDead = false;

            OnRespawnServer(newHealth, newPos, caller);

            //如果数值无效, 则使用默认的重生点
            if (float.IsInfinity(newPos.x) || float.IsInfinity(newPos.y))
                newPos = ((PlayerSave)Init.save).respawnPoint;

            ClientRespawn(newHealth, newPos, caller);
        }

        [ClientRpc]
        void ClientRespawn(int newHealth, Vector2 newPos, NetworkConnection caller)
        {
            OnRespawnClient(newHealth, newPos, caller);

            //玩家重生时, 由对应客户端设置位置
            transform.position = newPos;
        }

        void OnRespawnServer(float newHealth, Vector2 newPos, NetworkConnection caller)
        {

        }

        void OnRespawnClient(float newHealth, Vector2 newPos, NetworkConnection caller)
        {
            if (isLocalPlayer)
            {
                //防止玩家重生时摔死
                fallenY = newPos.y;

                //关闭 UI
                MethodAgent.CallUntil(() => pui != null, () => pui.respawnPanel.gameObject.SetActive(false));
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

            var result = playerDefaultGravity;

            //潮湿带重力减小
            if (caller.regionIndex.y == -1)
                result *= 0.4f;

            caller.gravity = result;
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








        #region 生成区域

        /// <summary>
        /// 告诉服务器要生成, 并让服务器生成 (隐性), 然后在生成好后传给客户端
        /// </summary>
        [Button]
        public void GenerateExistingRegion(Vector2Int index)
        {
            //检查是否正在生成
            if (regionGeneration_blockSaves != null || regionGeneration_region != null || isAskingForGeneratingRegion)
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
        public void GenerateRegion(Vector2Int index, bool shouldTransportToTheRegion, string specificBiome = null)
        {
            //检查是否是挑战房间
            if (index.y == ChallengeRoomGeneration.challengeRoomIndexY)
            {
                Debug.LogError($"不应该使用 {nameof(GenerateRegion)} 方法来生成挑战房间");
                return;
            }

            //检查是否正在生成
            if (regionGeneration_blockSaves != null || regionGeneration_region != null || isAskingForGeneratingRegion)
            {
                Debug.LogError("正在生成区域, 请等待");
                return;
            }

            //初始化资源
            isAskingForGeneratingRegion = true;
            regionGeneration_blockSaves = new();

            //向服务器发送请求
            ServerGenerateRegion(index, !hasGeneratedFirstRegion, shouldTransportToTheRegion, specificBiome);

            //等待服务器发回所有资源切片
            coroutineWaitingForRegionSegments = StartCoroutine(IEWaitForRegionSegments());
        }


        IEnumerator IEWaitForRegionSegments()
        {
            //等待服务器发送所有数据
            yield return new WaitUntil(() => regionGeneration_region != null);
            yield return new WaitUntil(() => regionGeneration_blockSaves.Count == regionGeneration_blocksCount);


            //恢复方块数据 (因为服务器浅拷贝时方块数据被清除了)
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
        private void ServerGenerateRegion(Vector2Int index, bool isFirstGeneration, bool shouldTransportToTheRegion, string specificBiome, NetworkConnection caller = null)
        {
            Debug.Log($"Player={netId} 请求生成区域 {index}");

            MethodAgent.RunThread(() =>
            {
                //如果没有则生成新的区域
                GM.instance.GenerateNewRegion(index, specificBiome);

                //获取生成好的区域
                var regionToGenerate = GetRegionToGenerate(index) ?? throw new();

                //这些代码必须在主线程里执行
                MethodAgent.RunOnMainThread(() =>
                {
                    //如果存档中没有玩家位置, 则将玩家的位置设置到该区域出生点
                    if (isFirstGeneration && shouldTransportToTheRegion)
                    {
                        var respawnPoint = regionToGenerate.spawnPoint.To2();
                        ConnectionSetPosition(respawnPoint, caller);
                        ((PlayerSave)GetEntitySaveObjectFromWorld()).respawnPoint = respawnPoint;
                        GFiles.SaveAllDataToFiles();
                    }

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
            //将区域浅拷贝
            var copy = regionToGenerate.ShallowCopy();
            copy.blocks = null; //删除方块数据以防止报文过大
            copy.entities = new(); //清空实体数据，因为客户端不需要

            //把大体的数据发送给客户端
            ConnectionGenerateRegionTotal(copy, regionToGenerate.blocks.Count, isFirstGeneration, caller);

            //把方块切片发送给客户端
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

            //从存档中删除屏障方块
            targetRegion.RemoveBarriersBetweenNeighbors();

            //让客户端删除屏障方块
            var minPoint = targetRegion.RegionToMapPos(targetRegion.minPoint);
            var maxPoint = targetRegion.RegionToMapPos(targetRegion.maxPoint);
            ClientDestroyRegionBarriers(targetRegionIndex, minPoint, maxPoint);
        }
        /// <summary>
        /// 这个方法在客户端调用，作用是在地图中删除方块，而不是在存档中删除！
        /// </summary>
        [ClientRpc]
        void ClientDestroyRegionBarriers(Vector2Int targetRegionIndex, Vector2Int minPoint, Vector2Int maxPoint, NetworkConnection caller = null)
        {
            //删除上面的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.up))
            {
                int selfY = maxPoint.y;
                int otherY = maxPoint.y + 1;

                for (int x = minPoint.x + 1; x <= maxPoint.x - 1; x++)
                {
                    Map.instance.RemoveBlock(new(x, selfY), false, false, true);
                    Map.instance.RemoveBlock(new(x, otherY), false, false, true);
                }
            }
            //删除下面的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.down))
            {
                int selfY = minPoint.y;
                int otherY = minPoint.y - 1;

                for (int x = minPoint.x + 1; x <= maxPoint.x - 1; x++)
                {
                    Map.instance.RemoveBlock(new(x, otherY), false, false, true);
                    Map.instance.RemoveBlock(new(x, selfY), false, false, true);
                }
            }
            //删除左边的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.left))
            {
                int selfX = minPoint.x;
                int otherX = minPoint.x - 1;

                for (int y = minPoint.y + 1; y <= maxPoint.y - 1; y++)
                {
                    Map.instance.RemoveBlock(new(otherX, y), false, false, true);
                    Map.instance.RemoveBlock(new(selfX, y), false, false, true);
                }
            }
            //删除右边的屏障方块
            if (GM.instance.generatedExistingRegions.Exists(p => p.index == targetRegionIndex + Vector2Int.right))
            {
                int selfX = maxPoint.x;
                int otherX = maxPoint.x + 1;

                for (int y = minPoint.y + 1; y <= maxPoint.y - 1; y++)
                {
                    Map.instance.RemoveBlock(new(selfX, y), false, false, true);
                    Map.instance.RemoveBlock(new(otherX, y), false, false, true);
                }
            }
        }



        void SetPosition(Vector2 newPos)
        {
            transform.position = newPos;

            //防止摔死
            fallenY = newPos.y;
        }

        [ClientRpc, Button]
        public void ClientSetPosition(Vector2 newPos, NetworkConnection caller = null)
        {
            Debug.Log(newPos);
            SetPosition(newPos);
        }

        [ConnectionRpc]
        public void ConnectionSetPosition(Vector2 newPos, NetworkConnection caller)
        {
            Debug.Log(newPos);
            SetPosition(newPos);
        }


        #endregion


        #region 生成挑战房间

        [Button]
        public void GenerateChallengeRoom(string challengeId)
        {
            ServerGenerateChallengeRoom(challengeId);
        }

        [ServerRpc]
        void ServerGenerateChallengeRoom(string challengeId, NetworkConnection caller = null)
        {
            //设置 x 为 max(x)+1
            int indexX = 0;
            foreach (var item in GFiles.world.regionData)
            {
                if (item.index.y == ChallengeRoomGeneration.challengeRoomIndexY)
                {
                    if (item.index.x > indexX)
                        indexX = item.index.x;
                }
            }
            indexX++;


            //生成并获取区域
            Vector2Int index = new(indexX, ChallengeRoomGeneration.challengeRoomIndexY);
            GM.instance.GenerateNewRegion(index, challengeId);
            var regionToGenerate = GetRegionToGenerate(index) ?? throw new();


            //将区域发送给客户端
            ConnectionGenerateChallengeRoom(challengeId, regionToGenerate, caller);
        }

        [ConnectionRpc]
        void ConnectionGenerateChallengeRoom(string challengeId, Region region, NetworkConnection caller)
        {
            //生成出区域
            GM.instance.GenerateExistingRegion(region, () =>
            {
                transform.position = region.spawnPoint.To3();
            }, null, (ushort)(GFiles.settings.performanceLevel * 2));
        }

        #endregion










        #region 控制

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

        private void UpdateController(ControlMode newMode) => playerController = ControlModeToController(this, newMode);
        public static PlayerController ControlModeToController(Player player, ControlMode mode) => mode switch
        {
            ControlMode.Touchscreen => new TouchscreenController(player),
            ControlMode.KeyboardAndMouse => new KeyboardAndMouseController(player),
            ControlMode.Gamepad => new GamepadController(player),
            _ => throw new()
        };





        private void ControlPlayer()
        {
            /* ------------------------------- 如果在地面上并且点跳跃 ------------------------------ */
            if (isOnGround && playerController.Jump())
                rb.SetVelocityY(GetJumpVelocity(30));


            /* ----------------------------------- 冲刺 ----------------------------------- */
            if (Tools.time > rushTimer && playerController.Rush(out var direction) && IsSkillUnlocked(SkillID.Exploration))
            {
                //如果使用物品失败且冲刺CD过了就冲刺
                Rush(direction);
            }


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

            //检查锁定目标是否死亡
            if (lockOnTarget && lockOnTarget.isDead)
            {
                CancelLockOnTarget();
            }

            //锁定敌人
            if (Keyboard.current.leftShiftKey.isPressed)
            {
                if (lockOnTarget == null)
                {
                    //做一次范围检测
                    Array.Clear(lockOnOverlapTemp, 0, lockOnOverlapTemp.Length);
                    RayTools.OverlapCircleNonAlloc(transform.position, 10, lockOnOverlapTemp, Enemy.enemyLayerMask);

                    //找到数组的有效长度
                    int validLength = lockOnOverlapTemp.Length;
                    for (int i = 0; i < lockOnOverlapTemp.Length; i++)
                    {
                        if (lockOnOverlapTemp[i] == null)
                        {
                            validLength = i;
                            break;
                        }
                    }

                    // 0 表示没有找到敌人
                    if (validLength != 0)
                    {
                        var collider = lockOnOverlapTemp[Random.Range(0, validLength)];
                        if (collider.TryGetComponent<Enemy>(out var enemy))
                        {
                            lockOnTarget = enemy;
                            pui.LockOnEnemy(enemy);
                            Tools.instance.mainCameraController.secondLookAt = lockOnTarget.transform;
                            Tools.instance.mainCameraController.EnableGlobalVolumeVignette();
                            Tools.instance.mainCameraController.SetGlobalVolumeVignette(0.35f);
                        }
                    }
                }
            }
            else
            {
                if (lockOnTarget)
                {
                    CancelLockOnTarget();
                }
            }

            void CancelLockOnTarget()
            {
                lockOnTarget = null;
                Tools.instance.mainCameraController.secondLookAt = null;
                Tools.instance.mainCameraController.DisableGlobalVolumeVignette();
                pui.LockOnEnemy(null);
            }

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
                void TryUnlockRegion(Vector2Int targetIndex)
                {
                    if (!IsRegionUnlocked(targetIndex))
                        return;

                    var cost = GM.GetRegionUnlockingCost(targetIndex);

                    if (coin < cost)
                    {
                        InternalUIAdder.instance.SetStatusText("金币不足!");
                        return;
                    }

                    ServerAddCoin(-cost);
                    ServerAddSkillPoint(1);

                    GenerateRegion(targetIndex, false);
                    RefreshRegionUnlockingRenderers();
                    ServerDestroyRegionBarriers(targetIndex);
                }

                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    TryUnlockRegion(regionIndex + Vector2Int.up);
                }
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    TryUnlockRegion(regionIndex + Vector2Int.down);
                }
                else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                {
                    TryUnlockRegion(regionIndex + Vector2Int.left);
                }
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    TryUnlockRegion(regionIndex + Vector2Int.right);
                }
            }


            /* ----------------------------------- 睡眠 ----------------------------------- */
            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            {
                //TODO: 睡眠
            }

            #region 切换物品

            void FuncSwitchItem(Func<bool> whetherToSwitch, int ii)
            {
                if (whetherToSwitch())
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

        void Rush(bool isRight)
        {
            var rushSpeed = 18f;
            rb.AddVelocityX(isRight ? rushSpeed : -rushSpeed);
            rushTimer = Tools.time + 1;
        }

        #endregion





        #region 攻击/挖掘

        public static Func<Player, Block, float> GetBlockExcavationCD = (player, block) =>
        {
            var result = player.GetUsingItemCD();

            //采矿技能
            if (player.IsSkillUnlocked(SkillID.Exploration_Mining))
            {
                result *= 0.92f;

                //石头、矿物的额外加成
                if (block.data.id == BlockID.Stone || block.data.HasTag("ori:ore"))
                    result *= 0.85f;
            }

            return result;
        };

        private void ExcavateBlock(Block block)
        {
            if (!isLocalPlayer || block == null)
                return;


            //播放挖掘动画
            if (!animWeb.GetAnim("attack_rightarm", 0).isPlaying)
                animWeb.SwitchPlayingTo("attack_rightarm", 0);

            //让目标方块 3x3 范围内扣血
            if (block.data.IsValidForAreaMiningI() && IsSkillUnlocked(SkillID.Exploration_Mining_AreaMiningI))
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (Map.instance.TryGetBlock(new(block.pos.x + x, block.pos.y + y), block.isBackground, out var currentBlock) &&
                            currentBlock.data.IsValidForAreaMiningI())
                        {
                            currentBlock.TakeDamage(excavationStrength);
                        }
                    }
                }
            }
            //连锁采矿
            else if (block.data.HasTag("ori:ore") && IsSkillUnlocked(SkillID.Exploration_Mining_AreaMiningII))
            {
                List<Block> blocksFound = new();

                void FindNeighbor(int x, int y)
                {
                    for (int currentX = x - 2; currentX <= x + 2; currentX++)
                    {
                        for (int currentY = y - 2; currentY <= y + 2; currentY++)
                        {
                            Vector2Int pos = new(currentX, currentY);
                            if (Map.instance.TryGetBlock(pos, block.isBackground, out var currentBlock) &&
                                currentBlock.data.id == block.data.id)
                            {
                                if (!blocksFound.Any(p => p.pos == pos))
                                {
                                    blocksFound.Add(currentBlock);
                                    FindNeighbor(currentX, currentY);
                                }
                            }
                        }
                    }
                }

                //开始递归式搜索
                FindNeighbor(block.pos.x, block.pos.y);

                foreach (var currentBlock in blocksFound)
                {
                    currentBlock.TakeDamage(excavationStrength);
                }
            }
            //让目标方块扣血
            else
            {
                block.TakeDamage(excavationStrength);
            }

            //播放音效
            GAudio.Play(AudioID.ExcavatingBlock, transform.position);

            //手柄震动
            if (GControls.mode == ControlMode.Gamepad)
                GControls.GamepadVibrationSlighter(0.1f);

            //设置时间
            ResetUseItemCD(GetBlockExcavationCD(this, block));
        }

        public void OnHoldAttack()
        {
            if (!HasItemCDPast())
                return;

            //如果 鼠标在挖掘范围内 && 在鼠标位置获取到方块 && 方块是激活的 && 方块不是液体
            if (IsPointInteractable(cursorWorldPos) &&
                Map.instance.TryGetBlock(PosConvert.WorldToMapPos(cursorWorldPos), isControllingBackground, out Block block) &&
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
                ResetUseItemCD(GetUsingItemCD());
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
            //播放攻击动画
            if (leftArm)
                animWeb.SwitchPlayingTo("attack_leftarm", 0);
            if (rightArm)
                animWeb.SwitchPlayingTo("attack_rightarm", 0);
        }

        public bool AttackEntity(Entity entity)
        {
            //如果使用手柄就震动一下
            if (GControls.mode == ControlMode.Gamepad)
                GControls.GamepadVibrationMedium();

            int damage = GetUsingItemChecked()?.data?.damage ?? ItemData.defaultDamage;
            return entity.TakeDamage(damage, 0.3f, transform.position, transform.localScale.x.Sign() * Vector2.right * 12);
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
            EntityInventoryOwnerBehaviour.RefreshItemRenderers(this);

            //播放切换音效
            GAudio.Play(AudioID.SwitchQuickInventorySlot, null);

            //改变状态文本
            string itemName = GameUI.CompareText(GetUsingItemChecked()?.data?.id);
            if (itemName.IsNullOrWhiteSpace())
                itemName = GameUI.CompareText("ori:empty_item");

            InternalUIAdder.instance.SetStatusText(GameUI.CompareText("ori:switch_item").Replace("{item_id}", itemName));
        }





        public void Interact(Vector2 point)
        {
            //格挡
            if (lockOnTarget && !Item.Null(inventory.shield) && inventory.shield.data.Shield != null)
            {
                if (Tools.time > parryCDEndTime)
                    ServerParry();

                return;
            }

            //实体交互
            foreach (var entity in EntityCenter.all)
            {
                //筛选出可以交互的实体
                if (entity is not IInteractableEntity interactable || entity == this)
                    continue;

                //检测是否在互动范围内
                if ((entity.transform.position.x - transform.position.x).Abs() <= interactable.interactionSize.x &&
                    (entity.transform.position.y - transform.position.y).Abs() <= interactable.interactionSize.y &&
                    entity.mainCollider.IsInCollider(point))
                {
                    //如果交互成功就返回
                    if (interactable.PlayerInteraction(this))
                        return;
                }
            }

            //与方块交互
            if (IsPointInteractable(point) &&
                Map.instance.TryGetBlock(PosConvert.WorldToMapPos(point), isControllingBackground, out Block block) &&
                block.PlayerInteraction(this))
            {

            }
            //使用物品
            else
            {
                ItemBehaviour usingItemBehaviour = GetUsingItemBehaviourChecked();

                //使用物品
                usingItemBehaviour?.Use(point);
            }
        }






        [ServerRpc]
        void ServerParry(NetworkConnection caller = null)
        {
            SetParryTimeVars();
            ClientParry();
        }

        [ClientRpc]
        void ClientParry(NetworkConnection caller = null)
        {
            SetParryTimeVars();

            //播放盾反音效和动画
            GAudio.Play(AudioID.Parry, transform.position);
            animWeb.SwitchPlayingTo("slight_leftarm_lift");
        }

        void SetParryTimeVars()
        {
            parryEndTime = Tools.time + inventory.shield.data.Shield.parryTime;
            parryCDEndTime = parryEndTime + inventory.shield.data.Shield.parryCD;
        }






        public void ResetUseItemCD(float cd)
        {
            itemUseTime = Tools.time;
            itemCDEndTime = itemUseTime + cd;
        }

        public float GetUsingItemCD() => TryGetUsingItem(out var item) ? item.data.useCD : ItemData.defaultUseCD;

        public bool HasItemCDPast() => Tools.time >= itemCDEndTime;

        #endregion



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
