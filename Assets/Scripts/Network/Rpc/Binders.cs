using System;

namespace GameCore.Network
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class RpcAttribute : Attribute
    {
        public RpcAttribute()
        {

        }
    }



    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : RpcAttribute
    {
        public ServerRpcAttribute() : base()
        {

        }
    }



    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : RpcAttribute
    {
        public ClientRpcAttribute() : base()
        {

        }
    }



    [AttributeUsage(AttributeTargets.Method)]
    public class ConnectionRpc : RpcAttribute
    {
        public ConnectionRpc() : base()
        {

        }
    }
}