using UnityEngine;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;
using System.Text;
using SP.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace GameCore
{
    [Serializable]
    public class Inventory
    {
        /* ------------------------ 使用 DatumItem 是因为需要储存当前耐久等数据 ------------------------ */
        [LabelText("头盔")] public Item helmet = null;
        [LabelText("身体")] public Item breastplate = null;
        [LabelText("护腿")] public Item legging = null;
        [LabelText("靴子")] public Item boots = null;
        [LabelText("栏位")] public Item[] slots;

        [NonSerialized] public IInventoryOwner owner;
        /* --------- 要注意不能直接赋值, 否则 caller.OnInventoryItemChange 不会运行 --------- */



        /* ----------------------------------- 行为包 ---------------------------------- */
        [NonSerialized] public ItemBehaviour helmetBehaviour;
        [NonSerialized] public ItemBehaviour breastplateBehaviour;
        [NonSerialized] public ItemBehaviour leggingBehaviour;
        [NonSerialized] public ItemBehaviour bootsBehaviour;
        [NonSerialized] public ItemBehaviour[] slotsBehaviours;



        /* ---------------------------------- Const --------------------------------- */
        public const string helmetVar = nameof(helmet);
        public const string breastplateVar = nameof(breastplate);
        public const string leggingVar = nameof(legging);
        public const string bootsVar = nameof(boots);


        public void DoBehaviours()
        {
            if (!Item.Null(helmet))
            {
                helmetBehaviour?.AsHelmet();
            }
            if (!Item.Null(breastplate))
            {
                breastplateBehaviour?.AsBreastplate();
            }
            if (!Item.Null(legging))
            {
                leggingBehaviour?.AsLegging();
            }
        }

        /// <summary>
        /// !!!=   当你需要 *直接* 操作物品栏里的物品, 而不是使用 Inventory 里定义好的方法时, 请务必使用这个方法   =!!!
        /// </summary>
        public void ExecuteOperation(Action<Inventory> operation, string inventoryIndex)
        {
            //检查参数
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            //执行
            operation(this);

            //这一行的作用是应用新的物品栏数据, 并刷新物品栏, 否则会导致更改无效
            owner?.OnInventoryItemChange(this, inventoryIndex);
        }

        public static Inventory ResumeFromStreamTransport(Inventory inventory, IInventoryOwner owner)
        {
            if (inventory == null)
            {
                return null;
            }

            Inventory result = new()
            {
                helmet = inventory.helmet,
                breastplate = inventory.breastplate,
                legging = inventory.legging,
                boots = inventory.boots,
                slots = inventory.slots,
                owner = owner,
                slotsBehaviours = new ItemBehaviour[inventory.slots.Length]
            };

            if (!Item.Null(result.helmet))
            {
                Item.ResumeFromStreamTransport(ref result.helmet);
                result.CreateBehaviour(result.helmet, helmetVar, out result.helmetBehaviour);
            }

            if (!Item.Null(result.breastplate))
            {
                Item.ResumeFromStreamTransport(ref result.breastplate);
                result.CreateBehaviour(result.breastplate, breastplateVar, out result.breastplateBehaviour);
            }

            if (!Item.Null(result.legging))
            {
                Item.ResumeFromStreamTransport(ref result.legging);
                result.CreateBehaviour(result.legging, leggingVar, out result.leggingBehaviour);
            }

            if (!Item.Null(result.boots))
            {
                Item.ResumeFromStreamTransport(ref result.boots);
                result.CreateBehaviour(result.boots, bootsVar, out result.bootsBehaviour);
            }

            for (int i = 0; i < result.slots.Length; i++)
            {
                if (!Item.Null(result.slots[i]))
                {
                    Item.ResumeFromStreamTransport(ref result.slots[i]);
                    result.CreateBehaviour(result.slots[i], i.ToString(), out result.slotsBehaviours[i]);
                }
            }

            return result;
        }


        public void SetSlotCount(int count)
        {
            //退出所有行为包
            if (slotsBehaviours != null)
                foreach (var slotBehaviour in slotsBehaviours)
                    slotBehaviour?.OnExit();

            //定义变量
            Item[] oldSlots = slots;
            slots = new Item[count];
            slotsBehaviours = new ItemBehaviour[count];

            //将原先的物品拷回来
            if (oldSlots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (oldSlots.Length >= i + 1 && oldSlots[i] != null)
                    {
                        slots[i] = oldSlots[i];
                        slotsBehaviours[i]?.OnExit();
                        CreateBehaviour(slots[i], i.ToString(), out slotsBehaviours[i]);
                    }
                    else
                    {
                        slots[i] = null;
                    }
                }
            }
        }

        public bool IsFull()
        {
            //如果任意一个不为空就返回 false
            foreach (var slot in slots)
                if (Item.Null(slot))
                    return false;

            return true;
        }

        public bool IsEmpty()
        {
            //如果任意一个不为空就返回 false
            foreach (var slot in slots)
                if (!Item.Null(slot))
                    return false;

            return true;
        }

        public bool HasItem(string id)
        {
            //如果任意一个不为空就返回 false
            foreach (var slot in slots)
                if (slot?.data?.id == id)
                    return true;

            return false;
        }

        public Item GetItem(string index) => index switch
        {
            helmetVar => helmet,
            breastplateVar => breastplate,
            leggingVar => legging,
            bootsVar => boots,
            _ => slots[Convert.ToInt32(index)]
        };

        public Item GetItem(int index)
        {
            return slots[index];
        }

        public void CreateBehaviour(Item item, string index, out ItemBehaviour behaviour)
        {
            if (item == null || item.data.behaviourType == null)
            {
                behaviour = null;
                return;
            }

            behaviour = (ItemBehaviour)Activator.CreateInstance(item.data.behaviourType, owner, item, index);
            behaviour.OnEnter();
        }

        public void SetItem(string index, Item value)
        {
            switch (index)
            {
                case helmetVar:
                    helmet = value;
                    helmetBehaviour?.OnExit();
                    CreateBehaviour(helmet, index, out helmetBehaviour);
                    break;

                case breastplateVar:
                    breastplate = value;
                    breastplateBehaviour?.OnExit();
                    CreateBehaviour(breastplate, index, out breastplateBehaviour);
                    break;

                case leggingVar:
                    legging = value;
                    leggingBehaviour?.OnExit();
                    CreateBehaviour(legging, index, out leggingBehaviour);
                    break;

                case bootsVar:
                    boots = value;
                    bootsBehaviour?.OnExit();
                    CreateBehaviour(boots, index, out bootsBehaviour);
                    break;

                default:
                    int i = Convert.ToInt32(index);

                    slots[i] = value;
                    slotsBehaviours[i]?.OnExit();
                    CreateBehaviour(value, index, out slotsBehaviours[i]);
                    break;
            }

            owner?.OnInventoryItemChange(this, index);
        }

        public void SetItem(int index, Item value)
        {
            slots[index] = value;
            slotsBehaviours[index]?.OnExit();
            CreateBehaviour(value, index.ToString(), out slotsBehaviours[index]);

            owner?.OnInventoryItemChange(this, index.ToString());
        }

        public void ReduceItemCount(string index, ushort count) => OperateItemCount(index, count, MathOperator.Reduce);

        public void OperateItemCount(string index, ushort count, MathOperator mo)
        {
            if (index.IsNullOrWhiteSpace())
                throw new();

            Item temp = GetItem(index);

            if (temp != null)
            {
                //修改数量
                if (mo == MathOperator.Reduce)
                    temp.count -= count;
                else
                    temp.count += count;

                //检查数量 <=0 就清掉 
                if (temp.count <= 0)
                    temp = null;
            }

            SetItem(index, temp);
        }

        public void SwapItem(string index1, string index2)
        {
            if (index1.IsNullOrWhiteSpace() || index2.IsNullOrWhiteSpace())
                throw new ArgumentException();

            //如果是一样的就不交换
            if (index1 == index2)
                return;

            //获取原本数据
            Item temp1 = GetItem(index1);
            Item temp2 = GetItem(index2);

            //交换数据
            (temp1, temp2) = (temp2, temp1);

            //设置缓存数据到实际数据
            SetItem(index1, temp1);
            SetItem(index2, temp2);
        }

        public void AddItem(Item item)
        {
            //如果有一样的, 添加数量
            //TODO: 如果有一样的, 但数量不足
            if (!GetIndexesToPutItemIntoItems(slots, item, out var indexes))
            {
                Debug.LogError("异常: 槽位满了");
                return;
            }

            //填入物品
            foreach (var index in indexes)
            {
                var i = index.Key;
                var count = index.Value;
                var slot = slots[i];

                if (Item.Null(slot))
                {
                    slot = item.data.DataToItem();
                    slot.count = count;
                    CreateBehaviour(slot, i.ToString(), out slotsBehaviours[i]);
                }
                else
                {
                    slot.count += count;
                }

                slots[i] = slot;
                owner?.OnInventoryItemChange(this, i.ToString());
            }
        }

        public Item GetItemChecked(string index)
        {
            return index switch
            {
                helmetVar => helmet,
                breastplateVar => breastplate,
                leggingVar => legging,
                bootsVar => boots,
                _ => GetItemChecked(Convert.ToInt32(index))
            };
        }

        public Item GetItemChecked(int index)
        {
            if (index > slots.Length - 1)
                return null;

            return slots[index];
        }

        public ItemBehaviour GetItemBehaviourChecked(string index)
        {
            return index switch
            {
                helmetVar => helmetBehaviour,
                breastplateVar => breastplateBehaviour,
                leggingVar => leggingBehaviour,
                bootsVar => bootsBehaviour,
                _ => GetItemBehaviourChecked(Convert.ToInt32(index))
            };
        }

        public ItemBehaviour GetItemBehaviourChecked(int index)
        {
            if (index > slotsBehaviours.Length - 1)
                return null;

            return slotsBehaviours[index];
        }

        public bool TryGetItemBehaviour(int index, out ItemBehaviour result)
        {
            if (index > slotsBehaviours.Length - 1)
            {
                result = null;
                return false;
            }

            result = slotsBehaviours[index];
            return result != null;
        }


        public bool ContainsItem(string id)
        {
            foreach (var slot in slots)
            {
                if (Item.Same(slot, id))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsItem(string id, ushort count)
        {
            ushort temp = 0;

            foreach (var slot in slots)
            {
                if (Item.Same(slot, id))
                {
                    temp += slot.count;

                    if (temp >= count)
                        return true;
                }
            }

            return temp >= count;
        }

        public bool ContainsItemTag(string tag)
        {
            foreach (var slot in slots)
            {
                if (slot.data.GetTag(tag).hasTag)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsItemTag(string tag, ushort count)
        {
            ushort temp = 0;

            foreach (var slot in slots)
            {
                if (slot.data.GetTag(tag).hasTag)
                {
                    temp += slot.count;

                    if (temp >= count)
                        return true;
                }
            }

            return temp >= count;
        }

        public bool ContainsBlock(string id)
        {
            foreach (var slot in slots)
            {
                if (slot.data.isBlock)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsBlock(string id, ushort count)
        {
            ushort temp = 0;

            foreach (var slot in slots)
            {
                if (slot.data.isBlock)
                {
                    temp += slot.count;

                    if (temp >= count)
                        return true;
                }
            }

            return temp >= count;
        }





        public Inventory()
        {

        }

        public Inventory(int slotCount, IInventoryOwner owner) : this()
        {
            SetSlotCount(slotCount);

            this.owner = owner;
        }

        public enum MathOperator
        {
            Reduce,
            Increase
        }
















        public static bool GetNeededItemIndexesFromItems(Item[] items, ItemData neededItem, uint neededCount, out Dictionary<int, ushort> result)
            => GetNeededItemIndexesFromItems(items, neededItem.id, neededItem.tags.ToArray(), neededCount, out result);

        public static bool GetNeededItemIndexesFromItems(Item[] items, string neededId, string[] neededTags, uint neededCount, out Dictionary<int, ushort> result)
        {
            Dictionary<int, ushort> resultTemp = new();
            ushort comparedCount = 0;




            for (int i = 0; i < items.Length; i++)
            {
                //已经匹配成功了就无须继续
                if (comparedCount == neededCount)
                    break;


                var current = items[i];



                void AddItemToList()
                {
                    //为什么要限制最大值为 neededCount - comparedCount? 因为我只需要这么多，再多就没用了
                    ushort count = Convert.ToUInt16(Mathf.Min(current.count, neededCount - comparedCount));

                    comparedCount += count;
                    resultTemp.Add(i, count);
                }



                //如果物品为空就跳过
                if (Item.Null(current))
                    continue;

                //如果 ID 一致则通过
                if (current.data.id == neededId)
                {
                    AddItemToList();
                    continue;
                }
                //如果 ID 不一致但标签匹配也通过
                else
                {
                    if (neededTags.Any(neededTag => current.data.tags.Any(currentTag => currentTag == neededTag)))
                    {
                        AddItemToList();
                        continue;
                    }
                }
            }



            result = resultTemp;
            return comparedCount == neededCount;
        }



        public static bool GetIndexesToPutItemIntoItems(Item[] items, Item item, out Dictionary<int, ushort> result)
            => GetIndexesToPutItemIntoItems(items, item.data.id, item.count, item.data.maxCount, out result);

        public static bool GetIndexesToPutItemIntoItems(Item[] items, string neededId, uint neededCount, ushort perSlotMaxCount, out Dictionary<int, ushort> result)
        {
            Dictionary<int, ushort> resultTemp = new();
            ushort comparedCount = 0;



            //TODO: 优先堆叠，而不是随便放
            void AddItemToList(int index, int space)
            {
                //current.data.maxCount - current.count 也就是还可以塞下多少个同样的物品
                //为什么要限制最大值为 neededCount - comparedCount? 因为我只需要这么多，再多就没用了
                ushort count = Convert.ToUInt16(Mathf.Min(space, neededCount - comparedCount));

                comparedCount += count;
                resultTemp.Add(index, count);
            }



            //第一轮先检测占用的槽位能不能堆叠
            for (int i = 0; i < items.Length; i++)
            {
                //已经匹配成功了就无须继续
                if (comparedCount == neededCount)
                    break;

                var current = items[i];



                //如果 ID 一致则通过
                if (!Item.Null(current) && current.data.id == neededId && current.count < perSlotMaxCount)
                {
                    AddItemToList(i, perSlotMaxCount - current.count);
                    continue;
                }
            }

            //如果第一轮没有找到足够的已占用槽位，就开始第二轮循环，寻找空槽位
            if (comparedCount != neededCount)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    //已经匹配成功了就无须继续
                    if (comparedCount == neededCount)
                        break;

                    var current = items[i];



                    //如果允许空槽位 & 物品为空就跳过
                    if (Item.Null(current))
                    {
                        AddItemToList(i, perSlotMaxCount);
                        continue;
                    }
                }
            }



            result = resultTemp;
            return comparedCount == neededCount;
        }
    }

    [Serializable]
    public class Item
    {
        [LabelText("数量")] public ushort count;
        [LabelText("自定义数据")] public JObject customData;
        public ItemData data;
        //TODO: behaviour include in this

        public Item()
        {

        }

        public static explicit operator CraftingRecipe_Item(Item item)
        {
            if (item == null)
                return null;

            return new(item.data.id, item.count, item.data.tags);
        }

        public static void ResumeFromStreamTransport(ref Item data)
        {
            if (!Null(data))
            {
                ItemData trueData = ModFactory.CompareItem(data.data.id);

                if (trueData == null)
                {
                    Debug.LogWarning($"存档中的物品数据不存在，已自动删除 (id={data.data.id})");
                    data = null;
                    return;
                }

                Item trueItem = trueData.DataToItem();

                trueItem.count = data.count;
                trueItem.customData = data.customData;

                data = trueItem;
            }
            else
            {
                data = null;
            }
        }

        public static bool Null(Item item)
        {
            return string.IsNullOrWhiteSpace(item?.data?.id);
        }

        public static bool Same(Item item1, Item item2)
        {
            return item1?.data?.id == item2?.data?.id && item1?.customData == item2?.customData;
        }

        public static bool Same(Item item, string id)
        {
            return item?.data?.id == id;
        }

        public static bool Same(string id1, string id2)
        {
            return id1 == id2;
        }

        public static bool IsHelmet(Item item)
        {
            return item?.data?.Helmet != null;
        }

        public static bool IsBreastplate(Item item)
        {
            return item?.data?.Breastplate != null;
        }

        public static bool IsLegging(Item item)
        {
            return item?.data?.Legging != null;
        }

        public static bool IsBoots(Item item)
        {
            return item?.data?.Boots != null;
        }
    }

    [Serializable]
    public class ItemData_Economy
    {
        public int worth;
    }

    [Serializable]
    public class ItemData_Armor
    {
        public int defense;
    }
    [Serializable]
    public class ItemData_Helmet : ItemData_Armor
    {
        public TextureData head = null;
    }
    [Serializable]
    public class ItemData_BodyArmor : ItemData_Armor
    {
        public TextureData body = null;
        public TextureData leftArm = null;
        public TextureData rightArm = null;
    }
    [Serializable]
    public class ItemData_Legging : ItemData_Armor
    {
        public TextureData leftLeg = null;
        public TextureData rightLeg = null;
    }
    [Serializable]
    public class ItemData_Boots : ItemData_Armor
    {
        public TextureData leftFoot = null;
        public TextureData rightFoot = null;
    }

    [Serializable]
    public class ItemData : ModClass, ITags
    {
        public const int defaultDamage = 5;
        public const ushort defaultMaxCount = 32;
        public static float defaultExcavationStrength = 40;
        public static float defaultUseCD = 0.2f;



        [NonSerialized] public Type behaviourType;
        [NonSerialized, LabelText("贴图数据")] public TextureData texture;

        [NonSerialized, LabelText("伤害")] public int damage = defaultDamage;
        [NonSerialized, LabelText("方块")] public bool isBlock;
        [NonSerialized, LabelText("最大数量")] public ushort maxCount = defaultMaxCount;
        [NonSerialized, LabelText("挖掘强度")] public float excavationStrength = defaultExcavationStrength;
        [NonSerialized, LabelText("使用CD")] public float useCD = defaultUseCD;
        [NonSerialized, LabelText("介绍")] public string description;
        [NonSerialized, LabelText("额外距离")] public float extraDistance;
        [NonSerialized, LabelText("大小")] public Vector2 size;
        [NonSerialized, LabelText("偏移")] public Vector2 offset;
        [NonSerialized, LabelText("偏移")] public int rotation;
        [NonSerialized, LabelText("经济")] public ItemData_Economy economy = new();



        [NonSerialized, LabelText("标签")] public List<string> tags = new();
        List<string> ITags.tags { get => tags; }



        public ValueTag<int> Edible() => this.GetValueTagToInt("ori:edible");



        [NonSerialized] public ItemData_Helmet Helmet;
        [NonSerialized] public ItemData_BodyArmor Breastplate;
        [NonSerialized] public ItemData_Legging Legging;
        [NonSerialized] public ItemData_Boots Boots;





        public override string ToString()
        {
            StringBuilder sb = Tools.stringBuilderPool.Get();

            sb.Append("id: ").AppendLine(id);
            sb.Append("texture: ").AppendLine(texture?.ToString());

            string content = sb.ToString();
            Tools.stringBuilderPool.Recover(sb);
            return content;
        }



#if UNITY_EDITOR
        [Button("输出物品信息")] public void EditorOutputTextureDatum() => Debug.Log(ToString());
#endif









        public static bool Null(ItemData item1)
        {
            return string.IsNullOrWhiteSpace(item1?.id);
        }

        public static bool Same(ItemData item1, ItemData item2)
        {
            return item1?.id == item2?.id;
        }

        public static bool Same(ItemData item, string id)
        {
            return item?.id == id;
        }

        public static bool Same(string id1, string id2)
        {
            return id1 == id2;
        }

        public static bool IsHelmet(ItemData item)
        {
            return item?.Helmet != null;
        }

        public static bool IsBreastplate(ItemData item)
        {
            return item?.Breastplate != null;
        }

        public static bool IsLegging(ItemData item)
        {
            return item?.Legging != null;
        }

        public static bool IsBoots(ItemData item)
        {
            return item?.Boots != null;
        }





        public ItemData()
        {

        }

        public ItemData(BlockData block, bool autoTexture = true)
        {
            id = block.id;
            isBlock = true;
            texture = block.defaultTexture;
            excavationStrength = defaultExcavationStrength;
            description = block.description;
            size = Vector2.one;
            offset = Vector2.zero;
            tags = block.tags;
            //texture = block.defaultTextureLoaded;
        }

        public Item DataToItem() => ModConvert.ItemDataToItem(this);
    }

    [Serializable]
    public class Recipe<T> : ModClass, IJOFormatCore where T : RecipeItem<T>
    {
        [LabelText("原料")] public T[] ingredients;
        [LabelText("结果")] public T result;


        public bool WhetherCanBeCrafted(Item[] items, out List<Dictionary<int, ushort>> ingredientTables)
        {
            ingredientTables = new();

            foreach (var ing in ingredients)
            {
                if (Inventory.GetNeededItemIndexesFromItems(items, ing.id, ing.tags.ToArray(), ing.count, out var itemsToUse))
                {
                    ingredientTables.Add(itemsToUse);
                }
                else
                {
                    ingredientTables = null;
                    return false;
                }
            }

            //如果全部原料都可以匹配就添加
            return true;
        }
    }

    [Serializable]
    public class RecipeItem<T> : ITags where T : RecipeItem<T>
    {
        [LabelText("ID")] public string id;
        [LabelText("数量")] public ushort count;
        [LabelText("标签")] public List<string> tags;
        List<string> ITags.tags { get => tags; }

        public RecipeItem(string id, ushort count, List<string> tags)
        {
            this.id = id;
            this.count = count;
            this.tags = tags;
        }
    }

    [Serializable]
    public class CraftingRecipe : Recipe<CraftingRecipe_Item>
    {

    }

    [Serializable]
    public class CraftingRecipe_Item : RecipeItem<CraftingRecipe_Item>
    {
        public CraftingRecipe_Item(string id, ushort count, List<string> tags) : base(id, count, tags)
        {

        }
    }

    [Serializable]
    public class CookingRecipe : Recipe<CookingRecipe_Item>
    {
        [LabelText("类型")] public string type;
    }

    [Serializable]
    public class CookingRecipe_Item : RecipeItem<CookingRecipe_Item>
    {
        public CookingRecipe_Item(string id, ushort count, List<string> tags) : base(id, count, tags)
        {

        }
    }
}

