using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;
using SP.Tools;
using SP.Tools.Unity;

namespace GameCore.High
{
    public static class NetworkCallbacks
    {
        #region 服务器
        public static event Action<NetworkConnectionToClient> OnClientReady = conn =>
        {
            Debug.Log($"客户端 {conn.address}:{conn.connectionId} 已准备完毕");
        };
        public static event Action<NetworkConnectionToClient> OnClientConnect = conn =>
        {
            //如果是 Host 模式, 且客户端不是 Host 连接, 就检查服务器是否还未添加玩家或是未加载第一区域, 如果是就断开连接
            if (Client.isClient && conn != Server.localConnection && (!Player.TryGetLocal(out Player player) || !player.generatedFirstRegion))
            {
                Debug.LogWarning($"客户端 {conn.address} (id={conn.connectionId}) 加入服务器时, 服务器正在加载第一区域, 请稍等");
                conn.Disconnect();
                return;
            }

            Debug.Log($"新客户端接入, 目前共有 {NetworkServer.connections.Count} 名玩家");
        };
        public static event Action<NetworkConnectionToClient> OnClientDisconnect = conn =>
        {
            Debug.Log($"客户端 {conn.address}:{conn.connectionId} 断开了连接");
        };
        public static event Action<NetworkConnectionToClient, TransportError, string> OnClientError = (conn, error, reason) =>
        {
            Exception ex = new(reason);

            if (conn == null)
                Debug.LogError("一个未知客户端出现了异常");
            else
                Tools.LogException(ex, $"客户端 {conn.address}:{conn.connectionId} 出现了问题 ({error}) : {reason}");
        };
        public static event Action<NetworkConnectionToClient, NMClientChangeScene> OnClientChangeScene = (b, c) => { };
        public static event Action OnStartServer = () =>
        {
            Debug.Log("开启了服务器");

            NetworkCallbacks.OnTimeToServerCallback();
        };
        public static event Action OnStopServer = () =>
        {
            Debug.Log("关闭了服务器");
        };
        public static event Action<NetworkConnectionToClient> OnAddPlayer = b => { };

        public static Action<EntityInit, EntityData, NMSummon, NetworkConnectionToClient> OnServerSummonEntity = (entity, datum, n, conn) =>
        {
            // StringBuilder sb = new(nameof(OnServerSummonEntity));

            // sb.Append(": ");
            // sb.Append(conn.address);
            // sb.Append(" 请求生成了 ");
            // sb.Append(datum.id);
            // sb.Append(" (");
            // sb.Append(datum.behaviourType.FullName);
            // sb.Append(")");

            // Debug.Log(sb.ToString());
        };

        public static void OnClientReadyThenRemove(Action<NetworkConnectionToClient> action)
        {
            OnClientReady += Method;

            void Method(NetworkConnectionToClient conn)
            {
                OnClientReady -= Method;
                action(conn);
            }
        }

        internal static void CallOnClientReady(NetworkConnectionToClient conn) => OnClientReady(conn);
        internal static void CallOnClientConnect(NetworkConnectionToClient conn) => OnClientConnect(conn);
        internal static void CallOnClientDisconnect(NetworkConnectionToClient conn) => OnClientDisconnect(conn);
        internal static void CallOnClientError(NetworkConnectionToClient conn, TransportError error, string reason) => OnClientError(conn, error, reason);
        internal static void CallOnClientChangeScene(NetworkConnectionToClient conn, NMClientChangeScene nm) => OnClientChangeScene(conn, nm);
        internal static void CallOnStartServer() => OnStartServer();
        internal static void CallOnStopServer() => OnStopServer();
        internal static void CallOnAddPlayer(NetworkConnectionToClient conn) => OnAddPlayer(conn);
        #endregion

