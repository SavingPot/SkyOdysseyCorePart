using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using Cysharp.Threading.Tasks;
using GameCore.High;

namespace GameCore.Network
{
    public static class Client
    {
        public static Dictionary<long, ServerResponse> respones = new();

        /// <summary>
        /// 延迟 (ms)
        /// </summary>
        /// <returns></returns>
        public static int ping => (int)Math.Round(NetworkTime.rtt * 1000);

        /// <returns>
        /// 客户端是否连接到或正在连接服务器
        /// </returns>
        public static bool isClient => NetworkClient.active;

        /// <returns>
        /// 客户端是否连接到服务器
        /// </returns>
        public static bool isConnected => NetworkClient.isConnected;

        /// <returns>
        /// 客户端是否正在连接连接服务器
        /// </returns>
        public static bool isConnecting => NetworkClient.isConnecting;

        /// <returns>
        /// 客户端到服务器的连接
        /// </returns>
        public static NetworkConnection connection => NetworkClient.connection;

        public static NetworkIdentity localPlayerId => NetworkClient.localPlayer;

        public static Player localPlayer => ManagerNetwork.instance.localPlayer;

        public static bool HasOtherPlayer() => Server.HasOtherPlayer();

        /// <summary>
        /// 断开与服务器的连接
        /// </summary>
        public static void Disconnect() => NetworkClient.Disconnect();

        /// <summary>
        /// 到已连接服务器后执行
        /// </summary>
        /// <param name="action"></param>
        public static async void WhenIsConnected(Action action)
        {
            while (Application.isPlaying && !isConnected)
                await UniTask.NextFrame();

            if (!Application.isPlaying)
                return;

            action();
        }

        /// <summary>
        /// 发送消息给服务器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="channelId"></param>
        /// <param name="sendToReadyOnly"></param>
        public static void Send<T>(T message, int channelId = Channels.Reliable) where T : struct, NetworkMessage
        {
            NetworkClient.Send(message, channelId);
        }

        /// <summary>
        /// 注册客户端收到服务器消息时的回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">消息</param>
        /// <param name="channelId"></param>
        /// <param name="sendToReadyOnly"></param>
        public static void Callback<T>(Action<T> handler, bool requireAuthentication = true) where T : struct, NetworkMessage
        {
            NetworkClient.RegisterHandler(handler, requireAuthentication);
        }

        /// <summary>
        /// 取消注册客户端收到服务器消息时的回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Uncallback<T>() where T : struct, NetworkMessage
        {
            NetworkClient.UnregisterHandler<T>();
        }
    }
}
