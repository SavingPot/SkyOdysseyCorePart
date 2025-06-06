using kcp2k;
using Mirror;
using SP.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Runtime.InteropServices;
using Org.BouncyCastle.Asn1.Esf;

namespace GameCore.Network
{
    //TODO: 适配ipv6
    public class ManagerNetwork : NetworkManager
    {
        #region 单例实现
        private static ManagerNetwork _instance;

        public static ManagerNetwork instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType<ManagerNetwork>();

                if (!_instance)
                {
                    if (!Application.isPlaying)
                        return null;

                    SummonInstance();
                    Debug.LogWarning($"不存在单例物体, 已生成 ({typeof(ManagerNetwork)})");
                }

                return _instance;
            }
        }

        public static bool HasInstance() => _instance;

        public static void SummonInstance()
        {
            if (!Application.isPlaying)
                return;

            _instance = Instantiate(GInit.instance.managerNetworkPrefab).GetComponent<ManagerNetwork>();
        }

        public static async void WhenReady(Action<ManagerNetwork> action)
        {
            if (action == null)
            {
                Debug.LogError($"{nameof(action)} 值为空");
                return;
            }

            while (!HasInstance())
                await Time.fixedDeltaTime;

            while (!instance.ready)
                await Time.fixedDeltaTime;

            action(instance);
        }

        protected virtual void DestroyOrSave()
        {
            if (FindObjectsOfType<ManagerNetwork>().Length > 1)
                Destroy(gameObject);
            else
                DontDestroyOnLoad(gameObject);
        }

        public bool ready { get; protected set; }
        #endregion

        private Player _localPlayer;
        public Player localPlayer
        {
            get
            {
                if (!_localPlayer && NetworkClient.localPlayer)
                    _localPlayer = NetworkClient.localPlayer.GetComponent<Player>();

                return _localPlayer;
            }
        }

        [SerializeField]
        private ServerDiscovery _discovery;
        public ServerDiscovery discovery { get => _discovery; }

        public ThreadedKcpTransport kcp { get; private set; }

        #region Unity Callbacks
        /// <summary>
        /// Runs on both Server and Client.
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Awake()
        {
            DestroyOrSave();

            if (!_instance)
                _instance = this;

            base.Awake();

            kcp = GetComponent<ThreadedKcpTransport>();
        }

        /// <summary>
        /// Runs on both Server and Client
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Start()
        {
            base.Start();

            NetworkClient.RegisterPrefab(GInit.instance.GetEntityPrefab().gameObject);
            NetworkClient.RegisterPrefab(GInit.instance.GetDropPrefab().gameObject);
            NetworkClient.RegisterPrefab(GInit.instance.GetEnemyPrefab().gameObject);
            NetworkClient.RegisterPrefab(GInit.instance.GetNPCPrefab().gameObject);
            NetworkClient.RegisterPrefab(GInit.instance.GetCreaturePrefab().gameObject);

            static void OnServerGetNMClientChangeScene(NetworkConnectionToClient conn, NMClientChangeScene n)
            {
                NetworkCallbacks.CallOnClientChangeScene(conn, n);
            }

            static void OnServerGetNMRpc(NetworkConnectionToClient conn, NMRpc m)
            {
                switch (m.callType)
                {
                    //在服务器上调用: 收到了客户端的调用请求: 直接调用
                    case RpcType.ServerRpc:
                        Rpc.LocalCall(m.methodPath, conn, m.parameter0, m.parameter1, m.parameter2, m.parameter3, m.parameter4, m.instance);
                        break;

                    //发送给全部客户端执行: 收到了客户端的广播请求: 把消息广播给所有客户端 
                    case RpcType.ClientRpc:
                        Server.Send(m);
                        break;

                    default:
                        Debug.LogError($"参数错误, 服务器错误地收到了类型为 {m.callType} 的方法");
                        break;
                }
            }

            static void OnClientGetNMRpc(NMRpc m)
            {
                //无论什么 CallType, 只要服务器发送了就执行
                Rpc.LocalCall(m.methodPath, Client.connection, m.parameter0, m.parameter1, m.parameter2, m.parameter3, m.parameter4, m.instance);
            }

            NetworkCallbacks.OnTimeToServerCallback += () =>
            {
                Server.Callback<NMAddPlayer>((conn, nm) =>
                {
                    //TODO: 告诉客户端为什么被踢了, 并等待客户端的回复, 客户端自行断开连接, 如果客户端长时间不回复就直接断连
                    /* --------------------------------- 检查游戏版本 --------------------------------- */
                    if (nm.gameVersion != GInit.gameVersion)
                    {
                        Debug.LogWarning($"客户端 {conn.address} (id={conn.connectionId}) 与服务器游戏版本不一致, 不允许共同游玩");
                        conn.Disconnect();
                        return;
                    }

                    //TODO: 告诉客户端为什么被踢了, 并等待客户端的回复, 客户端自行断开连接, 如果客户端长时间不回复就直接断连
                    /* --------------------------------- 检查玩家名称 --------------------------------- */
                    if (nm.playerName.IsNullOrWhiteSpace())
                    {
                        Debug.LogWarning($"客户端 {conn.address} (id={conn.connectionId}) 的玩家名为空");
                        conn.Disconnect();
                        return;
                    }

                    /* ---------------------------------- 检查模组 ---------------------------------- */
                    List<(string modId, string modVersion)> unexpectedMods = new();
                    //检查客户端是否不包含服务器的模组
                    foreach (var serverMod in ModFactory.mods)
                    {
                        for (int clientModIndex = 0; clientModIndex < nm.modIds.Count; clientModIndex++)
                        {
                            var clientModId = nm.modIds[clientModIndex];
                            var clientModVersion = nm.modVersions[clientModIndex];

                            if (serverMod.info.id == clientModId && serverMod.info.version == clientModVersion)
                            {
                                goto thisModIsOkay;
                            }
                        }

                        unexpectedMods.Add((serverMod.info.id, serverMod.info.version));

                    thisModIsOkay: { }
                    }
                    //检查服务器是否不包含客户端的模组
                    for (int clientModIndex = 0; clientModIndex < nm.modIds.Count; clientModIndex++)
                    {
                        var clientModId = nm.modIds[clientModIndex];
                        var clientModVersion = nm.modVersions[clientModIndex];

                        foreach (var serverMod in ModFactory.mods)
                        {
                            if (serverMod.info.id == clientModId && serverMod.info.version == clientModVersion)
                            {
                                goto thisModIsOkay;
                            }
                        }

                        unexpectedMods.Add((clientModId, clientModVersion));

                    thisModIsOkay: { }
                    }

                    //TODO: 告诉客户端为什么被踢了, 并等待客户端的回复, 客户端自行断开连接, 如果客户端长时间不回复就直接断连
                    if (unexpectedMods.Count != 0)
                    {
                        string errorMessage = $"客户端 {conn.address} (id={conn.connectionId}) 的模组列表与服务器不一致!";
                        foreach (var item in unexpectedMods)
                        {
                            errorMessage += $"{item}\n";
                        }
                        Debug.LogWarning(errorMessage);
                        conn.Disconnect();
                        return;
                    }

                    //TODO: 告诉客户端为什么被踢了, 并等待客户端的回复, 客户端自行断开连接, 如果客户端长时间不回复就直接断连
                    /* --------------------------------- 检查玩家皮肤 --------------------------------- */
                    if (nm.skinHead == null || nm.skinBody == null || nm.skinLeftArm == null || nm.skinRightArm == null || nm.skinLeftLeg == null || nm.skinRightLeg == null || nm.skinLeftFoot == null || nm.skinRightFoot == null)
                    {
                        Debug.LogWarning($"客户端 {conn.address} (id={conn.connectionId}) 的一个或多个皮肤部位精灵为空");
                        conn.Disconnect();
                        return;
                    }





                    /* ------------------------------- 检查完毕, 创建玩家 ------------------------------- */
                    GameObject player = Instantiate(playerPrefab);
                    player.name = $"Player {nm.playerName} [{conn.address}, id={conn.connectionId}]";

                    var init = player.GetComponent<EntityInit>();
                    init.entityId = EntityID.Player;
                    init.data = ModFactory.CompareEntity(EntityID.Player);

                    //寻找 PlayerSave
                    PlayerSave playerSaveTemp = null;
                    foreach (var save in GFiles.world.playerSaves)
                    {
                        if (save.id == nm.playerName)
                        {
                            playerSaveTemp = save;
                        }
                    }
                    if (playerSaveTemp == null)
                    {
                        playerSaveTemp = PlayerSave.NewPlayer(nm.playerName);

                        GFiles.world.playerSaves.Add(playerSaveTemp);
                        Debug.LogWarning($"未在存档中匹配到玩家 {nm.playerName} 的数据, 已自动添加");
                    }

                    //设置皮肤
                    playerSaveTemp.skinHead = nm.skinHead;
                    playerSaveTemp.skinBody = nm.skinBody;
                    playerSaveTemp.skinLeftArm = nm.skinLeftArm;
                    playerSaveTemp.skinRightArm = nm.skinRightArm;
                    playerSaveTemp.skinLeftLeg = nm.skinLeftLeg;
                    playerSaveTemp.skinRightLeg = nm.skinRightLeg;
                    playerSaveTemp.skinLeftFoot = nm.skinLeftFoot;
                    playerSaveTemp.skinRightFoot = nm.skinRightFoot;

                    //添加领主信息
                    PlayerLord lord = null;
                    bool lordFound = false;
                    foreach (var iLord in GFiles.world.lords)
                    {
                        if (iLord.id == nm.playerName)
                        {
                            //把玩家对应的领主的类型强制转为 PlayerLord (因为存档中同意存储 Lord 而非 PlayerLord)
                            lord = new(playerSaveTemp);
                            lord.WriteData(iLord);
                            lordFound = true;
                            break;
                        }
                    }
                    if (!lordFound)
                    {
                        bool hasHadZeroManor = false;
                        Vector2Int manorIndex = Vector2Int.zero;
                        foreach (var lordSave in GFiles.world.lords)
                        {
                            if (lordSave.manorIndex == Vector2Int.zero)
                            {
                                hasHadZeroManor = true;
                                break;
                            }
                        }

                        if (hasHadZeroManor)
                        {
                            foreach (var regionDatum in GFiles.world.regionData)
                            {
                                if (regionDatum.isManor)
                                {
                                    manorIndex = regionDatum.index;
                                    goto complete;
                                }
                            }

                            Debug.LogWarning($"严重错误！！！未在存档中匹配到玩家 {nm.playerName} 的领主信息, 但地图上没有多余庄园了！");
                            conn.Disconnect();
                            return;
                        }

                    complete:
                        lord = new(playerSaveTemp);
                        lord.WriteData(nm.playerName, manorIndex, null, playerSaveTemp.coin, new());
                        GFiles.world.CreateLord(lord);
                        Debug.LogWarning($"未在存档中匹配到玩家 {nm.playerName} 的领主信息, 已自动添加");
                    }

                    init.save = playerSaveTemp;

                    NetworkServer.AddPlayerForConnection(conn, player);
                    NetworkCallbacks.CallOnAddPlayer(conn);
                });
                Server.Callback<NMSummon>(NetworkCallbacks.SummonEntity);
                Server.Callback<NMClientChangeScene>(OnServerGetNMClientChangeScene);
                Server.Callback<NMRpc>(OnServerGetNMRpc);
            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {
                Client.Callback<NMRpc>(OnClientGetNMRpc);
            };

            ready = true;


            // NetworkCallbacks.OnTimeToServerCallback();
            // NetworkCallbacks.OnTimeToClientCallback();
        }

        /// <summary>
        /// 在服务器和客户端都执行
        /// </summary>
        public override void LateUpdate()
        {
            base.LateUpdate();
        }

        /// <summary>
        /// 在服务器和客户端都执行
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region Start & Stop

        /// <summary>
        /// 设置无头服务器的帧速率 (即 CMD 模式)
        /// <para>Override if you wish to disable the behavior or set your own tick rate.</para>
        /// </summary>
        public override void ConfigureHeadlessFrameRate()
        {
            base.ConfigureHeadlessFrameRate();
        }

        /// <summary>
        /// 当通过关闭窗口或点击编辑器 Stop 按钮时执行
        /// </summary>
        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
        }

        #endregion

        #region 场景回调

        /// <summary>
        /// This causes the server to switch scenes and sets the networkSceneName.
        /// <para>Clients that connect to this server will automatically switch to this scene. This is called automatically if onlineScene or offlineScene are set, but it can be called from user code to switch scenes again while the game is in progress. This automatically sets clients to be not-ready. The clients must call NetworkClient.Ready() again to participate in the new scene.</para>
        /// </summary>
        /// <param name="newSceneName"></param>
        public override void ServerChangeScene(string newSceneName)
        {
            base.ServerChangeScene(newSceneName);
        }

        /// <summary>
        /// Called from ServerChangeScene immediately before SceneManager.LoadSceneAsync is executed
        /// <para>This allows server to do work / cleanup / prep before the scene changes.</para>
        /// </summary>
        /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
        public override void OnServerChangeScene(string newSceneName) { }

        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
        /// </summary>
        /// <param name="sceneName">The name of the new scene.</param>
        public override void OnServerSceneChanged(string sceneName) { }

        /// <summary>
        /// Called from ClientChangeScene immediately before SceneManager.LoadSceneAsync is executed
        /// <para>This allows client to do work / cleanup / prep before the scene changes.</para>
        /// </summary>
        /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
        /// <param name="sceneOperation">Scene operation that's about to happen</param>
        /// <param name="customHandling">true to indicate that scene loading will be handled through overrides</param>
        public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) { }

        /// <summary>
        /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
        /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
        /// </summary>
        public override void OnClientSceneChanged()
        {
            base.OnClientSceneChanged();
        }

        #endregion

        #region 在服务器执行的回调

        /// <summary>
        /// 当客户端连接时在服务器调用.
        /// <para>Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.</para>
        /// </summary>
        /// <param name="conn">客户端的连接</param>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            NetworkCallbacks.CallOnClientConnect(conn);
        }

        /// <summary>
        /// 当客户端准备好时在服务器调用.
        /// <para>此方法默认通过调用 NetworkServer.SetClientReady() 来继续网络设置过程</para>
        /// </summary>
        /// <param name="conn">客户端的连接</param>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);

            //AddPlayer(conn);

            NetworkCallbacks.CallOnClientReady(conn);
        }

        /// <summary>
        /// 当客户端使用 ClientScene.AddPlayer 添加新玩家时在服务器上调用
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">客户端的连接</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {

        }

        /// <summary>
        /// 当客户端断开连接时在服务器调用
        /// <para>Use an override to decide what should happen when a disconnection is detected.</para>
        /// </summary>
        /// <param name="conn">客户端的连接</param>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            NetworkCallbacks.CallOnClientDisconnect(conn);
        }

        /// <summary>
        /// 传输引发异常时在服务器上调用
        /// <para>conn 可能为空.</para>
        /// </summary>
        /// <param name="conn">客户端的连接 (可能为空)</param>
        /// <param name="exception">传输抛出的异常.</param>
        public override void OnServerError(NetworkConnectionToClient conn, TransportError error, string reason)
        {
            NetworkCallbacks.CallOnClientError(conn, error, reason);
        }
        #endregion

        #region 在客户端执行的回调
        /// <summary>
        /// 当连接到服务器时在客户端上调用
        /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
        /// </summary>
        public override void OnClientConnect()
        {
            base.OnClientConnect();

            NetworkCallbacks.CallOnConnectToServer();
        }

        /// <summary>
        /// 当从服务器断开连接时在客户端上调用
        /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
        /// </summary>
        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();

            NetworkCallbacks.CallOnDisconnectFromServer();
        }

        /// <summary>
        /// Called on clients when a servers tells the client it is no longer ready.
        /// <para>This is commonly used when switching scenes.</para>
        /// </summary>
        public override void OnClientNotReady() { }

        /// <summary>
        /// Called on client when transport raises an exception.</summary>
        /// </summary>
        /// <param name="exception">Exception thrown from the Transport.</param>
        public override void OnClientError(TransportError error, string reason)
        {
            NetworkCallbacks.CallOnLocalClientError(error, reason);
        }

        #endregion

        #region Start & Stop Callbacks

        // Since there are multiple versions of StartServer, StartClient and StartHost, to reliably customize
        // their functionality, users would need override all the versions. Instead these callbacks are invoked
        // from all versions, so users only need to implement this one case.
        public void StartHostForAddress(ushort port)
        {
            kcp.Port = port;

            StartHost();
        }

        public void StartServerForAddress(string address, ushort port)
        {
            networkAddress = address;
            kcp.Port = port;

            StartServer();
        }

        public void StartClientForAddress(string address, ushort port)
        {
            networkAddress = address;
            kcp.Port = port;

            StartClient();
        }


        public override void OnStartHost()
        {
            base.OnStartHost();

            NetworkCallbacks.OnStartHost();
        }

        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            NetworkCallbacks.CallOnStartServer();
        }


        /// <summary>
        /// This is invoked when the client is started.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            NetworkCallbacks.CallOnStartClient();
        }

        /// <summary>
        /// This is called when a host is stopped.
        /// </summary>
        public override void OnStopHost()
        {
            base.OnStopHost();

            NetworkCallbacks.OnStopHost();
        }

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();

            discovery.StopDiscovery();
            NetworkCallbacks.CallOnStopServer();
        }

        /// <summary>
        /// This is called when a client is stopped.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();

            discovery.StopDiscovery();
            NetworkCallbacks.CallOnStopClient();
        }

        public bool isServer => Server.isServer;

        public bool isClient => Client.isClient;

        public bool isHost => isServer && isClient;

        public interface ISummonSetup
        {
            void SummonSetup(string param);
        }
        #endregion
    }
}
