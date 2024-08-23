using GameCore.High;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public sealed class BlockData : ModClass, IEquatable<BlockData>, ITags, IJOFormatCore
    {
        public static float defaultHardness = 5;

        //以 1 为标准
        [LabelText("硬度")] public float hardness;
        [JsonIgnore, LabelText("默认贴图")] public TextureData defaultTexture;
        [LabelText("介绍")] public string description;
        [LabelText("可碰撞")] public bool collidible = true;
        [LabelText("掉落物")] public DropData[] drops;
        [LabelText("光亮等级")] public float lightLevel;

        #region 方块行为

        public string behaviourName;
        public Type behaviourType { get; internal set; }

        #endregion

        #region 接口

        public List<string> tags = new();
        List<string> ITags.tags { get => tags; }

        public bool IsConstructionWall() => this.HasTag("ori:construction_wall");
        public bool IsFurniture() => this.HasTag("ori:furniture");
        public bool IsValidForAreaMiningI() => id == BlockID.Stone || id == BlockID.Dirt || id == BlockID.GrassBlock || id == BlockID.Clay || id == BlockID.Sand;

        #endregion

        public bool Equals(BlockData block) => block?.id == id;

        public BlockData()
        {

        }

        public BlockData(string id)
        {
            this.id = id;
            this.hardness = defaultHardness;
            this.defaultTexture = GInit.instance.blockUnknown?.defaultTexture;
            this.collidible = GInit.instance.blockUnknown?.collidible ?? true;
        }

        public BlockData(string id, float hardness) : this(id)
        {
            this.hardness = hardness;
        }

        public BlockData(string id, float hardness, string textureId) : this(id, hardness)
        {
            this.defaultTexture = new(textureId);
        }

        public BlockData(string id, float hardness, string textureId, bool collidible) : this(id, hardness, textureId)
        {
            this.collidible = collidible;
        }

        public override string ToString()
        {
            StringBuilder sb = Tools.stringBuilderPool.Get();

            string content = ToString(sb);
            Tools.stringBuilderPool.Recover(sb);
            return content;
        }

        public string ToString(StringBuilder sb)
        {
            string indentation = "   ";

            sb.AppendLine("{");

            sb.Append(indentation).Append("id: ").AppendLine(id);
            sb.Append(indentation).Append("hardness: ").AppendLine(hardness.ToString());
            sb.Append(indentation).Append("defaultTexture: ").AppendLine(defaultTexture?.ToString());
            sb.Append(indentation).Append("collidible: ").AppendLine(collidible.ToString());

            sb.Append(indentation).AppendLine("tags");
            sb.Append(indentation).AppendLine("{");
            indentation += "   ";
            for (int i = 0; i < tags.Count; i++)
            {
                sb.Append(indentation).Append("E").Append(i).AppendLine(tags[i]);
            }
            indentation = "   ";
            sb.Append(indentation).AppendLine("}");

            sb.AppendLine("}");

            return sb.ToString();
        }

#if UNITY_EDITOR
        [Button("输出方块信息")] public void EditorOutputDatum() => Debug.Log(ToString());

        [Button]
        private void OutputBehaviourInfo()
        {
            Debug.Log($"{behaviourName}, {behaviourType == null}");
        }
#endif
    }
}
