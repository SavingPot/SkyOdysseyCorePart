using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameCore.High;
using GameCore.Network;
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ClientRpcAttribute = GameCore.Network.ClientRpcAttribute;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace GameCore
{
    //TODO: 告别冗长代码
    [DisallowMultipleComponent]
    public class Entity : MonoBehaviour, IRigidbody2D, IVarInstanceID, IDeath
    {
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
        /*                                    生成属性                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("生成属性"), LabelText("初始化器")] public EntityInit Init { get; internal set; }
        [BoxGroup("变量ID"), LabelText("变量唯一ID")] public uint varInstanceId => Init.netId;
        [BoxGroup("属性"), LabelText("数据"), HideInInspector] public EntityData data = null;



        /* -------------------------------------------------------------------------- */
        /*                                     信息                                     */
        /* -------------------------------------------------------------------------- */
        [HideInInspector] public bool isPlayer;
        [HideInInspector] public bool isNotPlayer;
        [HideInInspector] public bool isNPC;
        [HideInInspector] public bool isNotNPC;



        /* -------------------------------------------------------------------------- */
        /*                                     网络                                     */
        /* -------------------------------------------------------------------------- */
        [HideInInspector] public NetworkIdentity netIdentity;
        [HideInInspector] public uint netId;
        [HideInInspector] public bool isServer;
        [HideInInspector] public bool isClient;
        [HideInInspector] public bool isOwned;
        [HideInInspector] public bool isHost;
        [HideInInspector] public bool isLocalPlayer;



        /* -------------------------------------------------------------------------- */
        /*                                     组件                                     */
        /* -------------------------------------------------------------------------- */
        public Rigidbody2D rb { get; set; }
        public BoxCollider2D mainCollider { get; set; }
        [BoxGroup("组件"), LabelText(text: "渲染器")] public readonly List<Renderer> renderers = new();
        [BoxGroup("组件"), LabelText("精灵渲染器"), ReadOnly] public List<SpriteRenderer> spriteRenderers = new();



        /* -------------------------------------------------------------------------- */
        /*                                     属性                                     */
        /* -------------------------------------------------------------------------- */
        [HideInInspector] public float timeToAutoDestroy;
        public bool isHurting => invincibleTime > 0.05f;
        public int maxHealth => data.maxHealth;
        public bool isHurtable = true;
        public float previousHurtTime;


        public const int height = 2;







        #region 同步变量
        [Sync, SyncDefaultValue(false)] public bool isDead;
        [Sync] public JObject customData;
        static JObject NewJObject() => new();

        #region 无敌时间
        [Sync(nameof(OnInvincibleTimeChange)), SyncDefaultValue(0f)] public float invincibleTime;
        void OnInvincibleTimeChange(byte[] _)
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

        #region 血量
#if UNITY_EDITOR
        [Button, ServerRpc] void editor_health_set(int value, NetworkConnection caller) => health = value;
#endif
        [Sync(nameof(OnHealthChangeMethod)), SyncDefaultValue(DEFAULT_HEALTH)] public int health;
        public const int DEFAULT_HEALTH = 100;
        public Action OnHealthChange = () => { };
        private void OnHealthChangeMethod(byte[] _)
        {
            OnHealthChange();
        }
        #endregion

        #region 当前区域序列
        [Sync(nameof(OnRegionIndexChangeMethod)), SyncDefaultValueFromMethod(nameof(regionIndex_default), false)] public Vector2Int regionIndex;
        static Vector2Int regionIndex_default() => Vector2Int.zero;
        public Vector2Int chunkIndex;

        private void OnRegionIndexChangeMethod(byte[] _)
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

        public static Action<Entity> OnRegionIndexChange = (entity) =>
        {
            // if (entity is Player player)
            // {
            //     //TODO: 像以前一样自动生成周围的区块
            //     if (player.generatedFirstRegion && !player.askingForGeneratingRegion && player.askingForGeneratingRegionTime + 5 <= Tools.time && !GM.instance.generatingExistingRegion && !GM.instance.generatingNewRegion)
            //     {
            //         //生成周围的八个区域
            //         player.GenerateNeighborRegions();

            //         //刷新时间
            //         player.askingForGeneratingRegionTime = Tools.time;
            //     }
            // }
        };

        #endregion

        #endregion









        /* -------------------------------------------------------------------------- */
        /*                                 Entity 远程指令                                */
        /* 作用：让任意脚本都可以以实体为载体，远程调用任意命令，并允许传参。 */
        /* 原理：在实体上注册命令，然后在客户端/服务器上注册远程调用的函数，并将命令ID和参数一起发送给服务器。 */
        /*      服务器收到命令后，会根据命令ID找到对应的函数，并调用它，同时将返回值发送给客户端。 */
        /*      客户端收到返回值后，会根据返回值进行相应的处理。 */
        /* 优点：非常灵活，可以实现各种各样的远程调用，并且可以传参，可以实现服务器和客户端的同步。 */
        /* 缺点：是实现起来比较复杂，需要编写大量的代码，并且需要在客户端和服务器上都注册命令。 */
        /* -------------------------------------------------------------------------- */
        #region 无参远程指令
        readonly Dictionary<string, Action<Entity>> remoteCommands = new();

        public void RegisterRemoteCommand(string commandId, Action<Entity> action)
        {
            //检查 commandId
            if (commandId.IsNullOrWhiteSpace())
            {
                Debug.LogError($"实体 {name} 注册了一个 {nameof(commandId)} 为空的命令");
                return;
            }

            //检查 action
            if (action == null)
            {
                Debug.LogError($"实体 {name} 注册了一个 {nameof(action)} 为空的命令");
                return;
            }

            //检查重复
            if (remoteCommands.ContainsKey(commandId))
            {
                Debug.LogError($"实体 {name} 已经注册了命令 {commandId}, 不要反复注册");
                return;
            }


            //添加命令
            remoteCommands.Add(commandId, action);
        }

        [ServerRpc]
        public void ServerExecuteRemoteCommand(string commandId, NetworkConnection caller = null)
        {
            _ExecuteRemoteCommand(commandId);
        }

        [ServerRpc]
        public void ServerToClientsExecuteRemoteCommand(string commandId, NetworkConnection caller = null)
        {
            _ClientsExecuteRemoteCommand(commandId, caller);
        }

        [ServerRpc]
        public void ServerToConnectionExecuteRemoteCommand(string commandId, NetworkConnection caller = null)
        {
            _ConnectionExecuteRemoteCommand(commandId, caller);
        }



        [ClientRpc]
        private void _ClientsExecuteRemoteCommand(string commandId, NetworkConnection caller = null)
        {
            _ExecuteRemoteCommand(commandId);
        }

        [ConnectionRpc]
        private void _ConnectionExecuteRemoteCommand(string commandId, NetworkConnection caller = null)
        {
            _ExecuteRemoteCommand(commandId);
        }

        private void _ExecuteRemoteCommand(string commandId)
        {
            //检查命令
            if (!remoteCommands.ContainsKey(commandId))
            {
                Debug.LogError($"调用失败: 实体 {name} 不存在命令 {commandId}");
                return;
            }

            //调用
            remoteCommands[commandId](this);
        }
        #endregion







        #region 有参远程指令
        readonly Dictionary<string, Action<Entity, JObject>> paramRemoteCommands = new();

        public void RegisterParamRemoteCommand(string commandId, Action<Entity, JObject> action)
        {
            //检查 commandId
            if (commandId.IsNullOrWhiteSpace())
            {
                Debug.LogError($"实体 {name} 注册了一个 {nameof(commandId)} 为空的命令");
                return;
            }

            //检查 action
            if (action == null)
            {
                Debug.LogError($"实体 {name} 注册了一个 {nameof(action)} 为空的命令");
                return;
            }

            //检查重复
            if (paramRemoteCommands.ContainsKey(commandId))
            {
                Debug.LogError($"实体 {name} 已经注册了命令 {commandId}, 不要反复注册");
                return;
            }


            //添加命令
            paramRemoteCommands.Add(commandId, action);
        }

        [ServerRpc]
        public void ServerExecuteParamRemoteCommand(string commandId, JObject param, NetworkConnection caller = null)
        {
            _ExecuteParamRemoteCommand(commandId, param);
        }

        [ServerRpc]
        public void ServerToClientsExecuteParamRemoteCommand(string commandId, JObject param, NetworkConnection caller = null)
        {
            _ClientsExecuteParamRemoteCommand(commandId, param, caller);
        }

        [ServerRpc]
        public void ServerToConnectionExecuteParamRemoteCommand(string commandId, JObject param, NetworkConnection caller = null)
        {
            _ConnectionExecuteParamRemoteCommand(commandId, param, caller);
        }



        [ClientRpc]
        private void _ClientsExecuteParamRemoteCommand(string commandId, JObject param, NetworkConnection caller = null)
        {
            _ExecuteParamRemoteCommand(commandId, param);
        }

        [ConnectionRpc]
        private void _ConnectionExecuteParamRemoteCommand(string commandId, JObject param, NetworkConnection caller = null)
        {
            _ExecuteParamRemoteCommand(commandId, param);
        }

        private void _ExecuteParamRemoteCommand(string commandId, JObject param)
        {
            //检查命令
            if (!paramRemoteCommands.ContainsKey(commandId))
            {
                Debug.LogError($"调用失败: 实体 {name} 不存在命令 {commandId}");
                return;
            }

            //调用
            paramRemoteCommands[commandId](this, param);
        }
        #endregion

















        public void Kill()
        {
            if (isPlayer)
                return;

            WriteDataToWorldSave();
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





        #region 生命周期

        protected virtual void Awake()
        {
            //初始化信息
            isPlayer = this is Player;
            isNotPlayer = !isPlayer;
            isNPC = this is NPC;
            isNotNPC = !isNPC;

            //初始化组件
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<BoxCollider2D>();

            //初始化网络
            netIdentity = GetComponent<NetworkIdentity>();
            netId = netIdentity.netId;
            isServer = Server.isServer;
            isClient = Client.isClient;
            isOwned = netIdentity.isOwned;
            isHost = isServer && isClient;
            isLocalPlayer = Client.localPlayer == this;
        }

        public virtual void Initialize()
        {
            rb.gravityScale = data.gravity;
            mainCollider.size = data.colliderSize;
            mainCollider.offset = data.colliderOffset;

            SetAutoDestroyTime();
        }

        public virtual void AfterInitialization()
        {
            EntityCenter.AddEntity(this);
        }

        protected virtual void OnDestroy()
        {
            EntityCenter.RemoveEntity(this);
            SyncPacker.EntitiesIDTable.Remove(netId);
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

            //如果区域序列改变
            if (regionIndex != newRegionIndex)
            {
                //把实体数据转移到新的区域
                //玩家的数据是自行管理的，这里要排除玩家
                if (isNotPlayer)
                {
                    Debug.Log($"实体 {name} 从 {regionIndex} 区域移到 {newRegionIndex} 区域");
                    GFiles.world.GetOrAddRegion(regionIndex).RemoveEntity(Init.save);
                    GFiles.world.GetOrAddRegion(newRegionIndex).AddEntity(Init.save);
                }

                //更改值
                regionIndex = newRegionIndex;
            }



            //自动销毁
            if (ShouldBeAutoDestroyed())
            {
                DestroyEntityOnServer();
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (Block.TryGetBlockFromCollider(other, out Block block))
            {
                block.OnEntityEnter(this);
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (Block.TryGetBlockFromCollider(other, out Block block))
            {
                block.OnEntityStay(this);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (Block.TryGetBlockFromCollider(other, out Block block))
            {
                block.OnEntityExit(this);
            }
        }

        protected virtual void OnEnable()
        {
            OnHealthChange += CheckHealthOnHealthChange;
        }

        protected virtual void OnDisable()
        {
            OnHealthChange -= CheckHealthOnHealthChange;
        }


#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {

        }
#endif
        #endregion

        public bool ShouldBeAutoDestroyed()
        {
            if (isPlayer || isNPC || Tools.time < timeToAutoDestroy)
                return false;

            var position = transform.position;

            foreach (var player in PlayerCenter.all)
            {
                //如果有任意一个玩家在 15 格以内就不自动销毁实体
                if ((player.transform.position - position).sqrMagnitude < 225)
                {
                    return false;
                }
            }

            return true;
        }

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












        /* -------------------------------------------------------------------------- */
        /*                                    受伤逻辑                                    */
        /* -------------------------------------------------------------------------- */
        public virtual void OnGetHurtServer(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }
        public virtual void OnGetHurtClient(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }



        public void CheckHealthOnHealthChange()
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
            if (!isHurtable)
                return;

            ServerTakeDamage(damage, invincibleTime, hurtPos, impactForce, null);
        }



        [ServerRpc]
        void ServerTakeDamage(int damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            if (isDead || isHurting)
                return;


            //根据防御值计算实际伤害
            CalculateActualDamage(ref damage);

            //扣血并刷新无敌时间
            health -= damage;
            this.invincibleTime = invincibleTime;

            OnGetHurtServer(damage, invincibleTime, damageOriginPos, impactForce, caller);



            if (isNotPlayer)
            {
                //应用击退效果
                if (rb) rb.velocity = impactForce;

                //如果实体受到伤害, 就延迟自动销毁的时间
                SetAutoDestroyTime();
            }



            Debug.Log($"{transform.GetPath()} 收到伤害, 值为 {damage}, 新血量为 {health}");

            ClientTakeDamage(damage, invincibleTime, damageOriginPos, impactForce, caller);
        }

        public void CalculateActualDamage(ref int damage)
        {
            if (this is IInventoryOwner owner)
            {
                var inventory = owner.GetInventory();
                if (inventory != null)
                {
                    damage = Mathf.Max(damage - owner.TotalDefense, 1);
                }
            }
        }

        [ClientRpc]
        void ClientTakeDamage(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            if (!Server.isServer)
                Debug.Log($"{transform.GetPath()} 收到伤害, 值为 {damage}");

            //播放受伤音频
            if (!data.hurtAudioId.IsNullOrWhiteSpace())
                GAudio.Play(data.hurtAudioId);

            //记录受伤时的时间
            previousHurtTime = Tools.time;

            OnGetHurtClient(damage, invincibleTime, damageOriginPos, impactForce, caller);

            //应用击退效果
            if (isPlayer && isOwned)
                rb.velocity = impactForce;

            //受伤特效
            GM.instance.bloodParticlePool.Get(this);
            GM.instance.damageTextPool.Get(this, damage);
        }





        /* -------------------------------------------------------------------------- */
        /*                                    死亡逻辑                                    */
        /* -------------------------------------------------------------------------- */
        public virtual async void OnDeathServer()
        {
            SummonCoins();
            SummonDrops();

            //? 等待一秒是为了防止客户端延迟过高导致报错
            await 1;
            DestroyEntityOnServer();
        }
        public virtual void OnDeathClient()
        {
            //让玩家看不见实体
            Hide();
            ColliderOff();
            this.enabled = false;
        }



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
        protected void ClientDeath(NetworkConnection caller) => MethodAgent.DebugRun(() =>
        {
            //Debug.Log($"客户端: 实体 {name} 已死亡");

            //? 不要使用 RpcDeath 来回收资源等, 资源回收应该放在 OnDestroy 中, 因为服务器可能会在调用 RpcDeath 前删除物体, ClientDeath 是用来显示死亡动效的
            OnDeathClient();
        });















        private void SummonCoins()
        {
            if (data.coinCount > 0)
                managerGame.SummonCoinEntity(transform.position, data.coinCount);
        }

        private void SummonDrops()
        {
            foreach (DropData drop in data.drops)
            {
                managerGame.SummonDrop(transform.position, drop.id, drop.count);
            }
        }

        public virtual void WriteDataToSaveObject(EntitySave save)
        {
            save.WriteFromEntity(this);
        }

        public void WriteDataToWorldSave()
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
                foreach (PlayerSave save in GFiles.world.playerSaves)
                {
                    if (save.id == player.playerName)
                    {
                        WriteDataToSaveObject(save);

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
                            WriteDataToSaveObject(save);

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
                        region.RemoveEntity(save);
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
    }
}
