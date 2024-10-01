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
        public static int entityLayer { get; internal set; }
        public static int entityLayerMask { get; internal set; }
        public static Vector2 impactForceConst { get; } = new(12, 16f);
        public const int height = 2;





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
        public bool isHurtable = true;
        public float previousHurtTime;
        public float temperatureEffectEndTime;
        public TemperatureEffectType temperatureEffectType;
        public enum TemperatureEffectType : byte
        {
            OnFire,
            Frozen,
        }
        public enum TemperatureEffectState : byte
        {
            None,
            OnFire,
            Frozen,
        }
        public TemperatureEffectState GetTemperatureEffectState()
        {
            if (Tools.time > temperatureEffectEndTime)
                return TemperatureEffectState.None;

            if (temperatureEffectType == TemperatureEffectType.OnFire)
                return TemperatureEffectState.OnFire;

            return TemperatureEffectState.Frozen;
        }









        #region 同步变量
        [Sync, SyncDefaultValue(0f)] public float invincibleTime;
        [Sync, SyncDefaultValue(false)] public bool isDead;
        [Sync] public JObject customData;

        #region 血量
#if UNITY_EDITOR
        [Button, ServerRpc] void editor_health_set(int value, NetworkConnection caller) => health = value;
#endif
        [Sync(nameof(OnHealthChangeMethod)), SyncDefaultValue(DEFAULT_HEALTH)] public int health;
        public const int DEFAULT_HEALTH = 100;
        public Action<Entity> OnHealthChange = entity =>
        {
            if (entity.isServer && entity.health <= 0)
            {
                entity.Death();
            }
        };
        private void OnHealthChangeMethod(byte[] _)
        {
            OnHealthChange(this);
        }
        #endregion

        #region 最大生命值

        [Sync, SyncDefaultValueFromMethod(nameof(GetDefaultMaxHealth), true)] public int maxHealth;

        public static int GetDefaultMaxHealth(EntityData data)
        {
            //玩家是固定的
            if (data.IsPlayer)
                return DEFAULT_HEALTH;

            //其他一律按照 json 来
            return data.maxHealth;
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
            if (entity is Player player && player.isLocalPlayer && entity.TryGetRegion(out var region))
            {
                InternalUIAdder.instance.SetTitleText($"{GameUI.CompareText(region.biomeId)}\n<size=55%>{GameUI.CompareText($"ori:region_index_y.{region.index.y}")}</size>");
            }
        };

        #endregion

        #endregion









        /* -------------------------------------------------------------------------- */
        /*                                 Entity 远程指令                                */
        /* 作用：让任意脚本都可以轻易地以实体为载体，远程调用任意命令，并允许传参。 */
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

















        public void Unload()
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

        /// <summary>
        /// 注意：服务器调用该方法时客户端可能还未初始化完毕
        /// </summary>
        public virtual void Initialize()
        {
            customData = ModifyCustomData(customData);

            rb.gravityScale = data.gravity;
            mainCollider.size = data.colliderSize;
            mainCollider.offset = data.colliderOffset;

            SetAutoDestroyTime();
        }

        /// <summary>
        /// 注意：服务器调用该方法时客户端可能还未初始化完毕
        /// </summary>
        public virtual void AfterInitialization()
        {
            LoadFromCustomData();

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

            SetColorOfSpriteRenderers(DecideColorOfSpriteRenderers());
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
                    //从当前区域移除实体
                    GFiles.world.GetOrAddRegion(regionIndex).RemoveEntity(Init.save);

                    //创建新区域（如果不存在）
                    if (!GFiles.world.TryGetRegion(newRegionIndex, out Region region))
                    {
                        region = new()
                        {
                            index = newRegionIndex,
                        };
                        GFiles.world.AddRegion(region);
                    }

                    //添加实体到新区域
                    region.AddEntity(Init.save);

                    //如果新区域未生成过，则销毁实体
                    if (!region.generatedAlready)
                    {
                        Debug.Log($"实体 {name} 从 {regionIndex} 区域移到了未生成过的区域 {newRegionIndex}, 同时实体被销毁");
                        Unload();
                    }
                    else
                    {
                        Debug.Log($"实体 {name} 从 {regionIndex} 区域移到了已生成过的区域 {newRegionIndex}");
                    }
                }

                //更改值
                regionIndex = newRegionIndex;
            }

            //烧伤
            if (GetTemperatureEffectState() == TemperatureEffectState.OnFire)
            {
                TakeDamage(2, 0.3f, transform.position, Vector2.zero);
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
                OnBlockEnter(block);
                block.OnEntityEnter(this);
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (Block.TryGetBlockFromCollider(other, out Block block))
            {
                OnBlockStay(block);
                block.OnEntityStay(this);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (Block.TryGetBlockFromCollider(other, out Block block))
            {
                OnBlockExit(block);
                block.OnEntityExit(this);
            }
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }


#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {

        }
