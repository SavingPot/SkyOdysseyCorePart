using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

namespace GameCore.Network
{
    public static class Server
    {
        /// <returns>
        /// 服务器已开启
        /// </returns>
        public static bool isServer => NetworkServer.active;

        /// <returns>
        /// 玩家数量 (连接到服务器的客户端个数)
        /// </returns>
        public static int playerCount => NetworkServer.connections.Count;

        /// <returns>
        /// 是否有玩家
        /// </returns>
        public static bool HasPlayer() => playerCount > 0;

        /// <returns>
        /// 是否有其他玩家
        /// </returns>
        public static bool HasOtherPlayer()
        {
            if (isServer)
                if (Client.isClient)
                    return playerCount > 1;
                else
                    return HasPlayer();
            else
                return playerCount > 1;
        }

        public static void ClearCallbacks() => NetworkServer.ClearHandlers();


        /// <returns>
        /// 到 Host 客户端的连接
        /// </returns>
        public static LocalConnectionToClient localConnection => NetworkServer.localConnection;

        /// <summary>
        /// 发送消息给所有客户端
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="channelId"></param>
        /// <param name="sendToReadyOnly"></param>
        public static void Send<T>(T message, int channelId = Channels.Reliable, bool sendToReadyOnly = false) where T : struct, NetworkMessage
        {
            NetworkServer.SendToAll(message, channelId, sendToReadyOnly);
        }

        /// <summary>
        /// 注册服务器收到客户端消息时的回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="channelId"></param>
        /// <param name="sendToReadyOnly"></param>
        public static void Callback<T>(Action<NetworkConnectionToClient, T> handler, bool requireAuthentication = true) where T : struct, NetworkMessage
        {
            NetworkServer.RegisterHandler(handler, requireAuthentication);
        }

        /// <summary>
        /// 替换服务器收到客户端消息时的回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="channelId"></param>
        /// <param name="sendToReadyOnly"></param>
        public static void ReplaceCallback<T>(Action<NetworkConnectionToClient, T> handler, bool requireAuthentication = true) where T : struct, NetworkMessage
        {
            NetworkServer.ReplaceHandler(handler, requireAuthentication);
        }

        public static void DisconnectClients()
        {
            NetworkServer.DisconnectAll();
        }

        /// <summary>
        /// 取消注册服务器收到客户端消息时的回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="channelId"></param>
        /// <param name="sendToReadyOnly"></param>
        public static void Uncallback<T>() where T : struct, NetworkMessage
        {
            NetworkServer.UnregisterHandler<T>();
        }

        public static void DestroyObj(GameObject go)
        {
            NetworkServer.Destroy(go);
        }
    }
}
