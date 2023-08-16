using System;
using Mirror;

namespace GameCore
{
    public struct NMRpc : NetworkMessage
    {
        public readonly string methodPath;
        public readonly RpcType callType;
        public ByteWriter parameters;
        public uint instance;

        public NMRpc(string methodPath, RpcType type)
        {
            this.methodPath = methodPath;
            this.callType = type;
            this.parameters = default;
            this.instance = uint.MaxValue;
        }
    }
}