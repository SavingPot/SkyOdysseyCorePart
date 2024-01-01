using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace GameCore
{
    [Serializable]
    public class Spell : ModClass, ITags
    {
        [LabelText("花费")] public int cost;
        [LabelText("介绍")] public string description;
        [LabelText("组合")] public Spell[] combination;
        public Type behaviourType;



        [NonSerialized, LabelText("标签")] public List<string> tags = new();
        List<string> ITags.tags { get => tags; }
    }
}