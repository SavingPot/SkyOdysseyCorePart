using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.Converters;
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
using System.Reflection;
using System.Text;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
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
                lock (all)
                {
                    NativeArray<float> invincibleTimeIn = new(all.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> invincibleTimeOut = new(all.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                    //填入数据
                    for (int i = 0; i < all.Count; i++)
                    {
                        var entity = all[i];

                        invincibleTimeIn[i] = entity.invincibleTime;
                    }

                    int batchCount = all.Count / 4;
                    if (batchCount == 0) batchCount = 1;
                    new PropertiesComputingJob()
                    {
                        invincibleTimeIn = invincibleTimeIn,
                        invincibleTimeOut = invincibleTimeOut,
                        frameTime = Performance.frameTime
                    }.ScheduleParallel(all.Count, batchCount, default).Complete();  //除以4代表由四个 Job Thread 执行

                    //填入数据
                    for (int i = 0; i < all.Count; i++)
                    {
                        var entity = all[i];

                        var invincibleTime = invincibleTimeOut[i];
                        if (invincibleTime != 0) entity.invincibleTime += invincibleTime;
                    }

                    invincibleTimeIn.Dispose();
                    invincibleTimeOut.Dispose();
                }
            }
        }

        [BurstCompile]
        public struct PropertiesComputingJob : IJobFor
        {
            [ReadOnly] public NativeArray<float> invincibleTimeIn;
            [WriteOnly] public NativeArray<float> invincibleTimeOut;
            [ReadOnly] public float frameTime;

            [BurstCompile]
            public void Execute(int index)
            {
                var invincibleTime = invincibleTimeIn[index];

                if (invincibleTime > 0)
                    invincibleTimeOut[index] = -Mathf.Min(frameTime, invincibleTime);
                else
                    invincibleTimeOut[index] = 0;
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
    //TODO: NetworkBehaviour->MonoBehaviour
    [DisallowMultipleComponent]
    public class Entity : NetworkBehaviour, IEntity, IRigidbody2D, IVarInstanceID
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
        public JObject customData { get => Init.customData; set => Init.customData = value; }
        [BoxGroup("变量ID"), LabelText("变量唯一ID")] public uint varInstanceId => Init.netId;





        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        public Rigidbody2D rb { get; set; }
        public CapsuleCollider2D mainCollider { get; set; }
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
        [Sync, SyncDefaultValue(0f)] public float invincibleTime { get => invincibleTime_get(); set => invincibleTime_set(value); }
        #endregion

        #region 最大血量
        [SyncGetter] float maxHealth_get() => default; [SyncSetter] void maxHealth_set(float value) { }
        [Sync(nameof(OnMaxHealthChangeMethod)), SyncDefaultValue(100f)] public float maxHealth { get => maxHealth_get(); set => maxHealth_set(value); }

        void OnMaxHealthChangeMethod()
        {
            OnMaxHealthChange();
        }
        public Action OnMaxHealthChange = () => { };
        #endregion

        #region 血量
        [SyncGetter] float health_get() => default; [SyncSetter] void health_set(float value) { }
        [Sync(nameof(OnHealthChangeMethod)), SyncDefaultValue(100f)] public float health { get => health_get(); set => health_set(value); }

        private void OnHealthChangeMethod()
        {
            OnHealthChange();
        }
        public static float defaultHealth = 100;
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

        private void OnRegionIndexChangeMethod()
        {
            OnRegionIndexChange(this);
        }

        public bool TryGetRegion(out Region region)
        {
            if (GFiles.world == null)
                goto fail;

            try
            {
                GFiles.world.TryGetRegion(regionIndex, out region);

                return true;
            }
            catch (System.Exception)
            {
                goto fail;
            }

        fail:
            region = null;

            return false;
        }
        public Region region => GFiles.world.GetRegion(regionIndex);

        public static Action<Entity> OnRegionIndexChange = (entity) => MethodAgent.TryRun(() =>
        {
            if (entity.isPlayer)
            {
                Debug.Log($"玩家 {entity.netId} 的区域改变为了 {entity.regionIndex}");
            }
        }, true);

        [HideInInspector] public bool isPlayer;
        Type classType;

        #endregion

        public const int height = 2;
        //public Dictionary<string, string> localVars = new();
        #endregion









        /* -------------------------------------------------------------------------- */
        /*                                   Base 覆写                                  */
        /* -------------------------------------------------------------------------- */
        public new NetworkIdentity netIdentity;

        public new uint netId => netIdentity.netId;
        //TODO: Temp them
        public new bool isServer => Server.isServer;
        public new bool isClient => Client.isClient;
        public new bool isOwned => netIdentity.isOwned;
        public bool isHost => isServer && isClient;
        public new bool isLocalPlayer => Client.localPlayer == this;





        /* -------------------------------------------------------------------------- */
        /*                                Behaviour 系统                                */
        /* -------------------------------------------------------------------------- */
        public virtual async void OnDeathServer()
        {
            ClearSaveDatum();
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
        public virtual void OnRebornServer() { }
        public virtual void OnRebornClient() { }
        public virtual void OnGetHurtServer() { }
        public virtual void OnGetHurtClient() { }





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
            classType = GetType();
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<CapsuleCollider2D>();
        }

        protected virtual void OnDestroy()
        {
            //TODO: 移动到 EntityInit
            //Debug.Log($"实体 {name} 被删除, Datum Null = {datum == null}", gameObject);

            EntityCenter.RemoveEntity(this);
        }

        protected virtual void Start()
        {
            //TODO: 移动到 EntityInit
            WaitOneFrame(() =>
            {
                if (data != null)
                {
                    rb.gravityScale = data.gravity;
                    mainCollider.direction = data.colliderSize.x > data.colliderSize.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
                    mainCollider.size = data.colliderSize;
                }
            });
        }

        protected virtual void Update()
        {
            if (isServer)
                ServerUpdate();
        }

        protected virtual void FixedUpdate()
        {

        }

        protected virtual void ServerUpdate()
        {
            if (!isPlayer)
            {
                regionIndex = PosConvert.WorldPosToRegionIndex(transform.position);
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (other.TryGetComponent(out Block block))
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

        #region 网络变量逻辑

        #region 等待

        public IEnumerator IEWaitOneFrame(Action action)
        {
            yield return null;

            action();
        }

        public IEnumerator IEWaitFrames(int count, Action action)
        {
            for (int i = 0; i < count; i++)
                yield return null;

            action();
        }

        public IEnumerator IEWaitForCondition(Func<bool> conditionToWait, Action action)
        {
            yield return new WaitWhile(conditionToWait);

            action();
        }

        public void WaitOneFrame(Action action)
        {
            StartCoroutine(IEWaitOneFrame(action));
        }

        public void WaitFrames(int count, Action action)
        {
            StartCoroutine(IEWaitFrames(count, action));
        }
        #endregion







        public SpriteRenderer AddSpriteRenderer(string textureId) => AddSpriteRenderer(ModFactory.CompareTexture(textureId).sprite);
        public SpriteRenderer AddSpriteRenderer(Sprite sprite)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();

            sr.sprite = sprite;
            sr.material = GInit.instance.spriteLitMat;
            renderers.Add(sr);
            spriteRenderers.Add(sr);

            return sr;
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

        public virtual void TakeDamage(float damage) => TakeDamage(damage, 0.1f, transform.position, Vector2.zero);

        public void TakeDamage(float damage, float invincibleTime, Vector2 hurtPos, Vector2 impactForce)
        {
            if (hurtable)
                ServerTakeDamage(damage, invincibleTime, hurtPos, impactForce, null);
        }

        [ServerRpc]
        void ServerTakeDamage(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            if (isDead || isHurting)
                return;

            damage = Mathf.Floor(damage);

            //扣血刷新无敌时间
            health -= damage;
            this.invincibleTime = invincibleTime;

            OnGetHurtServer();

            if (!isPlayer)
                rb.velocity = impactForce;

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

            OnGetHurtClient();

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
        public void Reborn(float newHealth, Vector2? newPos)
        {
            ServerReborn(newHealth, newPos ?? new(float.PositiveInfinity, float.NegativeInfinity), null);
        }

        [ServerRpc]
        public void ServerReborn(float newHealth, Vector2 newPos, NetworkConnection caller)
        {
            health = newHealth;
            isDead = false;

            OnRebornServer();

            if (newPos.x == float.PositiveInfinity && newPos.y == float.NegativeInfinity)
                newPos = GFiles.world.GetRegion(regionIndex)?.spawnPoint ?? Vector2Int.zero;

            if (!isPlayer)
                transform.position = newPos;

            ClientReborn(newHealth, newPos, caller);
        }

        [ClientRpc]
        public void ClientReborn(float newHealth, Vector2 newPos, NetworkConnection caller)
        {
            OnRebornClient();

            if (isPlayer)
                transform.position = newPos;
        }
        #endregion

        public void SummonDrops()
        {
            foreach (DropData drop in data.drops)
            {
                managerGame.SummonItem(transform.position, drop.id, drop.count);
            }
        }

        public void WriteDataToSave()
        {
            if (GFiles.world == null)
            {
                Debug.LogError("世界为空, 无法写入保存数据");
                return;
            }

            if (isPlayer)
            {
                Player player = (Player)this;

                //将玩家数据写入
                foreach (PlayerData datum in GFiles.world.playerData)
                {
                    if (datum.playerName == player.playerName)
                    {
                        datum.playerName = player.playerName;
                        datum.currentRegion = player.regionIndex;
                        datum.inventory = player.inventory;
                        datum.hungerValue = player.hungerValue;
                        datum.thirstValue = player.thirstValue;
                        datum.happinessValue = player.happinessValue;
                        //TODO: to Init
                        datum.health = player.health;
                        datum.completedTasks = player.completedTasks;

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
                        if (save.saveId == Init.saveId)
                        {
                            //写入数据
                            save.pos = transform.position;
                            save.health = health;
                            save.customData = customData.ToString(Formatting.None);

                            return;
                        }
                    }
                }
            }





            Debug.LogWarning($"未在存档中找到实体 {Init.saveId}, 保存失败");
        }

        public void ClearSaveDatum()
        {
            if (GFiles.world == null)
            {
                Debug.LogError("世界为空, 无法清除");
                return;
            }

            foreach (Region region in GFiles.world.regionData)
            {
                foreach (EntitySave save in region.entities)
                {
                    if (save.saveId == Init.saveId)
                    {
                        region.entities.Remove(save);
                        return;
                    }
                }
            }

            Debug.LogWarning($"未在存档中找到实体 {Init.saveId}, 清除失败");
        }

        public void DestroyEntityOnServer()
        {
            Server.DestroyObj(gameObject);
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
        #endregion





        /* -------------------------------------------------------------------------- */
        /*                                  Static 方法                                 */
        /* -------------------------------------------------------------------------- */

        public static Entity GetEntityByNetId(uint netIdToFind) => GetEntityByNetId<Entity>(netIdToFind);

        public static T GetEntityByNetId<T>(uint netIdToFind) where T : Entity
        {
            //uint.MaxValue 是我设定的无效值, 如果 netIdToFind 为 uint.MaxValue 是几乎不可能找到合适的 NetworkIdentity 的
            if (!NetworkClient.spawned.TryGetValue(netIdToFind, out NetworkIdentity identity))
            {
                if (netIdToFind == uint.MaxValue)
                    Debug.LogError($"无法找到无效 {typeof(T).FullName}");
                else
                    Debug.LogError($"无法找到 {typeof(T).FullName} {netIdToFind}");
                Debug.Log(Tools.HighlightedStackTrace());
                return null;
            }

            return identity.GetComponent<T>();
        }

        public static bool TryGetEntityByNetId(uint netIdToFind, out Entity result)
        {
            result = GetEntityByNetId(netIdToFind);

            return result;
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
        [LabelText("最大血量")] public float maxHealth;
        public Type behaviourType;
        [LabelText("掉落的物品")] public List<DropData> drops;
    }

    [Serializable]
    public class EntityData_Summon
    {
        [LabelText("群系")] public string biome;
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
