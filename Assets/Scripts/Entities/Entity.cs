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

    public static class JsonExtensions
    {
        public static void AddObject(this JObject jo, string name)
        {
            jo.Add(new JProperty(name, new JObject()));
        }
        public static void AddObject(this JObject jo, string name, params object[] content)
        {
            jo.Add(new JProperty(name, new JObject(content)));
        }
        public static void AddObject(this JObject jo, string name, object content)
        {
            jo.Add(new JProperty(name, new JObject(content)));
        }

        public static void AddProperty(this JObject jo, string name)
        {
            jo.Add(new JProperty(name));
        }
        public static void AddProperty(this JObject jo, string name, params object[] content)
        {
            jo.Add(new JProperty(name, content));
        }
        public static void AddProperty(this JObject jo, string name, object content)
        {
            jo.Add(new JProperty(name, content));
        }

        public static void AddArray(this JObject jo, string name)
        {
            jo.Add(new JProperty(name, new JArray()));
        }
        public static void AddArray(this JObject jo, string name, JArray other)
        {
            jo.Add(new JProperty(name, new JArray(other)));
        }
        public static void AddArray(this JObject jo, string name, params object[] content)
        {
            jo.Add(new JProperty(name, new JArray(content)));
        }
        public static void AddArray(this JObject jo, string name, object content)
        {
            jo.Add(new JProperty(name, new JArray(content)));
        }





        /* -------------------------------------------------------------------------- */
        /*                                   JToken                                   */
        /* -------------------------------------------------------------------------- */
        public static void AddObject(this JToken jt, string name)
        {
            AddObject((JObject)jt, name);
        }
        public static void AddObject(this JToken jt, string name, params object[] content)
        {
            AddObject((JObject)jt, name, content);
        }
        public static void AddObject(this JToken jt, string name, object content)
        {
            AddObject((JObject)jt, name, content);
        }

        public static void AddProperty(this JToken jt, string name)
        {
            AddProperty((JObject)jt, name);
        }
        public static void AddProperty(this JToken jt, string name, params object[] content)
        {
            AddProperty((JObject)jt, name, content);
        }
        public static void AddProperty(this JToken jt, string name, object content)
        {
            AddProperty((JObject)jt, name, content);
        }

        public static void AddArray(this JToken jt, string name)
        {
            AddArray((JObject)jt, name);
        }
        public static void AddArray(this JToken jt, string name, JArray other)
        {
            AddArray((JObject)jt, name, other);
        }
        public static void AddArray(this JToken jt, string name, params object[] content)
        {
            AddArray((JObject)jt, name, content);
        }
        public static void AddArray(this JToken jt, string name, object content)
        {
            AddArray((JObject)jt, name, content);
        }
    }

    public static class EntityCenter
    {
        public static List<Entity> all = new();
        public static List<Entity> allReady = new();

        public static void Update()
        {
            if (Server.isServer)
            {
                lock (allReady)
                {
                    NativeArray<float> invincibleTimeIn = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float> invincibleTimeOut = new(allReady.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                    //填入数据
                    for (int i = 0; i < allReady.Count; i++)
                    {
                        var entity = allReady[i];

                        invincibleTimeIn[i] = entity.invincibleTime;
                    }

                    int batchCount = allReady.Count / 4;
                    if (batchCount == 0) batchCount = 1;
                    new PropertiesComputingJob()
                    {
                        invincibleTimeIn = invincibleTimeIn,
                        invincibleTimeOut = invincibleTimeOut,
                        frameTime = Performance.frameTime
                    }.ScheduleParallel(allReady.Count, batchCount, default).Complete();  //除以4代表由四个 Job Thread 执行

                    //填入数据
                    for (int i = 0; i < allReady.Count; i++)
                    {
                        var entity = allReady[i];

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
    [DisallowMultipleComponent]
    public class Entity : NetworkBehaviour, IEntity, IRigidbody2D, IHasDestroyed, IVarInstanceID
    {
        /* -------------------------------------------------------------------------- */
        /*                                     接口                                     */
        /* -------------------------------------------------------------------------- */
        public List<string> tags { get; }





        /* -------------------------------------------------------------------------- */
        /*                               Static & Const                               */
        /* -------------------------------------------------------------------------- */
        public const string entityVarPrefix = nameof(GameCore) + "." + nameof(Entity) + ".";
        public static event Action<Entity> OnEntityGetNetworkId = _ => { };





        /* -------------------------------------------------------------------------- */
        /*                                     引用                                     */
        /* -------------------------------------------------------------------------- */
        public GM managerGame => GM.instance;
        public Map map => Map.instance;





        /* -------------------------------------------------------------------------- */
        /*                                     生成属性                                     */
        /* -------------------------------------------------------------------------- */
        private static readonly Dictionary<Type, SyncAttributeData[]> TotalSyncVarAttributeTemps = new();
        [BoxGroup("生成属性"), LabelText("初始化器")] public EntityInit Init { get; internal set; }
        public JObject customData { get => Init.customData; set => Init.customData = value; }
        [BoxGroup("变量ID"), LabelText("变量唯一ID")] public uint varInstanceId { get; internal set; } = uint.MaxValue;





        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        public bool hasDestroyed { get; protected set; }
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
        [Sync] public float invincibleTime { get => invincibleTime_get(); set => invincibleTime_set(value); }
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
        [Sync] public bool isDead { get => isDead_get(); set => isDead_set(value); }
        #endregion

        #region 当前沙盒指针
        [SyncGetter] Vector2Int sandboxIndex_get() => default; [SyncSetter] void sandboxIndex_set(Vector2Int value) { }
        static Vector2Int sandboxIndex_default() => Vector2Int.zero;
        [Sync(nameof(OnSandboxIndexChangeMethod)), SyncDefaultValueFromMethod(nameof(sandboxIndex_default), false)] public Vector2Int sandboxIndex { get => sandboxIndex_get(); set => sandboxIndex_set(value); }

        private void OnSandboxIndexChangeMethod()
        {
            OnSandboxIndexChange(this);
        }

        public bool TryGetSandbox(out Sandbox sandbox)
        {
            if (GFiles.world == null)
                goto fail;

            try
            {
                GFiles.world.TryGetSandbox(sandboxIndex, out sandbox);

                return true;
            }
            catch (System.Exception)
            {
                goto fail;
            }

        fail:
            sandbox = null;

            return false;
        }
        public Sandbox sandbox => GFiles.world.GetSandbox(sandboxIndex);

        public static Action<Entity> OnSandboxIndexChange = (entity) => MethodAgent.TryRun(() =>
        {
            if (entity.isPlayer)
            {
                Debug.Log($"玩家 {entity.netId} 的沙盒改变为了 {entity.sandboxIndex}");
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
            EntityCenter.all.Add(this);
            netIdentity = GetComponent<NetworkIdentity>();
            isPlayer = this is Player;
            classType = GetType();
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<CapsuleCollider2D>();

            WhenGotVarInstanceId(OnGetVarInstanceId);
            WhenGotNetId(OnGetNetId);
        }

        protected virtual void OnDestroy()
        {
            hasDestroyed = true;

            //Debug.Log($"实体 {name} 被删除, Datum Null = {datum == null}", gameObject);

            EntityCenter.all.Remove(this);
            EntityCenter.allReady.Remove(this);

            //TODO: Unregister the sync vars here
        }

        protected virtual void Start()
        {
            WhenCorrectedSyncVars(() => WaitOneFrame(() =>
            {
                if (data != null)
                {
                    maxHealth = data.maxHealth;
                    health = maxHealth;
                    rb.gravityScale = data.gravity;
                    mainCollider.direction = data.colliderSize.x > data.colliderSize.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
                    mainCollider.size = data.colliderSize;
                }
            }));
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
            if (!isPlayer && correctedSyncVars)
            {
                sandboxIndex = PosConvert.WorldPosToSandboxIndex(transform.position);
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

        protected virtual void OnGetNetId()
        {
            varInstanceId = netId;
        }

        protected virtual void OnGetVarInstanceId()
        {
            AutoRegisterVars();
        }

        [BoxGroup("变量ID"), LabelText("注册了网络变量")] public bool registeredSyncVars;
        [BoxGroup("变量ID"), LabelText("校正了网络变量")] public bool correctedSyncVars;






        public void AutoRegisterVars()
        {
            var syncVarTemps = ReadFromSyncAttributeTemps(classType);

            //遍历每个属性
            StringBuilder sb = new();

            //TODO: Improve readability and perfomance step and step
            foreach (SyncAttributeData pair in syncVarTemps)
            {
                string id = SyncPacker.GetInstanceID(sb, pair.propertyPath, varInstanceId);

                if (pair.hook != null)
                {
                    SyncPacker.OnVarValueChange += OnValueChangeBoth;

                    void OnValueChangeBoth(NMSyncVar i, ByteWriter v)
                    {
                        if (i.varId == id)
                            pair.hook.Invoke(this, null);

                        if (hasDestroyed)
                        {
                            SyncPacker.OnVarValueChange -= OnValueChangeBoth;
                            return;
                        }
                    }
                }

                if (isServer)
                {
                    SyncPacker.RegisterVar(id, true, ByteWriter.CreateNull());

                    if (pair.includeDefaultValue)
                    {
                        if (pair.defaultValueMethod == null)
                        {
                            SyncPacker.SetValue(id, pair.defaultValue);
                        }
                        else
                        {
                            var writer = ByteWriter.Create();
                            ByteWriter.TypeWrite(pair.valueType, pair.defaultValueMethod.Invoke(null, null), writer);

                            SyncPacker.SetValue(id, writer);
                        }
                    }
                }
            }

            if (isServer)
            {
                maxHealth = 100;
                if (!isPlayer)
                {
                    health = maxHealth;
                    sandboxIndex = Vector2Int.zero;
                }
                invincibleTime = 0;
                isDead = false;
            }

            //开始等待注册完毕
            StartCoroutine(IEWaitRegistering(syncVarTemps));
        }

        IEnumerator IEWaitRegistering(SyncAttributeData[] syncVarTemps)
        {
            StringBuilder sb = new();


            //等待注册
            foreach (var pair in syncVarTemps)
            {
                string vn = SyncPacker.GetInstanceID(sb, pair.propertyPath, varInstanceId);

                //等待数值正确
                waitingRegisteringVar = vn;
                yield return new WaitUntil(() => SyncPacker.HasVar(vn));
            }

            waitingRegisteringVar = string.Empty;
            registeredSyncVars = true;



            //等待数值修正
            foreach (var pair in syncVarTemps)
            {
                string vn = SyncPacker.GetInstanceID(sb, pair.propertyPath, varInstanceId);

                //等待数值正确
                waitingCorrectingVar = vn;
                yield return new WaitUntil(() => SyncPacker.IsValueCorrect(vn));
            }

            waitingCorrectingVar = string.Empty;
            correctedSyncVars = true;


            //准备好了
            OnReady();
        }

        public virtual void OnReady()
        {
            EntityCenter.allReady.Add(this);
        }

        public string waitingRegisteringVar;
        public string waitingCorrectingVar;

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

        public void WhenGotNetId(Action action)
        {
            StartCoroutine(IEWaitForCondition(() => netId == 0, () =>
            {
                action();
                MethodAgent.TryRun(() => OnEntityGetNetworkId(this), true);
            }));
        }

        public void WhenGotVarInstanceId(Action action)
        {
            StartCoroutine(IEWaitForCondition(() => varInstanceId == uint.MaxValue, action));
        }

        public void WhenRegisteredSyncVars(Action action)
        {
            StartCoroutine(IEWaitForCondition(() => !registeredSyncVars, action));
        }

        public void WhenCorrectedSyncVars(Action action)
        {
            StartCoroutine(IEWaitForCondition(() => !correctedSyncVars, action));
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

        public virtual void TakeDamage(float hurtHealth) => TakeDamage(hurtHealth, 0.1f, transform.position, Vector2.zero);

        public void TakeDamage(float hurtHealth, float invincibleTime, Vector2 hurtPos, Vector2 impactForce)
        {
            if (hurtable)
                ServerTakeDamage(this, hurtHealth, invincibleTime, hurtPos, impactForce, null);
        }

        [ServerRpc]
        static void ServerTakeDamage(Entity param0, float param1, float param2, Vector2 param3, Vector2 param4, NetworkConnection caller)
        {
            if (param0)
            {
                if (param0.isDead || param0.isHurting)
                    return;

                param1 = Mathf.Floor(param1);

                //扣血刷新无敌时间
                param0.health -= param1;
                param0.invincibleTime = param2;

                param0.OnGetHurtServer();

                if (!param0.isPlayer)
                    param0.rb.velocity = param4;

                Debug.Log($"{param0.transform.GetPath()} 收到伤害, 值为 {param1}, 新血量为 {param0.health}");

                ClientTakeDamage(param0, param1, param2, param3, param4, caller);
            }
        }

        [ClientRpc]
        static void ClientTakeDamage(Entity param0, float param1, float param2, Vector2 param3, Vector2 param4, NetworkConnection caller)
        {
            if (param0)
            {
                if (!Server.isServer)
                    Debug.Log($"{param0.transform.GetPath()} 收到伤害, 值为 {param1}");

                //播放受伤音频
                if (!param0.takeDamageAudioId.IsNullOrWhiteSpace())
                    GAudio.Play(param0.takeDamageAudioId);

                param0.OnGetHurtClient();

                //应用击退效果
                if (param0.isPlayer && param0.isOwned)
                    param0.rb.velocity = param4;

                GM.instance.bloodParticlePool.Get(param0);
                GM.instance.damageTextPool.Get(param0, param1);
            }
        }


        #region 死亡
        public void Death()
        {
            ServerDeath(this, null);
        }

        [ServerRpc]
        protected static void ServerDeath(Entity entity, NetworkConnection caller)
        {
            //防止一生成就死亡时导致报错
            if (entity.registeredSyncVars)
            {
                if (entity.isDead)
                {
                    Debug.LogError($"实体 {entity.gameObject.name} 已死亡, 请勿反复执行");
                    return;
                }

                entity.isDead = true;
            }

            //Debug.Log($"服务器: 实体 {name} 已死亡");

            entity.OnDeathServer();
            ClientDeath(entity, caller);
        }

        [ClientRpc]
        protected static void ClientDeath(Entity entity, NetworkConnection caller)
        {
            //Debug.Log($"客户端: 实体 {name} 已死亡");

            //? 不要使用 RpcDeath 来回收资源等, 资源回收应该放在 OnDestroy 中, 因为服务器可能会在调用 RpcDeath 前删除物体
            if (entity)
            {
                entity.OnDeathClient();
            }
        }
        #endregion


        #region 重生
        public void Reborn(float newHealth, Vector2? newPos)
        {
            ServerReborn(this, newHealth, newPos ?? new(float.PositiveInfinity, float.NegativeInfinity), null);
        }

        [ServerRpc]
        public static void ServerReborn(Entity param0, float param1, Vector2 param2, NetworkConnection caller)
        {
            param0.health = param1;
            param0.isDead = false;

            param0.OnRebornServer();

            if (param2.x == float.PositiveInfinity && param2.y == float.NegativeInfinity)
                param2 = GFiles.world.GetSandbox(param0.sandboxIndex)?.spawnPoint ?? Vector2Int.zero;

            if (!param0.isPlayer)
                param0.transform.position = (Vector2)param2;

            ClientReborn(param0, param1, param2, caller);
        }

        [ClientRpc]
        public static void ClientReborn(Entity param0, float param1, Vector2 param2, NetworkConnection caller)
        {
            param0.OnRebornClient();

            if (param0.isPlayer)
                param0.transform.position = (Vector2)param2;
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
                        datum.currentSandbox = player.sandboxIndex;
                        datum.inventory = player.inventory;
                        datum.hungerValue = player.hungerValue;
                        datum.thirstValue = player.thirstValue;
                        datum.happinessValue = player.happinessValue;
                        datum.health = player.health;

                        return;
                    }
                }
            }
            else
            {
                //将实体数据写入
                foreach (Sandbox sb in GFiles.world.sandboxData)
                {
                    foreach (EntitySave save in sb.entities)
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

            foreach (Sandbox sb in GFiles.world.sandboxData)
            {
                foreach (EntitySave save in sb.entities)
                {
                    if (save.saveId == Init.saveId)
                    {
                        sb.entities.Remove(save);
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
        internal class SyncAttributeData
        {
            public string propertyPath;
            public MethodInfo hook;
            public bool includeDefaultValue;
            public ByteWriter defaultValue;
            public MethodInfo defaultValueMethod;
            public string valueType;
        }

        internal static SyncAttributeData[] ReadFromSyncAttributeTemps(Type type)
        {
            if (TotalSyncVarAttributeTemps.TryGetValue(type, out SyncAttributeData[] value))
            {
                return value;
            }

            //如果没有就添加
            List<SyncAttributeData> ts = new();

            foreach (var property in type.GetAllProperties())
            {
                //如果存在 SyncAttribute 就添加到列表
                if (AttributeGetter.TryGetAttribute<SyncAttribute>(property, out var att))
                {
                    string propertyPath = $"{property.DeclaringType.FullName}.{property.Name}";
                    bool includeDefaultValue = false;
                    MethodInfo hookMethod = null;
                    ByteWriter defaultValue = ByteWriter.CreateNull();
                    MethodInfo defaultValueMethod = null;

                    if (!att.hook.IsNullOrWhiteSpace())
                    {
                        hookMethod = !att.hook.Contains(".") ? type.GetMethodFromAllIncludingBases(att.hook) : ModFactory.SearchUserMethod(att.hook);

                        if (hookMethod == null)
                        {
                            Debug.LogError($"无法找到同步变量 {propertyPath} 的钩子: {att.hook}");
                        }
                    }

                    if (AttributeGetter.TryGetAttribute<SyncDefaultValueAttribute>(property, out var defaultValueAtt))
                    {
                        if (defaultValueAtt.defaultValue != null && property.PropertyType.FullName != defaultValueAtt.defaultValue.GetType().FullName)
                            Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {property.PropertyType.FullName} , 但默认值为 {defaultValueAtt.defaultValue.GetType().FullName}");
                        else
                        {
                            var writer = ByteWriter.Create();

                            //TODO: 检查方法是否实例, 参数列表
                            if (defaultValueAtt.defaultValue != null)
                                ByteWriter.TypeWrite(property.PropertyType.FullName, defaultValueAtt.defaultValue, writer);
                            else
                                writer.WriteNull();

                            ByteWriter.TypeWrite(property.PropertyType.FullName, defaultValueAtt.defaultValue, writer);
                            defaultValue = writer;
                            includeDefaultValue = true;
                        }
                    }
                    else if (AttributeGetter.TryGetAttribute<SyncDefaultValueFromMethodAttribute>(property, out var defaultValueFromMethodAtt))
                    {
                        defaultValueMethod = !defaultValueFromMethodAtt.methodName.Contains(".") ? type.GetMethodFromAllIncludingBases(defaultValueFromMethodAtt.methodName) : ModFactory.SearchUserMethod(defaultValueFromMethodAtt.methodName);

                        if (defaultValueMethod == null)
                        {
                            Debug.LogError($"无法找到同步变量 {propertyPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName}");
                            continue;
                        }

                        if (property.PropertyType.FullName != defaultValueMethod.ReturnType.FullName)
                        {
                            Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {property.PropertyType.FullName} , 但默认值为 {defaultValueMethod.ReturnType.FullName}");
                            continue;
                        }

                        includeDefaultValue = true;

                        if (defaultValueFromMethodAtt.getValueUntilRegister)
                        {

                        }
                        else
                        {
                            var writer = ByteWriter.Create();
                            ByteWriter.TypeWrite(property.PropertyType.FullName, defaultValueMethod.Invoke(null, null), writer);
                            defaultValue = writer;
                        }
                    }

                    ts.Add(new()
                    {
                        propertyPath = propertyPath,
                        hook = hookMethod,
                        includeDefaultValue = includeDefaultValue,
                        defaultValue = defaultValue,
                        defaultValueMethod = defaultValueMethod,
                        valueType = property.PropertyType.FullName,
                    });
                }
            }


            //将数据写入字典
            value = ts.ToArray();
            TotalSyncVarAttributeTemps.Add(type, value);

            return value;
        }

        public static Entity GetEntityByNetId(uint netIdToFind) => GetEntityByNetId<Entity>(netIdToFind);

        public static T GetEntityByNetId<T>(uint netIdToFind) where T : Entity
        {
            //uint.MaxValue 是我设定的无效值, 实际上也许不一定无效
            if (netIdToFind == uint.MaxValue)
                return null;

            if (!NetworkClient.spawned.TryGetValue(netIdToFind, out NetworkIdentity identity))
            {
                Debug.LogError($"无法找到 Entity {netIdToFind}");
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
