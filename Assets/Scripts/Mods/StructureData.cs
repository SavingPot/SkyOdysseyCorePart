using System;
using Newtonsoft.Json.Linq;
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



        public JObject ToJObject()
        {
            //获取 require
            var requireJArray = new JArray();
            if (require != null)
            {
                foreach (var block in require)
                {
                    requireJArray.Add(block.ToJObject());
                }
            }

            //获取 fixedBlocks
            var fixedBlockJArray = new JArray();
            foreach (var block in fixedBlocks)
            {
                fixedBlockJArray.Add(block.ToJObject());
            }

            //返回结果
            var result = new JObject
            {
                { "json_format", jsonFormat },
                {
                    "ori:structure", new JObject
                    {
                        { "id", id },
                        {
                            "generation", new JObject
                            {
                                { "probability", probability },
                                { "must_enough", mustEnough },
                                { "require", requireJArray }
                            }
                        },
                        {
                            "blocks", new JObject
                            {
                                { "fixed", fixedBlockJArray }
                            }
                        }
                    }
                }
            };

            return result;
        }
    }
}