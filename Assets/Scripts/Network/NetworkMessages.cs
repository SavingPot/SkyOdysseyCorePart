using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore.Converters;
using System.IO;
using SP.Tools;
using Unity.Collections.LowLevel.Unsafe;

namespace GameCore.High
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
        public float? health;

        public NMSummon(Vector3 pos, string entityId, string saveId, bool saveIntoRegion, float? health, string customData)
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

    public struct NMRegisterSyncVar : NetworkMessage
    {
        public string varId;
        public bool clientCanSet;
        public byte[] defaultValue;

        public NMRegisterSyncVar(string varId, bool clientCanSet, byte[] defaultValue)
        {
            this.varId = varId;
            this.clientCanSet = clientCanSet;
            this.defaultValue = defaultValue;
        }
    }

    public struct NMUnregisterSyncVar : NetworkMessage
    {
        public string varId;

        public NMUnregisterSyncVar(string varId)
        {
            this.varId = varId;
        }
    }

    public struct NMSyncVar : NetworkMessage
    {
        public string varId;
        public byte[] value;
        [NonSerialized] public byte[] valueLastSync;
        public readonly bool clientCanSet;

        public NMSyncVar(string varId, byte[] value, bool clientCanSet)
        {
            this.varId = varId;
            this.value = value;
            this.valueLastSync = null;
            this.clientCanSet = clientCanSet;
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

        public NMChat(byte[] portrait, string playName, string msg)
        {
            this.portrait = portrait;
            this.playerName = playName;
            this.msg = msg;
        }
    }
}
