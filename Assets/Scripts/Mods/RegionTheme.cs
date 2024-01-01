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
    public class RegionTheme : ModClass
    {
        [LabelText("分布")] public int distribution;
        [LabelText("群系")] public string[] biomes;
    }
}