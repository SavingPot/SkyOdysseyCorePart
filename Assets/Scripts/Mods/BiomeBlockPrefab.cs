using System;
using System.Collections.Generic;

namespace GameCore
{
    [Serializable]
    public class BiomeBlockPrefab : ModClass
    {
        public List<BiomeData_Block> content = new();
    }
}