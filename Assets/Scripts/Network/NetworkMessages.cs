using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore.Converters;
using System.IO;
using SP.Tools;

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

    public struct NMSummon : NetworkMessage
    {
        public Vector3 pos;
        public string entityId;
        public string saveId;
        public string customData;
        public bool autoDatum;
        public float? health;

        public NMSummon(Vector3 pos, string entityId, string saveId, bool autoDatum, float? health, string customData)
        {
            this.pos = pos;
            this.entityId = entityId;
            this.saveId = saveId;
            this.customData = customData;
            this.autoDatum = autoDatum;
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
        public byte[] valueLastSync;
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
        public Vector2Int sandbox;
        public Vector2Int pos;
        public BlockLayer layer;

        public NMDestroyBlock(Vector2Int sandbox, Vector2Int pos, BlockLayer layer)
        {
            this.sandbox = sandbox;
            this.pos = pos;
            this.layer = layer;
        }
    }

    public struct NMSetBlock : NetworkMessage
    {
        public Vector2Int pos;
        public BlockLayer layer;
        public string block;
        public string customData;

        public NMSetBlock(Vector2Int pos, BlockLayer layer, string block, string customData)
        {
            this.pos = pos;
            this.layer = layer;
            this.block = block;
            this.customData = customData;
        }
    }

    public struct NMSetBlockCustomData : NetworkMessage
    {
        public Vector2Int pos;
        public BlockLayer layer;
        public string customData;

        public NMSetBlockCustomData(Vector2Int pos, BlockLayer layer, string customData)
        {
            this.pos = pos;
            this.layer = layer;
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