        #region 客户端
        public static event Action OnConnectToServer = () =>
        {
            Debug.Log($"连接了服务器");

            EntityInit.isFirstEntityInit = true;

            NetworkCallbacks.OnTimeToClientCallback();
        };
        public static event Action OnDisconnectFromServer = () =>
        {
            Debug.Log($"与服务器断开了连接");
        };
        public static event Action<TransportError, string> OnLocalClientError = (error, reason) =>
        {
            Exception ex = new(reason);
            Tools.LogException(ex, $"客户端因以下异常断开连接 ({error}): ");
        };
        public static event Action OnStartClient = () =>
        {
            Debug.Log($"进入了服务器");
        };
        public static event Action OnStopClient = () =>
        {
            Debug.Log("客户端关闭");

            EntityInit.isFirstEntityInit = true;
        };

        public static void OnConnectToServerThenRemove(Action action)
        {
            OnConnectToServer += Method;

            void Method()
            {
                OnConnectToServer -= Method;
                action();
            }
        }

        internal static void CallOnConnectToServer() => OnConnectToServer();
        internal static void CallOnDisconnectFromServer() => OnDisconnectFromServer();
        internal static void CallOnLocalClientError(TransportError error, string reason) => OnLocalClientError(error, reason);
        internal static void CallOnStartClient() => OnStartClient();
        internal static void CallOnStopClient() => OnStopClient();
        #endregion

        #region 服务器 & 客户端
        public static Action OnTimeToServerCallback = () => { };
        public static Action OnTimeToClientCallback = () => { };

        public static Action OnStartHost = () =>
        {
            Debug.Log("开启并加入了服务器");
        };
        public static Action OnStopHost = () =>
        {
            Debug.Log("关闭并退出了服务器");
        };
        #endregion


        static readonly Dictionary<string, EntityData> EntityTemps = new();
        static EntityData ReadFromEntityTemps(string id)
        {
            if (id.IsNullOrWhiteSpace())
                return null;

            if (EntityTemps.TryGetValue(id, out EntityData data))
            {
                return data;
            }

            //寻找 Entity
            foreach (var mod in ModFactory.mods)
            {
                foreach (var entityTemp in mod.entities)
                {
                    //找到合适的 type etc.
                    if (entityTemp.behaviourType != null && entityTemp.id == id)
                    {
                        data = entityTemp;
                        break;
                    }
                }
            }

            EntityTemps.Add(id, data);
            return data;
        }

        //生成实体的逻辑
        internal static void SummonEntity(NetworkConnectionToClient conn, NMSummon n)
        {
            EntityData data = ReadFromEntityTemps(n.entityId);

            if (data != null)
            {
                //生成并添加实体数据
                EntitySave saveDatum = new()
                {
                    id = n.entityId,
                    pos = n.pos,
                    saveId = n.saveId,
                    customData = n.customData,
                    health = n.health,
                };

                if (n.saveIntoRegion)
                {
                    if (GFiles.world.TryGetRegion(PosConvert.WorldPosToRegionIndex(saveDatum.pos), out var region))
                    {
                        region.entities.Add(saveDatum);
                    }
                    else
                    {
                        Debug.LogError($"尝试把实体存档添加到世界时失败! 位置: {n.pos}");
                    }
                }

                //在服务器生成并初始化
                EntityInit init = GameObject.Instantiate(GetEntityPrefab(data), n.pos, Quaternion.identity);

                /* ---------------------------------- 执行初始化 --------------------------------- */
                init.generationId = data.id;
                init.data = data;
                init.save = saveDatum;
                init.gameObject.name = data.id;

                OnServerSummonEntity(init, data, n, conn);
                NetworkServer.Spawn(init.gameObject);
                return;
            }

            Debug.LogWarning($"{MethodGetter.GetCurrentMethodName()}: 实体生成失败, 未匹配到实体 {n.entityId} (也可能是行为包未正确设置)");
        }

        internal static EntityInit GetEntityPrefab(EntityData entity)
        {
            if (entity.id == EntityID.Drop)
                return GInit.instance.GetDropPrefab();

            if (entity.behaviourType.IsSubclassOf(typeof(Enemy)))
                return GInit.instance.GetEnemyPrefab();

            if (entity.behaviourType.IsSubclassOf(typeof(NPC)))
                return GInit.instance.GetNPCPrefab();

            if (entity.behaviourType.IsSubclassOf(typeof(Creature)))
                return GInit.instance.GetCreaturePrefab();

            return GInit.instance.GetEntityPrefab();
        }
    }
}