#endif
        #endregion





        /* -------------------------------------------------------------------------- */
        /*                                    自动销毁                                    */
        /* -------------------------------------------------------------------------- */
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





        /* -------------------------------------------------------------------------- */
        /*                                     方块                                     */
        /* -------------------------------------------------------------------------- */
        protected virtual void OnBlockEnter(Block block) { }
        protected virtual void OnBlockStay(Block block) { }
        protected virtual void OnBlockExit(Block block) { }






        [ServerRpc]
        public void ServerSetVelocity(Vector2 velocity, NetworkConnection caller = null)
        {
            rb.velocity = velocity;
        }






        /* -------------------------------------------------------------------------- */
        /*                                    温度效果                                    */
        /* -------------------------------------------------------------------------- */
        [ServerRpc]
        public void ServerSetTemperatureEffect(TemperatureEffectType value, NetworkConnection caller = null)
        {
            ClientSetTemperatureEffect(value);
        }

        [ClientRpc]
        void ClientSetTemperatureEffect(TemperatureEffectType value, NetworkConnection caller = null)
        {
            temperatureEffectType = value;
            temperatureEffectEndTime = Tools.time + 3; //持续三秒
        }

        [ServerRpc]
        public void ServerClearTemperatureEffect(NetworkConnection caller = null)
        {
            ClientClearTemperatureEffect();
        }

        [ClientRpc]
        void ClientClearTemperatureEffect(NetworkConnection caller = null)
        {
            temperatureEffectEndTime = 0;
        }




        /* -------------------------------------------------------------------------- */
        /*                                     渲染器                                    */
        /* -------------------------------------------------------------------------- */
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

        public void SetColorOfSpriteRenderers(float r, float g, float b) => SetColorOfSpriteRenderers(new(r, g, b));
        public void SetColorOfSpriteRenderers(float r, float g, float b, float a) => SetColorOfSpriteRenderers(new(r, g, b, a));
        public void SetColorOfSpriteRenderers(Color color)
        {
            foreach (var sr in spriteRenderers)
            {
                sr.color = color;
            }
        }

        public virtual Color DecideColorOfSpriteRenderers()
        {
            if (isHurting)
                return new(1, 0.5f, 0.5f);

            if (GetTemperatureEffectState() == TemperatureEffectState.Frozen)
                return Color.blue;

            return Color.white;
        }





        /* -------------------------------------------------------------------------- */
        /*                                    受伤逻辑                                    */
        /* -------------------------------------------------------------------------- */
        public virtual void OnGetHurtServer(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }
        public virtual void OnGetHurtClient(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller) { }





        public bool CanTakeDamage() => !isDead && !isHurting && isHurtable && GetTemperatureEffectState() != TemperatureEffectState.Frozen;
        public void CalculateActualDamage(ref int damage)
        {
            //计算防御值
            if (this is IInventoryOwner owner)
            {
                var inventory = owner.GetInventory();
                if (inventory != null)
                {
                    damage = Mathf.Max(damage - owner.TotalDefense, 1);
                }
            }
        }




        /// <returns>是否向服务器发送了伤害请求</returns>
        [Button]
        public virtual bool TakeDamage(int damage) => TakeDamage(damage, 0.1f, transform.position, Vector2.zero);

        /// <returns>是否向服务器发送了伤害请求</returns>
        public bool TakeDamage(int damage, float invincibleTime, Vector2 hurtPos, Vector2 impactForce)
        {
            //检查能否被伤害
            if (!CanTakeDamage())
                return false;

            ServerTakeDamage(damage, invincibleTime, hurtPos, impactForce, null);
            return true;
        }




        [ServerRpc]
        void ServerTakeDamage(int damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            //检查能否被伤害
            if (!CanTakeDamage())
                return;


            //根据防御值等数据计算实际伤害
            CalculateActualDamage(ref damage);


            //扣血并刷新无敌时间
            health -= damage;
            this.invincibleTime = invincibleTime;


            //排除玩家是因为玩家的速度由各自的客户端控制
            if (isNotPlayer)
            {
                //应用击退效果
                if (rb) rb.AddVelocity(impactForce);

                //如果实体受到伤害, 就延迟自动销毁的时间
                SetAutoDestroyTime();
            }


            Debug.Log($"{transform.GetPath()} 收到伤害, 值为 {damage}, 新血量为 {health}");


            OnGetHurtServer(damage, invincibleTime, damageOriginPos, impactForce, caller);
            ClientTakeDamage(damage, invincibleTime, damageOriginPos, impactForce, caller);
        }

        [ClientRpc]
        void ClientTakeDamage(float damage, float invincibleTime, Vector2 damageOriginPos, Vector2 impactForce, NetworkConnection caller)
        {
            //排除服务器是因为服务器已经日志过了
            if (!Server.isServer)
                Debug.Log($"{transform.GetPath()} 收到伤害, 值为 {damage}");

            //播放受伤音频
            if (!data.hurtAudioId.IsNullOrWhiteSpace())
                GAudio.Play(data.hurtAudioId, transform.position);

            //记录受伤时的时间
            previousHurtTime = Tools.time;

            OnGetHurtClient(damage, invincibleTime, damageOriginPos, impactForce, caller);

            //应用本地玩家击退效果
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
            //生成战利品
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



        [Button]
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
                GM.instance.SummonCoinEntity(transform.position, data.coinCount);
        }

        protected virtual void SummonDrops()
        {
            foreach (DropData drop in data.drops)
            {
                GM.instance.SummonDrop(transform.position, drop.id, drop.count);
            }
        }






        /* -------------------------------------------------------------------------- */
        /*                                     存档                                     */
        /* -------------------------------------------------------------------------- */

        public virtual JObject ModifyCustomData(JObject data)
        {
            return data;
        }

        public virtual void LoadFromCustomData()
        {

        }

        public virtual void WriteToEntitySave(EntitySave save)
        {
            save.WriteFromEntity(this);
        }

        public virtual EntitySave GetEntitySaveObjectFromWorld()
        {
            //将实体数据写入
            foreach (Region region in GFiles.world.regionData)
            {
                foreach (EntitySave save in region.entities)
                {
                    //如果匹配到自己
                    if (save.saveId == Init.save.saveId)
                    {
                        return save;
                    }
                }
            }

            return null;
        }

        public void WriteDataToWorldSave()
        {
            if (GFiles.world == null)
            {
                Debug.LogError("世界为空, 无法写入保存数据");
                return;
            }

            var save = GetEntitySaveObjectFromWorld();
            if (save == null)
            {
                Debug.LogWarning($"未在存档中找到实体 {Init.save.saveId}, 保存失败");
                return;
            }
            else
            {
                WriteToEntitySave(save);
            }
        }

        public void ClearSaveDatum()
        {
            if (GFiles.world == null)
            {
                Debug.LogError("世界为空, 无法清除实体数据");
                return;
            }

            if (isPlayer)
            {
                Debug.LogWarning("玩家实体不能清除保存数据");
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

        /// <returns>true 代表右，false 代表左</returns>
        public bool GetOrientation() => transform.localScale.x > 0;






        static Collider2D[] tempGetEntitiesInRadius = new Collider2D[15];
        public static List<Entity> GetEntitiesInRadius(Vector2 point, float radius)
        {
            List<Entity> entities = new();
            RayTools.OverlapCircleNonAlloc(point, radius, tempGetEntitiesInRadius);

            foreach (var item in tempGetEntitiesInRadius)
            {
                if (item == null)
                    return entities;

                if (item.TryGetComponent(out Entity entity))
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }






        #region 实体画布

        public Canvas usingCanvas { get; private set; }

        public void GainNewEntityCanvas()
        {
            usingCanvas = EntityCanvasPool.Get();
            usingCanvas.transform.SetParent(transform, false);
            usingCanvas.transform.localPosition = Vector3.zero;
            usingCanvas.transform.localScale = new Vector2(0.075f, 0.075f);
            usingCanvas.enabled = true;
        }

        public Canvas GetOrAddEntityCanvas()
        {
            if (!usingCanvas)
                GainNewEntityCanvas();

            return usingCanvas;
        }

        public bool TryGetEntityCanvas(out Canvas canvas)
        {
            canvas = usingCanvas;
            return canvas != null;
        }

        public static class EntityCanvasPool
        {
            public static Stack<Canvas> stack = new();

            public static Canvas Get()
            {
                var canvas = (stack.Count == 0) ? Generation() : stack.Pop();

                return canvas;
            }

            public static void Recover(Canvas canvas)
            {
                canvas.enabled = false;
                canvas.transform.SetParent(null);
                canvas.transform.DestroyChildren();
                stack.Push(canvas);
            }

            public static Canvas Generation()
            {
                GameObject go = new("EntityCanvas");
                Canvas canvas = go.AddComponent<Canvas>();

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 20;

                return canvas;
            }
        }
        #endregion
    }
}
