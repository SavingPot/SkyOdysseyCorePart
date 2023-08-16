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

        public static ValueTag<int> GetValueTagToInt(this ITags t, string id)
        {
            return GetValueTag<int>(t, id, str => str.ToInt(), 0);
        }

        public static ValueTag<T> GetValueTag<T>(this ITags t, string id, Func<string, T> func, T defaultValue)
        {
            for (int i = 0; i < t.tags.Count; i++)
            {
                string tag = t.tags[i];

                if (tag.IsNullOrWhiteSpace())
                    continue;

                string[] splitted = tag.Split('=');

                if (splitted.Length != 2 || splitted[0] != id)
                    continue;

                return new(true, func(splitted[1]));
            }

            return new(false, defaultValue);
        }
    }
}
