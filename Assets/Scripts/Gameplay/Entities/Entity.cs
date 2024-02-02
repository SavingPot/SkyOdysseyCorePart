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
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace GameCore
{
    public static class JObjectReaderWriter
    {
        public static void WriteJObject(this NetworkWriter writer, JObject jo)
        {
            writer.WriteString(Compressor.CompressString(jo?.ToString(Formatting.None)));
        }

        public static JObject ReadJObject(this NetworkReader reader)
        {
            string str = Compressor.DecompressString(reader.ReadString());

            return JsonTools.LoadJObjectByString(str);
        }
    }

    public static class EntityCenter
    {
        public static List<Entity> all = new();
        public static Action<Entity> OnAddEntity = _ => { };
        public static Action<Entity> OnRemoveEntity = _ => { };

        public static void AddEntity(Entity entity)
        {
            all.Add(entity);
            OnAddEntity(entity);
        }

        public static void RemoveEntity(Entity entity)
        {
            all.Remove(entity);
            OnRemoveEntity(entity);
        }

        public static void Update()
        {
            if (Server.isServer)
            {
                float frameTime = Performance.frameTime;

                foreach (var entity in all)
                {
                    var invincibleTime = entity.invincibleTime;

                    if (invincibleTime > 0)
                        entity.invincibleTime = invincibleTime - Mathf.Min(frameTime, invincibleTime);
                }
            }
        }
    }

    public class BloodParticlePool
    {
        public Stack<ParticleSystem> stack = new();

        public ParticleSystem Get(Entity entity)
        {
            var particle = stack.Count == 0 ? GameObject.Instantiate(GInit.instance.BloodParticleSystemPrefab) : stack.Pop();

            particle.transform.position = entity.transform.position;
            particle.gameObject.SetActive(true);
            particle.Play();
            Tools.InvokeAfter(particle.main.duration + particle.main.startLifetime.constant, () =>
            {
                stack.Push(particle);
            });

            //生成一个新的
            return particle;
        }

        public void Recover(ParticleSystem particle)
        {
            stack.Push(particle);

            particle.gameObject.SetActive(false);
        }
    }

    public class DamageTextPool
    {
        public Stack<TMP_Text> stack = new();

        public TMP_Text Get(Entity entity, float damage)
        {
            TMP_Text text = null;
            if (stack.Count == 0)
            {
                text = GameObject.Instantiate(GInit.instance.DamageTextPrefab);
                text.transform.SetParent(GameUI.worldSpaceCanvasRT);
            }
            else
            {
                text = stack.Pop();
            }

            text.transform.position = entity.transform.position;
            text.text = damage.ToString();
            text.gameObject.SetActive(true);
            text.GetComponent<Rigidbody2D>().velocity = new(UnityEngine.Random.Range(-5, 5), 9);
            Tools.InvokeAfter(0.5f, () => text.transform.DOScale(new Vector3(0, 0, 1), 0.3f).OnStepComplete(() =>
            {
                text.transform.localScale = Vector3.one;
                Recover(text);
            }));

            //生成一个新的
            return text;
        }

        public void Recover(TMP_Text text)
        {
            stack.Push(text);

            text.gameObject.SetActive(false);
        }
    }

    //TODO: Finish
    public interface IEntityExtensionOnGround : IEntityExtension
    {
        bool onGround { get; set; }
    }

    public interface IEntityExtension
    {

    }

    //TODO: 告别冗长代码
    [DisallowMultipleComponent]
    public class Entity : MonoBehaviour, IEntity, IRigidbody2D, IVarInstanceID
    {
        /* -------------------------------------------------------------------------- */
        /*                                     接口                                     */
        /* -------------------------------------------------------------------------- */
        public List<string> tags { get; }





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public const string entityVarPrefix = nameof(GameCore) + "." + nameof(Entity) + ".";





        /* -------------------------------------------------------------------------- */
        /*                                     引用                                     */
        /* -------------------------------------------------------------------------- */
        public GM managerGame => GM.instance;
        public Map map => Map.instance;





        /* -------------------------------------------------------------------------- */
        /*                                     生成属性                                     */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("生成属性"), LabelText("初始化器")] public EntityInit Init { get; internal set; }
        public JObject customData;
        [BoxGroup("变量ID"), LabelText("变量唯一ID")] public uint varInstanceId => Init.netId;





        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        public Rigidbody2D rb { get; set; }
        public BoxCollider2D mainCollider { get; set; }
        public bool isHurting => invincibleTime > 0;
        public bool hurtable = true;
        [BoxGroup("属性"), LabelText("数据"), HideInInspector] public EntityData data = null;
        [BoxGroup("属性"), LabelText("效果")] public Dictionary<string, float> effects = new();
        [BoxGroup("组件"), LabelText("渲染器")] public List<Renderer> renderers = new();
        [BoxGroup("组件"), LabelText("精灵渲染器"), ReadOnly] public List<SpriteRenderer> spriteRenderers = new();
        [BoxGroup("属性"), LabelText("受伤音效")] public string takeDamageAudioId = AudioID.GetHurt;






        #region 同步变量
        #region 无敌时间
        [SyncGetter] float invincibleTime_get() => default; [SyncSetter] void invincibleTime_set(float value) { }
        [Sync(nameof(OnInvincibleTimeChange)), SyncDefaultValue(0f)] public float invincibleTime { get => invincibleTime_get(); set => invincibleTime_set(value); }
        void OnInvincibleTimeChange()
        {
            if (isHurting)
            {
                foreach (var sr in spriteRenderers)
                {
                    sr.color = new(sr.color.r, 0.5f, 0.5f);
                }
            }
            else
            {
                foreach (var sr in spriteRenderers)
                {
                    sr.color = Color.white;
                }
            }
        }
        #endregion

        #region 最大血量
        [SyncGetter] int maxHealth_get() => default; [SyncSetter] void maxHealth_set(int value) { }
        [Sync(nameof(OnMaxHealthChangeMethod)), SyncDefaultValue(DEFAULT_HEALTH)] public int maxHealth { get => maxHealth_get(); set => maxHealth_set(value); }

        void OnMaxHealthChangeMethod()
        {
            OnMaxHealthChange();
        }
        public Action OnMaxHealthChange = () => { };
        #endregion

        #region 血量
        [SyncGetter] int health_get() => default; [SyncSetter] void health_set(int value) { }
        [Sync(nameof(OnHealthChangeMethod)), SyncDefaultValue(DEFAULT_HEALTH)] public int health { get => health_get(); set => health_set(value); }

        private void OnHealthChangeMethod()
        {
            OnHealthChange();
        }
        public const int DEFAULT_HEALTH = 100;
        public Action OnHealthChange = () => { };
        #endregion

        #region 已死亡
        [SyncGetter] bool isDead_get() => default; [SyncSetter] void isDead_set(bool value) { }
        [Sync, SyncDefaultValue(false)] public bool isDead { get => isDead_get(); set => isDead_set(value); }
        #endregion

        #region 当前区域指针
        [SyncGetter] Vector2Int regionIndex_get() => default; [SyncSetter] void regionIndex_set(Vector2Int value) { }
        static Vector2Int regionIndex_default() => Vector2Int.zero;
        [Sync(nameof(OnRegionIndexChangeMethod)), SyncDefaultValueFromMethod(nameof(regionIndex_default), false)] public Vector2Int regionIndex { get => regionIndex_get(); set => regionIndex_set(value); }
        public Vector2Int chunkIndex;

        private void OnRegionIndexChangeMethod()
        {
            OnRegionIndexChange(this);
        }

        public bool TryGetRegion(out Region region)
        {
            if (GFiles.world == null)
            {
                region = null;
                return false;
            }

            try
            {
                return GFiles.world.TryGetRegion(regionIndex, out region);
            }
            catch (Exception)
            {
                region = null;
                return false;
            }
        }
        public Region region => GFiles.world.GetRegion(regionIndex);

        public static Action<Entity> OnRegionIndexChange = (entity) => { };

        [HideInInspector] public bool isPlayer;
        [HideInInspector] public bool isNotPlayer;
        [HideInInspector] public bool isNotNPC;
        [HideInInspector] public float timeToAutoDestroy;
        Type classType;

        #endregion

        public const int height = 2;
        //public Dictionary<string, string> localVars = new();
        #endregion









        /* -------------------------------------------------------------------------- */
        /*                                   Base 覆写                                  */
        /* -------------------------------------------------------------------------- */
        public NetworkIdentity netIdentity;

        //TODO: Temp them
        [HideInInspector] public uint netId;
        [HideInInspector] public bool isServer;
        [HideInInspector] public bool isClient;
        [HideInInspector] public bool isOwned;
        [HideInInspector] public bool isHost;
        [HideInInspector] public bool isLocalPlayer;





        /* -------------------------------------------------------------------------- */
        /*                                Behaviour 系统                                */
        /* -------------------------------------------------------------------------- */
        public virtual async void OnDeathServer()
        {
            SummonDrops();

            //? 等待一秒是为了防止客户端延迟过高导致报错
            await 1;
            DestroyEntityOnServer();
        }
        public virtual void OnDeathClient()
        {
            Hide();
            ColliderOff();
            this.enabled = false;
        }
        public virtual void OnRebornServer(float newHealth, Vector2 newPos, NetworkConnection caller) { }
        public virtual void OnRebornClient(float newHealth, Vector2 newPos, NetworkConnection caller) { }
        public virtual void OnGetHurtServer(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }
        public virtual void OnGetHurtClient(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }





        public void Recover()
        {
            if (isPlayer)
                return;

            WriteDataToSave();
            Destroy(gameObject);
        }



        public virtual void Hide()
        {
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }
        }

        public virtual void Show()
        {
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
        }

        public virtual void ColliderOff()
        {
            mainCollider.enabled = false;
        }





        #region Unity/Mirror/Entity 方法
        protected virtual void Awake()
        {
            EntityCenter.AddEntity(this);
            netIdentity = GetComponent<NetworkIdentity>();
            isPlayer = this is Player;
            isNotPlayer = !isPlayer;
            isNotNPC = this is not NPC;
            classType = GetType();
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<BoxCollider2D>();

            netId = netIdentity.netId;
            isServer = Server.isServer;
            isClient = Client.isClient;
            isOwned = netIdentity.isOwned;
            isHost = isServer && isClient;
            isLocalPlayer = Client.localPlayer == this;
        }

        public virtual void InitAfterAwake()
        {
            rb.gravityScale = data.gravity;
            mainCollider.size = data.colliderSize;
            mainCollider.offset = data.colliderOffset;

            SetAutoDestroyTime();
        }

        protected virtual void OnDestroy()
        {
            //TODO: 移动到 EntityInit
            //Debug.Log($"实体 {name} 被删除, Datum Null = {datum == null}", gameObject);

            EntityCenter.RemoveEntity(this);
        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {
            chunkIndex = PosConvert.WorldPosToChunkIndex(transform.position);

            if (isServer)
                ServerUpdate();
        }

        protected virtual void FixedUpdate()
        {

        }

        protected virtual void ServerUpdate()
        {
            //更新区域序列
            var newRegionIndex = PosConvert.ChunkToRegionIndex(chunkIndex);
            if (regionIndex != newRegionIndex) regionIndex = newRegionIndex;

            //自动销毁
            if (isNotPlayer && isNotNPC)
            {
                if (Tools.time >= timeToAutoDestroy)
                {
                    DestroyEntityOnServer();
                }
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (Block.TryGetBlockFromCollider(other, out Block block))
            {
                block.OnEntityStay(this);
            }
        }

        protected virtual void OnEnable()
        {
            OnHealthChange += HealthCheck;
        }

        protected virtual void OnDisable()
        {
            OnHealthChange -= HealthCheck;
        }


#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {

        }
#endif
        #endregion

        public void SetAutoDestroyTime()
        {
            timeToAutoDestroy = Tools.time + data.lifetime;
        }







        public SpriteRenderer AddSpriteRenderer(string textureId) => AddSpriteRenderer(ModFactory.CompareTexture(textureId).sprite);
        public SpriteRenderer AddSpriteRenderer(Sprite sprite)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();

            sr.sprite = sprite;
            sr.material = GInit.instance.spriteLitMat;
            sr.sortingOrder = 1;

            AddSpriteRenderer(sr);

            return sr;
        }
        public void AddSpriteRenderer(SpriteRenderer renderer)
        {
            //添加光线遮挡效果
            renderer.gameObject.AddComponent<ShadowCaster2D>();

            spriteRenderers.Add(renderer);
            AddRenderer(renderer);
        }

        public void AddRenderer(Renderer renderer)
        {
            renderers.Add(renderer);
        }



        public void HealthCheck()
        {
            if (isServer)
            {
                if (health <= 0)
                {
                    Death();
                }
            }
        }

        public virtual void TakeDamage(int damage) => TakeDamage(damage, 0.1f, transform.position, Vector2.zero);

        public void TakeDamage(int damage, float invincibleTime, Vector2 hurtPos, Vector2 impactForce)
        {
            if (hurtable)
                ServerTakeDamage(damage, invincibleTime, hurtPos, impactForce, null);
        }

        [ServerRpc]
        void ServerTakeDamage(int damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            if (isDead || isHurting)
                return;


            //通过防御值计算实际伤害
            if (this is IInventoryOwner owner)
            {
                var inventory = owner.GetInventory();

                if (inventory != null)
                {
                    int defense =
                        (inventory.helmet?.data?.Helmet?.defense ?? 0) +
                        (inventory.breastplate?.data?.Breastplate?.defense ?? 0) +
                        (inventory.legging?.data?.Legging?.defense ?? 0) +
                        (inventory.boots?.data?.Boots?.defense ?? 0);

                    //防御值与伤害减免值 1:1
                    damage -= defense;
                }
            }



            //扣血刷新无敌时间
            health -= damage;
            this.invincibleTime = invincibleTime;

            OnGetHurtServer(damage, invincibleTime, damageOriginPos, impactForce, caller);

            if (isNotPlayer)
            {
                rb.velocity = impactForce;
                SetAutoDestroyTime();
            }

            Debug.Log($"{transform.GetPath()} 收到伤害, 值为 {damage}, 新血量为 {health}");

            ClientTakeDamage(damage, invincibleTime, damageOriginPos, impactForce, caller);
        }

        [ClientRpc]
        void ClientTakeDamage(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) => MethodAgent.TryRun(() =>
        {
            if (!Server.isServer)
                Debug.Log($"{transform.GetPath()} 收到伤害, 值为 {damage}");

            //播放受伤音频
            if (!takeDamageAudioId.IsNullOrWhiteSpace())
                GAudio.Play(takeDamageAudioId);

            OnGetHurtClient(damage, invincibleTime, damageOriginPos, impactForce, caller);

            //应用击退效果
            if (isPlayer && isOwned)
                rb.velocity = impactForce;

            GM.instance.bloodParticlePool.Get(this);
            GM.instance.damageTextPool.Get(this, damage);
        }, true);


        #region 死亡
        public void Death()
        {
            //? 防止反复死亡
            if (this != null && !isDead)
                ServerDeath(null);
        }

        [ServerRpc]
        protected void ServerDeath(NetworkConnection caller)
        {
            //防止一生成就死亡时导致报错
            if (isDead)
            {
                Debug.LogError($"实体 {gameObject.name} 已死亡, 请勿反复执行");
                return;
            }

            isDead = true;

            //Debug.Log($"服务器: 实体 {name} 已死亡");

            ClientDeath(caller);
            OnDeathServer();
        }

        [ClientRpc]
        protected void ClientDeath(NetworkConnection caller)
        {
            //Debug.Log($"客户端: 实体 {name} 已死亡");

            //? 不要使用 RpcDeath 来回收资源等, 资源回收应该放在 OnDestroy 中, 因为服务器可能会在调用 RpcDeath 前删除物体, ClientDeath 是用来显示死亡动效的
            OnDeathClient();
        }
        #endregion


        #region 重生
        public void Reborn(int newHealth, Vector2? newPos)
        {
            ServerReborn(newHealth, newPos ?? new(float.PositiveInfinity, float.NegativeInfinity), null);
        }

        [ServerRpc]
        public void ServerReborn(int newHealth, Vector2 newPos, NetworkConnection caller)
        {
            health = newHealth;
            isDead = false;

            OnRebornServer(newHealth, newPos, caller);

            if (newPos.x == float.PositiveInfinity && newPos.y == float.NegativeInfinity)
                newPos = GFiles.world.GetRegion(regionIndex)?.spawnPoint ?? Vector2Int.zero;

            if (isNotPlayer)
                transform.position = newPos;

            ClientReborn(newHealth, newPos, caller);
        }

        [ClientRpc]
        public void ClientReborn(int newHealth, Vector2 newPos, NetworkConnection caller)
        {
            OnRebornClient(newHealth, newPos, caller);

            if (isPlayer)
                transform.position = newPos;
        }
        #endregion

        public void SummonDrops()
        {
            foreach (DropData drop in data.drops)
            {
                managerGame.SummonDrop(transform.position, drop.id, drop.count);
            }
        }

        public void WriteDataToSave()
        {
            if (GFiles.world == null)
            {
                Debug.LogError("世界为空, 无法写入保存数据");
                return;
            }

            //TODO: 统一!
            if (isPlayer)
            {
                Player player = (Player)this;

                //将玩家数据写入
                foreach (PlayerSave save in GFiles.world.playerSaves)
                {
                    if (save.id == player.playerName)
                    {
                        save.pos = player.transform.position;
                        save.inventory = player.inventory;
                        save.hungerValue = player.hungerValue;
                        save.thirstValue = player.thirstValue;
                        save.happinessValue = player.happinessValue;
                        save.health = player.health;
                        save.completedTasks = player.completedTasks;
                        save.customData = player.customData?.ToString(Formatting.None);

                        return;
                    }
                }
            }
            else
            {
                //将实体数据写入
                foreach (Region region in GFiles.world.regionData)
                {
                    foreach (EntitySave save in region.entities)
                    {
                        //如果匹配到自己
                        if (save.saveId == Init.save.saveId)
                        {
                            //写入数据
                            save.pos = transform.position;
                            save.health = health;
                            save.customData = customData?.ToString(Formatting.None);

                            return;
                        }
                    }
                }
            }


            Debug.LogWarning($"未在存档中找到实体 {Init.save.saveId}, 保存失败");
        }

        public void ClearSaveDatum()
        {
            if (GFiles.world == null)
            {
                Debug.LogError("世界为空, 无法清除实体数据");
                return;
            }

            foreach (Region region in GFiles.world.regionData)
            {
                foreach (EntitySave save in region.entities)
                {
                    if (save.saveId == Init.save.saveId)
                    {
                        region.entities.Remove(save);
                        return;
                    }
                }
            }

            Debug.LogWarning($"未在存档中找到实体 {Init.save.saveId}, 清除失败");
        }

        public void DestroyEntityOnServer()
        {
            Server.DestroyObj(gameObject);
            ClearSaveDatum();
        }



        public virtual void SetOrientation(bool right)
        {
            if (right)
            {
                transform.SetScaleXAbs();
            }
            else
            {
                transform.SetScaleXNegativeAbs();
            }
        }





        /* -------------------------------------------------------------------------- */
        /*                                  Static 方法                                 */
        /* -------------------------------------------------------------------------- */

        public static Component GetEntityByNetId(uint netIdToFind, Type type)
        {
#if DEBUG
            //uint.MaxValue 是我设定的无效值, 如果 netIdToFind 为 uint.MaxValue 是几乎不可能找到合适的 NetworkIdentity 的
            if (!NetworkClient.spawned.TryGetValue(netIdToFind, out NetworkIdentity identity))
            {
                if (netIdToFind == uint.MaxValue)
                    Debug.LogError($"无法找到无效实体 {type.FullName}");
                else
                    Debug.LogError($"无法找到实体 {type.FullName} {netIdToFind}");
                return null;
            }

            return identity.GetComponent(type);
#else
            return NetworkClient.spawned[netIdToFind].GetComponent(type);
#endif
        }

        public static T GetEntityByNetId<T>(uint netIdToFind) where T : Entity
        {
#if DEBUG
            //uint.MaxValue 是我设定的无效值, 如果 netIdToFind 为 uint.MaxValue 是几乎不可能找到合适的 NetworkIdentity 的
            if (!NetworkClient.spawned.TryGetValue(netIdToFind, out NetworkIdentity identity))
            {
                if (netIdToFind == uint.MaxValue)
                    Debug.LogError($"无法找到无效 {typeof(T).FullName}");
                else
                    Debug.LogError($"无法找到 {typeof(T).FullName} {netIdToFind}");
                return null;
            }

            return identity.GetComponent<T>();
#else
            return NetworkClient.spawned[netIdToFind].GetComponent<T>();
#endif
        }

        public static bool TryGetEntityByNetId<T>(uint netIdToFind, out T result) where T : Entity
        {
            result = GetEntityByNetId<T>(netIdToFind);

            return result;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            NetworkCallbacks.OnTimeToServerCallback += () =>
            {

            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {

            };
            GM.OnUpdate += EntityCenter.Update;
        }

        public static Type[] EntityExtensions;
        public static void EntityTypeInit()
        {
            List<Type> EntityExtensionList = new();
            ModFactory.EachUserType((_, type) =>
            {
                if (type.IsInterface && type.IsSubclassOf(typeof(IEntityExtension)))
                {
                    EntityExtensionList.Add(type);
                }
            });
        }
    }





    /* -------------------------------------------------------------------------- */
    /*                                    公共类型                                    */
    /* -------------------------------------------------------------------------- */
    [Serializable]
    public class EntityData : ModClass
    {
        [LabelText("路径")] public string path;
        [LabelText("生成")] public EntityData_Summon summon = new();
        [LabelText("速度")] public float speed;
        [LabelText("重力")] public float gravity;
        [LabelText("碰撞箱大小")] public Vector2 colliderSize;
        [LabelText("碰撞箱偏移")] public Vector2 colliderOffset;
        [LabelText("最大血量")] public int maxHealth;
        [LabelText("自动清除周期")] public float lifetime = defaultLifetime;
        [LabelText("搜索半径")] public ushort searchRadius;
        [LabelText("搜索半径平方")] public int searchRadiusSqr;
        [LabelText("普通攻击半径")] public float normalAttackRadius;
        [LabelText("普通攻击伤害")] public int normalAttackDamage;
        [LabelText("普通攻击CD")] public float normalAttackCD;
        public static float defaultLifetime = 60 * 3;
        public Type behaviourType;
        [LabelText("掉落的物品")] public List<DropData> drops;
    }

    [Serializable]
    public class EntityData_Summon
    {
        [LabelText("区域")] public string region;
        [LabelText("默认几率")] public float defaultProbability;
        [LabelText("最早时间")] public float timeEarliest;
        [LabelText("最晚时间")] public float timeLatest;
    }

    public class NotSummonableAttribute : Attribute { }

    public interface IHasDestroyed
    {
        bool hasDestroyed { get; }
    }
}
