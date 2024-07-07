using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GameCore.Converters;
using Mirror;
using Mirror.Discovery;
using SP.Tools;
using UnityEngine;

namespace GameCore.Network
{
    public struct ServerRequest : NetworkMessage { }
    public struct ServerResponse : NetworkMessage
    {
        // 发送回应的服务器
        // 这是一个属性, 因此它不会被序列化, 客户端在收到后会将其填充
        public IPEndPoint EndPoint { get; set; }

        public Uri uri;

        // 防止出现重复的服务器显示
        public long serverId;

        public string version;
        public string worldName;
    }

    public class ServerDiscovery : NetworkDiscoveryBase<ServerRequest, ServerResponse>
    {
        #region Server

        /// <summary>
        /// 给客户端的回应，以让客户端显示服务器信息
        /// </summary>
        /// <param name="request">客户端的请求</param>
        /// <param name="endpoint">发送请求的客户端 IP</param>
        /// <returns>发送给客户端的回应消息</returns>
        protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
        {
            try
            {
                var response = new ServerResponse
                {
                    serverId = ServerId,
                    uri = transport.ServerUri(),
                    version = GInit.gameVersion
                };

                if (GFiles.world != null)
                {
                    response.worldName = GFiles.world.basicData.worldName;

                    //TODO: Send world icon
                }

                return response;
            }
            catch (NotImplementedException)
            {
                Debug.LogError($"传送 {transport} 不支持网络发现");
                throw;
            }
        }
        #endregion



        #region Client

        /// <summary>
        /// 创建将在网络上广播以发现服务器的消息
        /// </summary>
        /// <returns>想要广播的消息实例</returns>
        protected override ServerRequest GetRequest() => new();

        /// <summary>
        /// 处理服务器的回复
        /// </summary>
        /// <remarks>
        /// 客户端受到服务器的回复, 而这个方法会处理回复，并触发一个事件
        /// </remarks>
        /// <param name="response">Response that came from the server</param>
        /// <param name="endpoint">Address of the server that replied</param>
        protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
        {
            //接收到了消息
            response.EndPoint = endpoint;

            // 虽然我们得到了一个可能有效的url, 但我们可能无法解析提供的主机
            // 单我们知道服务器的真实ip地址, 因为我们刚刚从服务器接收到一个数据包, 所以将其用作主机
            UriBuilder realUriBuilder = new(response.uri)
            {
                Host = response.EndPoint.Address.ToString()
            };
            response.uri = realUriBuilder.Uri;

            OnServerFound?.Invoke(response);
        }
        #endregion
    }
}