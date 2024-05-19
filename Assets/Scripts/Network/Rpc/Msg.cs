using System;
using Mirror;

namespace GameCore.Network
{
    public struct NMRpc : NetworkMessage
    {
        public readonly string methodPath;
        public readonly RpcType callType;
        public byte[] parameter0;
        public byte[] parameter1;
        public byte[] parameter2;
        public byte[] parameter3;
        public byte[] parameter4;
        public uint instance;

        public override readonly string ToString()
        {
            return $"MethodPath: {methodPath}\nCallType: {callType}\nParameter0: {parameter0?.Length}\nParameter1: {parameter1?.Length}\nParameter2: {parameter2?.Length}\nParameter3: {parameter3?.Length}\nParameter4: {parameter4?.Length}\nInstance: {instance}";
        }

        public NMRpc(string methodPath, RpcType type)
        {
            this.methodPath = methodPath;
            this.callType = type;
            this.parameter0 = null;
            this.parameter1 = null;
            this.parameter2 = null;
            this.parameter3 = null;
            this.parameter4 = null;
            this.instance = uint.MaxValue;
        }
    }
}