using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public sealed class EntityData : ModClass
    {
        [LabelText("路径")] public string path;
        [LabelText("生成")] public EntityData_Summon summon = new();
        [LabelText("速度")] public float speed;
        [LabelText("重力")] public float gravity;
        [LabelText("碰撞箱大小")] public Vector2 colliderSize;
        [LabelText("碰撞箱偏移")] public Vector2 colliderOffset;
        [LabelText("最大血量")] public int maxHealth;
        [LabelText("自动清除周期")] public float lifetime = defaultLifetime;
        [LabelText("搜索半径")] public ushort searchRadius;
        [LabelText("搜索半径平方")] public int searchRadiusSqr;
        [LabelText("普通攻击半径")] public float normalAttackRadius;
        [LabelText("普通攻击伤害")] public int normalAttackDamage;
        [LabelText("普通攻击CD")] public float normalAttackCD;
        public static float defaultLifetime = 60 * 3;
        public Type behaviourType;
        [LabelText("掉落的物品")] public List<DropData> drops;
        [LabelText("掉落的金币数")] public int coinCount;
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