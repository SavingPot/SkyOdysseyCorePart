using UnityEngine;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;
using System.Text;
using SP.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameCore
{
    public interface IInventoryOwner
    {
        void OnInventoryItemChange(string index);
        Inventory GetInventory();
        void SetInventory(Inventory value);
    }

    [Serializable]//, AutoByteConverterAttribute]
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

        public void ResumeFromNetwork()
        {
            if (!Item.Null(helmet))
            {
                Item.StreamResume(ref helmet);
                CreateBehaviour(helmet, helmetVar, out helmetBehaviour);
            }

            if (!Item.Null(breastplate))
            {
                Item.StreamResume(ref breastplate);
                CreateBehaviour(breastplate, breastplateVar, out breastplateBehaviour);
            }

            if (!Item.Null(legging))
            {
                Item.StreamResume(ref legging);
                CreateBehaviour(legging, leggingVar, out leggingBehaviour);
            }

            if (!Item.Null(boots))
            {
                Item.StreamResume(ref boots);
                CreateBehaviour(boots, bootsVar, out bootsBehaviour);
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (!Item.Null(slots[i]))
                {
                    Item.StreamResume(ref slots[i]);
                    CreateBehaviour(slots[i], i.ToString(), out slotsBehaviours[i]);
                }
            }
        }


        public void SetSlotCount(byte count)
        {
            //执行行为包
            if (slotsBehaviours != null)
            {
                for (int i = 0; i < slotsBehaviours.Length; i++)
                {
                    slotsBehaviours[i]?.OnExit();
                }
            }

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

        public void CreateBehaviour(Item datum, string index, out ItemBehaviour behaviour)
        {
            if (datum == null)
            {
                behaviour = null;
                return;
            }

            if (datum.data.behaviourType == null)
            {
                behaviour = null;
                return;
            }

            behaviour = (ItemBehaviour)Activator.CreateInstance(datum.data.behaviourType, owner, datum, index);
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

            owner?.OnInventoryItemChange(index);
        }

        public void SetItem(int index, Item value)
        {
            slots[index] = value;
            slotsBehaviours[index]?.OnExit();
            CreateBehaviour(value, index.ToString(), out slotsBehaviours[index]);

            owner?.OnInventoryItemChange(index.ToString());
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

        public void AddItem(Item datumItem)
        {
            //如果有一样的, 添加数量
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];

                if (!Item.Null(slot) && Item.Same(slot, datumItem) && slot.count + datumItem.count <= slot.data.maxCount)
                {
                    slot.count += datumItem.count;
                    slotsBehaviours[i]?.OnExit();
                    CreateBehaviour(slot, i.ToString(), out slotsBehaviours[i]);
                    slots[i] = slot;
                    owner?.OnInventoryItemChange(i.ToString());
                    return;
                }
            }

            //如果有空的直接改
            for (int i = 0; i < slots.Length; i++)
            {
                if (Item.Null(slots[i]))
                {
                    slots[i] = datumItem;
                    CreateBehaviour(slots[i], i.ToString(), out slotsBehaviours[i]);
                    owner?.OnInventoryItemChange(i.ToString());
                    return;
                }
            }

            Debug.LogError("槽位满了");
        }

        public Item TryGetItem(string index)
        {
            return index switch
            {
                helmetVar => helmet,
                breastplateVar => breastplate,
                leggingVar => legging,
                bootsVar => boots,
                _ => TryGetItem(Convert.ToInt32(index))
            };
        }

        public Item TryGetItem(int index)
        {
            if (index > slots.Length - 1)
                return null;

            return slots[index];
        }

        public ItemBehaviour TryGetItemBehaviour(string index)
        {
            return index switch
            {
                helmetVar => helmetBehaviour,
                breastplateVar => breastplateBehaviour,
                leggingVar => leggingBehaviour,
                bootsVar => bootsBehaviour,
                _ => TryGetItemBehaviour(Convert.ToInt32(index))
            };
        }

        public ItemBehaviour TryGetItemBehaviour(int index)
        {
            if (index > slotsBehaviours.Length - 1)
                return null;

            return slotsBehaviours[index];
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




        public void Init(byte slotCount, IInventoryOwner owner)
        {
            SetSlotCount(slotCount);

            this.owner = owner;
        }


        public Inventory()
        {

        }

        public Inventory(byte slotCount, IInventoryOwner owner) : this()
        {
            Init(slotCount, owner);
        }

        public enum MathOperator
        {
            Reduce,
            Increase
        }
    }

    public interface IOnInventoryItemChange
    {
        void OnInventoryItemChange(string index);
    }

    [Serializable]
    public class Item
    {
        //! 添加/删除变量后记得在ByteConverters同步修改
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

        public static void StreamResume(ref Item data)
        {
            if (!Null(data))
            {
                Item trueItem = ModFactory.CompareItem(data.data.id).ToExtended();

                trueItem.count = data.count;
                trueItem.customData = data.customData;

                data = trueItem;
            }
            else
            {
                data = null;
            }
        }

        public static bool Null(Item item1)
        {
            return string.IsNullOrWhiteSpace(item1?.data?.id);
        }

        public static bool Same(Item item1, Item item2)
        {
            return item1?.data?.id == item2?.data?.id;
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
    public class ItemData_Armor
    {
        public float defense;
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
    public class ItemData : IdClassBase, ITags
    {
        public const float defaultDamage = 5;
        public const ushort defaultMaxCount = 32;
        public static float defaultExcavationStrength = 40;
        public static float defaultUseCD = 0.15f;



        [NonSerialized] public Type behaviourType;
        [NonSerialized, LabelText("贴图数据")] public TextureData texture;

        [NonSerialized, LabelText("伤害")] public float damage = defaultDamage;
        [NonSerialized, LabelText("方块")] public bool isBlock;
        [NonSerialized, LabelText("最大数量")] public ushort maxCount = defaultMaxCount;
        [NonSerialized, LabelText("挖掘强度")] public float excavationStrength = defaultExcavationStrength;
        [NonSerialized, LabelText("使用CD")] public float useCD = defaultUseCD;
        [NonSerialized, LabelText("介绍")] public string description;
        [NonSerialized, LabelText("额外距离")] public float extraDistance;



        [NonSerialized, LabelText("标签")] public List<string> tags = new();
        List<string> ITags.tags { get => tags; }



        public ValueTag<int> Edible() => this.GetValueTagToInt("ori:edible");
        public ValueTag<int> Drinkable() => this.GetValueTagToInt("ori:drinkable");



        [NonSerialized] public ItemData_Helmet Helmet;
        [NonSerialized] public ItemData_BodyArmor Breastplate;
        [NonSerialized] public ItemData_Legging Legging;
        [NonSerialized] public ItemData_Boots Boots;





        public override string ToString()
        {
            StringBuilder sb = Tools.stringBuilderPool.Get();

            sb.Append("id=");
            sb.AppendLine(id);
            sb.Append(texture?.ToString());

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
            tags = block.tags;
            //texture = block.defaultTextureLoaded;
        }

        public Item ToExtended() => ModConvert.DatumItemBaseToDatumItem(this);
    }

    public class Recipe<T> : ModClass, IJOFormatCore where T : RecipeItem<T>
    {
        [LabelText("物品")] public List<T> items = new();
        [LabelText("结果")] public T result;
    }

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

    }

    [Serializable]
    public class CookingRecipe_Item : RecipeItem<CookingRecipe_Item>
    {
        public CookingRecipe_Item(string id, ushort count, List<string> tags) : base(id, count, tags)
        {

        }
    }
}

