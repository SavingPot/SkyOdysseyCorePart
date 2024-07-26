using Sirenix.OdinInspector;
using SP.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace GameCore
{
    [Serializable]
    public sealed class Mod
    {
        [LabelText("文件夹名称")] public string folderName;
        [LabelText("路径")] public string path;
        [LabelText("模组信息")] public Mod_Info info;
        //[LabelText("设置")] public Dictionary<string, string> config = new();

        [LabelText("物品")] public List<ItemData> items = new();
        [LabelText("魔咒")] public List<Spell> spells = new();
        [LabelText("合成表")] public List<CraftingRecipe> craftingRecipes = new();
        [LabelText("菜谱")] public List<CookingRecipe> cookingRecipes = new();
        [LabelText("贴图")] public List<TextureData> textures = new();
        [LabelText("方块")] public List<BlockData> blocks = new();
        [LabelText("群系方块预置")] public List<BiomeBlockPrefab> biomeBlockPrefabs = new();
        [LabelText("群系")] public List<BiomeData> biomes = new();
        [LabelText("结构")] public List<StructureData> structures = new();
        [LabelText("文本")] public List<GameLang> langs = new();
        [LabelText("音频")] public List<AudioData> audios = new();
        [LabelText("实体")] public List<EntityData> entities = new();
        [LabelText("导入类型")] public Type[] importedTypes;

        [NonSerialized] public ModEntry entryInstance;

        public bool isOri => info.isOri;


        public void FixWrongPart()
        {
            importedTypes ??= new Type[0];
        }


        public ItemData GetRandomItem(Func<ItemData, bool> condition) => GetRandomItem(Tools.staticRandom, condition);
        public ItemData GetRandomItem(Random random, Func<ItemData, bool> condition)
        {
            //抽取一个
            ItemData item = null;
            while (item == null)
            {
                item = items.Extract(random);

                //如果不符合就跳到下一个
                if (!condition(item))
                    item = null;
            }
            return item;
        }

        public Spell GetRandomSpell(Func<Spell, bool> condition) => GetRandomSpell(Tools.staticRandom, condition);
        public Spell GetRandomSpell(Random random, Func<Spell, bool> condition)
        {
            //抽取一个
            Spell spell = null;
            while (spell == null)
            {
                spell = spells.Extract(random);

                //如果不符合就跳到下一个
                if (!condition(spell))
                    spell = null;
            }
            return spell;
        }


        public override string ToString()
        {
            string texturePath = ModFactory.GetTexturesPath(path);

            StringBuilder sb = Tools.stringBuilderPool.Get();

            sb.Append(info.id).Append(" (").Append(path).AppendLine(")");
            sb.AppendLine("{");

            sb.AppendLine("   贴图:");
            lock (textures)
                textures.For(p => sb.AppendLine($"      {p.id} ({texturePath + p.texturePath}) "));
            sb.AppendLine("\n");

            sb.AppendLine("   方块:");
            lock (blocks)
                blocks.For(p => sb.AppendLine($"      {p.id}"));
            sb.AppendLine("\n");

            sb.AppendLine("   群系:");
            lock (biomes)
                biomes.For(p => sb.AppendLine($"      {p.id}"));
            sb.AppendLine("\n");

            sb.AppendLine("   文本:");
            lock (langs)
                langs.For(p => { sb.AppendLine($"      {p.id}"); p.texts.For(p => sb.AppendLine($"         {p.id} -> {p.text}")); sb.AppendLine("\n"); });
            sb.Append("\n");

            sb.AppendLine("   音频:");
            lock (audios)
                audios.For(p => sb.AppendLine($"      {p.id} ({p.path})"));
            sb.Append("\n");

            sb.AppendLine("   物品:");
            lock (items)
                items.For(p => sb.AppendLine($"      {p.id}"));
            sb.Append("\n");

            sb.AppendLine("   实体:");
            lock (entities)
                entities.For(p => sb.AppendLine($"      {p.id} [Behaviour: {p.behaviourType?.Name}]"));
            sb.Append("\n");

            sb.AppendLine("\n}\n");

            string content = sb.ToString();
            Tools.stringBuilderPool.Recover(sb);
            return content;
        }
    }
}