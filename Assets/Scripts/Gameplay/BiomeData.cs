using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;
using GameCore;
using SP.Tools;

namespace GameCore
{
    [Serializable]
    public sealed class BiomeData : ModClass, ITags
    {
        [LabelText("最小尺寸")] public Vector2Int minSize;
        [LabelText("最大尺寸")] public Vector2Int maxSize;
        [LabelText("随机实体"), NonSerialized] public List<RandomEntityData> randomEntityData = new();
        [LabelText("结构"), NonSerialized] public BiomeData_Structure[] structures;
        [LabelText("方块")] public BiomeData_Block[] blocks;

        [JsonIgnore, LabelText("标签")] public List<string> tags = new();
        List<string> ITags.tags { get => tags; }

        public ModTag Rich() => this.GetTag("ori:rich");
    }

    [Serializable]
    public sealed class BiomeData_Block_Perlin
    {
        public float fluctuationFrequency; //起伏频率, 值越大起伏越多
        public float fluctuationHeight; //起伏高度, 值越大起伏越高
        public string startYFormula;
        public BiomeData_Block_Perlin_Block[] blocks;
    }

    [Serializable]
    public sealed class BiomeData_Block_Perlin_Block
    {
        public string minFormula;
        public string maxFormula;
        public string block;
        public bool isBackground;
    }

    [Serializable]
    public sealed class BiomeData_Block : ModClassChild
    {
        [LabelText("类型")] public string type;
        public Vector3Int[] areas;
        public BiomeData_Block_Range[] ranges;
        public BiomeData_Block_Rules rules;
        public AttachedBlockDatum attached;
        public BiomeData_Block_Perlin perlin;
        public bool isPrefab;
        public bool initialized = true;
    }

    [Serializable]
    public sealed class BiomeData_Block_Rules
    {
        public float probability;
    }

    [Serializable]
    public sealed class BiomeData_Block_Range
    {
        public string minFormula;
        public string maxFormula;
    }

    [Serializable]
    public sealed class BiomeData_Structure
    {
        public StructureData structure;
    }

    [Serializable]
    public sealed class BiomeData_FixedLayer : IdClassBase
    {
        [LabelText("层")] public int layer;
        [LabelText("必须在边界")] public bool mustOnBoundary;
        [LabelText("必须不在边界")] public bool mustNotOnBoundary;
    }

    [Serializable]
    public sealed class BiomeData_InsertedBlock : IdClassBase
    {
        [LabelText("范围")] public List<BiomeData_InsertedBlock_Range> ranges = new();
        [LabelText("出现几率")] public float probability;
        [LabelText("依赖的方块")] public AttachedBlockDatum attachedBlock;
        [LabelText("必须在边界")] public bool mustOnBoundary;
        [LabelText("必须不在边界")] public bool mustNotOnBoundary;
    }

    [Serializable]
    public sealed class BiomeData_InsertedBlock_Range
    {
        [LabelText("最小")] public int min;
        [LabelText("最大")] public int max;
    }

    [Serializable]
    public sealed class AttachedBlockDatum
    {
        [LabelText("ID")] public string blockId;
        [LabelText("偏移")] public Vector2Int offset;
        [LabelText("背景")] public bool isBackground;

        public AttachedBlockDatum(string id, Vector2Int offset, bool isBackground)
        {
            this.blockId = id;
            this.offset = offset;
            this.isBackground = isBackground;
        }
    }

    [Serializable]
    public struct RandomEntityData
    {
        public bool onlyWhenGenerate;
        [LabelText("生成的实体")] public string summonEntityId;
        [LabelText("出现几率")] public float probability;
    }

    [Serializable]
    public struct BiomeInsertedBlock
    {
        [LabelText("瓦片")] public BlockData block;
        [LabelText("依赖的地面")] public string attachedBlockId;
        [LabelText("出现几率")] public float probability;
    }
}