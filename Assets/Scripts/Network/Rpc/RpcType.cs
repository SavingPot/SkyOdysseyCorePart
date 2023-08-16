namespace GameCore
{
    public enum RpcType : byte
    {
        /// <summary>
        /// 服务器运行
        /// </summary>
        ServerRpc,

        /// <summary>
        /// 让服务器命令所有客户端运行
        /// </summary>
        ClientRpc,

        /// <summary>
        /// 把命令传回发起者
        /// </summary>
        ConnectionRpc
    }
}