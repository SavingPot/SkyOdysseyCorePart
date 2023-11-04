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
    public interface IJsonFormat
    {
        string jsonFormat { get; set; }
    }

    public interface IJsonFormatWhenLoad
    {
        string jsonFormatWhenLoad { get; set; }
    }

    public interface IJObject
    {
        JObject jo { get; set; }
    }

    public interface IJToken
    {
        JToken jt { get; set; }
    }

    public interface IJOFormatCore : IJsonFormat, IJsonFormatWhenLoad, IJObject
    {

    }

    public interface IJOFormatCoreChild : IJsonFormat, IJsonFormatWhenLoad, IJToken
    {

    }

    [Serializable]
    public class BiomeData : ModClass, ITags
    {
        [LabelText("难度")] public int difficulty;
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
    public class BiomeData_Block : ModClassChild
    {
        public BlockLayer layer;
        public Vector3Int[] areas;
        public BiomeData_Block_Range[] ranges;
        public BiomeData_Block_Rules rules;
        public AttachedBlockDatum attached;
        public bool isPrefab;
        public bool initialized = true;
    }

    [Serializable]
    public class BiomeData_Block_Rules
    {
        public float probability;
    }

    [Serializable]
    public class BiomeData_Block_Range
    {
        public string min;
        public string max;

        public static ComputationRules GetRules(int bottom, int surface, int top)
        {
            return new()
            {
                {
                    "@bottom", bottom
                },
                {
                    "@surface", surface
                },
                {
                    "@top", top
                }
            };
        }
    }

    [Serializable]
    public class BiomeData_Structure
    {
        public StructureData structure;
    }

    [Serializable]
    public class BiomeData_FixedLayer : IdClassBase
    {
        [LabelText("层")] public int layer;
        [LabelText("必须在边界")] public bool mustOnBoundary;
        [LabelText("必须不在边界")] public bool mustNotOnBoundary;
    }

    [Serializable]
    public class BiomeData_InsertedBlock : IdClassBase
    {
        [LabelText("范围")] public List<BiomeData_InsertedBlock_Range> ranges = new();
        [LabelText("出现几率")] public float probability;
        [LabelText("依赖的方块")] public AttachedBlockDatum attachedBlock;
        [LabelText("必须在边界")] public bool mustOnBoundary;
        [LabelText("必须不在边界")] public bool mustNotOnBoundary;
    }

    [Serializable]
    public class BiomeData_InsertedBlock_Range
    {
        [LabelText("最小")] public int min;
        [LabelText("最大")] public int max;
    }

    [Serializable]
    public class AttachedBlockDatum
    {
        [LabelText("ID")] public string blockId;
        [LabelText("偏移")] public Vector2Int offset;
        [LabelText("层")] public BlockLayer layer;

        public AttachedBlockDatum(string id, Vector2Int offset, BlockLayer layer)
        {
            this.blockId = id;
            this.offset = offset;
            this.layer = layer;
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