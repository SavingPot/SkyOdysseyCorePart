using System;
using Sirenix.OdinInspector;

namespace GameCore
{
    [Serializable]
    public class StructureData : ModClass
    {
        [LabelText("必须足够")] public bool mustEnough = true;
        [LabelText("概率")] public float probability;
        [LabelText("需求")] public AttachedBlockDatum[] require;
        [LabelText("固定方块")] public AttachedBlockDatum[] fixedBlocks;
    }
}