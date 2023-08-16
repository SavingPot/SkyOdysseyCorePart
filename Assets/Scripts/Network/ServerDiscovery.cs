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

namespace GameCore
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
        /// 处理客户端的请求
        /// </summary>
        /// <remarks>
        /// Override if you wish to provide more information to the clients
        /// such as the name of the host player
        /// </remarks>
        /// <param name="request">Request coming from client</param>
        /// <param name="endpoint">Address of the client that sent the request</param>
        /// <returns>The message to be sent back to the client or null</returns>
        protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
        {
            // In this case we don't do anything with the request
            // but other discovery implementations might want to use the data
            // in there,  This way the client can ask for
            // specific game mode or something

            try
            {
                // this is an example reply message,  return your own
                // to include whatever is relevant for your game
                var response = new ServerResponse
                {
                    serverId = ServerId,
                    uri = transport.ServerUri(),
                    version = GInit.gameVersion
                };

                if (GFiles.world != null)
                {
                    response.worldName = GFiles.world.basicData.worldName;

                    // if (File.Exists(GFiles.world.worldImagePath))
                    //     response.worldIcon = IOTools.LoadBytes(GFiles.world.worldImagePath);
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
        /// <remarks>
        /// Override if you wish to include additional data in the discovery message
        /// such as desired game mode, language, difficulty, etc... </remarks>
        /// <returns>An instance of ServerRequest with data to be broadcasted</returns>
        protected override ServerRequest GetRequest() => new ServerRequest();

        /// <summary>
        /// 处理服务器的回复
        /// </summary>
        /// <remarks>
        /// A client receives a reply from a server, this method processes the
        /// reply and raises an event
        /// </remarks>
        /// <param name="response">Response that came from the server</param>
        /// <param name="endpoint">Address of the server that replied</param>
        protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
        {
            //接收到了消息
            response.EndPoint = endpoint;

            // 虽然我们得到了一个可能有效的url, 但我们可能无法解析提供的主机
            // 单我们知道服务器的真实ip地址, 因为我们刚刚从服务器接收到一个数据包, 所以将其用作主机
            UriBuilder realUriBuilder = new UriBuilder(response.uri)
            {
                Host = response.EndPoint.Address.ToString()
            };
            response.uri = realUriBuilder.Uri;

            OnServerFound?.Invoke(response);
        }
        #endregion
    }
}