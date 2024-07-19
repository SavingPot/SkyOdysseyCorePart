using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore.Converters;
using System.IO;
using SP.Tools;
using Unity.Collections.LowLevel.Unsafe;

namespace GameCore.Network
{
    public struct NMPos : NetworkMessage
    {
        public Vector3 vec;
        public string prefix;

        public NMPos(Vector3 vec, string prefix = null)
        {
            this.vec = vec;
            this.prefix = prefix;
        }

        public static void Check(NMPos n)
        {
            //检查 NMPos 中的值是否为空
            if (n.vec == null)
                throw new NullReferenceException($"{nameof(NMPos)}.{nameof(NMPos.vec)} 不能为 null");

            if (n.prefix.IsNullOrWhiteSpace())
                throw new NullReferenceException($"{nameof(NMPos)}.{nameof(NMPos.prefix)} 不能为空");
        }
    }

    public struct NMAddPlayer : NetworkMessage
    {
        public string playerName;
        public string gameVersion;
        public List<string> modIds;
        public List<string> modVersions;
        public byte[] skinHead;
        public byte[] skinBody;
        public byte[] skinLeftArm;
        public byte[] skinRightArm;
        public byte[] skinLeftLeg;
        public byte[] skinRightLeg;
        public byte[] skinLeftFoot;
        public byte[] skinRightFoot;

        public NMAddPlayer(string playerName, string gameVersion, List<string> modIds, List<string> modVersions)
        {
            this.playerName = playerName;
            this.gameVersion = gameVersion;
            this.modIds = modIds;
            this.modVersions = modVersions;

            PlayerSkin.SetSkinByName(GFiles.settings.playerSkinName);
            this.skinHead = Rpc.ObjectToBytes(PlayerSkin.skinHead);
            this.skinBody = Rpc.ObjectToBytes(PlayerSkin.skinBody);
            this.skinLeftArm = Rpc.ObjectToBytes(PlayerSkin.skinLeftArm);
            this.skinRightArm = Rpc.ObjectToBytes(PlayerSkin.skinRightArm);
            this.skinLeftLeg = Rpc.ObjectToBytes(PlayerSkin.skinLeftLeg);
            this.skinRightLeg = Rpc.ObjectToBytes(PlayerSkin.skinRightLeg);
            this.skinLeftFoot = Rpc.ObjectToBytes(PlayerSkin.skinLeftFoot);
            this.skinRightFoot = Rpc.ObjectToBytes(PlayerSkin.skinRightFoot);
        }
    }

    public struct NMSummon : NetworkMessage
    {
        public Vector3 pos;
        public string entityId;
        public string saveId;
        public string customData;
        public bool saveIntoRegion;
        public int? health;

        public NMSummon(Vector3 pos, string entityId, string saveId, bool saveIntoRegion, int? health, string customData)
        {
            this.pos = pos;
            this.entityId = entityId;
            this.saveId = saveId;
            this.customData = customData;
            this.saveIntoRegion = saveIntoRegion;
            this.health = health;
        }
    }

    public struct NMClientChangeScene : NetworkMessage
    {
        public string newSceneName;

        public NMClientChangeScene(string newSceneName)
        {
            this.newSceneName = newSceneName;
        }
    }

    public struct NMDestroy : NetworkMessage
    {
        public uint netId;

        public NMDestroy(uint netId)
        {
            this.netId = netId;
        }
    }


    public struct NMEntityTakeDamage : NetworkMessage
    {
        public uint netId;
        public float hurtHealth;
        public float invincibleTime;
        public Vector2 hurtPos;
        public Vector2 impactForce;

        public NMEntityTakeDamage(uint netId, float hurtHealth, float invincibleTime, Vector2 hurtPos, Vector2 impactForce)
        {
            this.netId = netId;
            this.hurtHealth = hurtHealth;
            this.invincibleTime = invincibleTime;
            this.hurtPos = hurtPos;
            this.impactForce = impactForce;
        }
    }

    public struct NMRequestInstanceVars : NetworkMessage
    {

    }

    public struct NMRegisterSyncVar : NetworkMessage
    {
        public string varId;
        public uint instance;
        public byte[] defaultValue;
        public float serverSendTime;

        public NMRegisterSyncVar(string varId, uint instance, byte[] defaultValue, float serverSendTime)
        {
            this.varId = varId;
            this.instance = instance;
            this.defaultValue = defaultValue;
            this.serverSendTime = serverSendTime;
        }
    }

    public struct NMUnregisterSyncVar : NetworkMessage
    {
        public string varId;
        public uint instance;

        public NMUnregisterSyncVar(string varId, uint instance)
        {
            this.varId = varId;
            this.instance = instance;
        }
    }

    public struct NMSyncVar : NetworkMessage
    {
        public string varId;
        public uint instance;
        public byte[] value;
        [NonSerialized] public object valueLastSync;

        /// <summary>
        /// 这个字段用来记录上一次从服务器同步该变量时服务器的时间。
        /// Kcp 协议中数据包不保证顺序，因此需要知道哪一个才是最新的数据。
        /// serverSendTime 记录了服务器发送该变量的时间，值越大说明数据越新。
        /// </summary>
        public float serverSendTime;




        public override string ToString()
        {
            return $"NMSyncVar: {varId} of {instance}, value is {Rpc.BytesToObject(value)}, sent at {serverSendTime}";
        }




        public NMSyncVar(string varId, uint instance, byte[] value, float serverSendTime)
        {
            this.varId = varId;
            this.instance = instance;
            this.value = value;
            this.valueLastSync = null;
            this.serverSendTime = serverSendTime;
        }

        public NMSyncVar(string varId, uint instance, byte[] value, object valueLastSync, float serverSendTime)
        {
            this.varId = varId;
            this.instance = instance;
            this.value = value;
            this.valueLastSync = valueLastSync;
            this.serverSendTime = serverSendTime;
        }
    }

    public struct NMDestroyBlock : NetworkMessage
    {
        public Vector2Int region;
        public Vector2Int pos;
        public bool isBackground;

        public NMDestroyBlock(Vector2Int region, Vector2Int pos, bool isBackground)
        {
            this.region = region;
            this.pos = pos;
            this.isBackground = isBackground;
        }
    }

    public struct NMRemoveBlock : NetworkMessage
    {
        public Vector2Int pos;
        public bool isBackground;

        public NMRemoveBlock(Vector2Int pos, bool isBackground)
        {
            this.pos = pos;
            this.isBackground = isBackground;
        }
    }

    public struct NMSetBlock : NetworkMessage
    {
        public Vector2Int pos;
        public bool isBackground;
        public string block;
        public string customData;

        public NMSetBlock(Vector2Int pos, bool isBackground, string block, string customData)
        {
            this.pos = pos;
            this.isBackground = isBackground;
            this.block = block;
            this.customData = customData;
        }
    }

    public struct NMSetBlockCustomData : NetworkMessage
    {
        public Vector2Int pos;
        public bool isBackground;
        public string customData;

        public NMSetBlockCustomData(Vector2Int pos, bool isBackground, string customData)
        {
            this.pos = pos;
            this.isBackground = isBackground;
            this.customData = customData;
        }
    }

    public struct NMChat : NetworkMessage
    {
        public byte[] portrait;
        public string playerName;
        public string msg;

        public NMChat(byte[] portrait, string playerName, string msg)
        {
            this.portrait = portrait;
            this.playerName = playerName;
            this.msg = msg;
        }
    }
}
