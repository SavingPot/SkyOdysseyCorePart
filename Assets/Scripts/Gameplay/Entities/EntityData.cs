using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public sealed class EntityData : ModClass
    {
        [LabelText("生成")] public EntityData_Summon summon = new();
        [LabelText("速度")] public float speed;
        [LabelText("重力")] public float gravity;
        [LabelText("碰撞箱大小")] public Vector2 colliderSize;
        [LabelText("碰撞箱偏移")] public Vector2 colliderOffset;
        [LabelText("最大血量")] public int maxHealth;
        [LabelText("自动清除周期")] public float lifetime = defaultLifetime;
        [LabelText("搜索半径")] public ushort searchRadius;
        [LabelText("搜索半径平方")] public int searchRadiusSqr;
        [LabelText("普通攻击半径")] public NormalAttackData normalAttack;
        [LabelText("受伤音效")] public string hurtAudioId = AudioID.GetHurt;
        public static float defaultLifetime = 60 * 3;
        public Type behaviourType;
        [LabelText("掉落的物品")] public DropData[] drops;
        [LabelText("掉落的金币数")] public int coinCount;


        public bool IsPlayer => id == EntityID.Player;



        [Serializable]
        public sealed class NormalAttackData
        {
            [LabelText("半径")] public float radius;
            [LabelText("伤害")] public int damage;
            [LabelText("警告时间")] public float warningTime;
            [LabelText("躲避时间")] public float dodgeTime;
            [LabelText("命中判断时间")] public float hitJudgementTime;
            [LabelText("后摇时间")] public float recoveryTime;
        }
    }

    [Serializable]
    public sealed class EntityData_Summon
    {
        [LabelText("区域")] public string biome;
        [LabelText("默认几率")] public float defaultProbability;
        [LabelText("最早时间")] public float timeEarliest;
        [LabelText("最晚时间")] public float timeLatest;
    }
}