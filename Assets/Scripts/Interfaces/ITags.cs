using SP.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public interface ITags
    {
        List<string> tags { get; }
    }

    public class ModTag
    {
        public readonly bool hasTag;

        public ModTag(bool hasTag)
        {
            this.hasTag = hasTag;
        }
    }

    public class ValueTag<T> : ModTag
    {
        public readonly T tagValue;

        public ValueTag(bool hasTag, T tagValue) : base(hasTag)
        {
            this.tagValue = tagValue;
        }
    }

    public static class TagExtensions
    {
        public static ModTag GetTag(this ITags t, string id)
        {
            for (int i = 0; i < t.tags.Count; i++)
            {
                string tag = t.tags[i];

                if (tag == id)
                    return new(true);
            }

            return new(false);
        }

        public static bool TryGetTag(this ITags t, string id, out ModTag tag)
        {
            tag = GetTag(t, id);
            return tag.hasTag;
        }

        public static bool HasTag(this ITags t, string id)
        {
            return GetTag(t, id).hasTag;
        }



        public static ValueTag<int> GetValueTagToInt(this ITags t, string id, int defaultValue = 0)
        {
            return GetValueTag(t, id, str => str.ToInt(), defaultValue);
        }

        public static bool TryGetValueTagToInt(this ITags t, string id, out ValueTag<int> tag, int defaultValue = 0)
        {
            tag = GetValueTagToInt(t, id, defaultValue);
            return tag.hasTag;
        }



        static bool _TryGetValueTagStringValue(this ITags t, string id, out string value)
        {
            for (int i = 0; i < t.tags.Count; i++)
            {
                string tag = t.tags[i];

                if (tag.IsNullOrWhiteSpace())
                    continue;

                string[] splitted = tag.Split('=');

                if (splitted.Length != 2 || splitted[0] != id)
                    continue;

                value = splitted[1];
                return true;
            }

            value = null;
            return false;
        }

        public static ValueTag<T> GetValueTag<T>(this ITags t, string id, Func<string, T> stringToTargetType, T defaultValue)
        {
            if (_TryGetValueTagStringValue(t, id, out string stringValue))
            {
                return new(true, stringToTargetType(stringValue));
            }

            return new(false, defaultValue);
        }

        public static bool HasValueTag<T>(this ITags t, string id) => _TryGetValueTagStringValue(t, id, out _);

        public static bool TryGetValueTag<T>(this ITags t, string id, Func<string, T> stringToTargetType, out ValueTag<T> tag, T defaultValue)
        {
            tag = GetValueTag(t, id, stringToTargetType, defaultValue);
            return tag.hasTag;
        }




        public static string GetTagName(this string tag) => tag.Contains('=') ? tag.Split('=')[0] : tag;
    }
}
