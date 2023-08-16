using System;

namespace GameCore
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ItemBindingAttribute : IdAttribute
    {
        public ItemBindingAttribute(string id) : base(id)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SpellBindingAttribute : IdAttribute
    {
        public SpellBindingAttribute(string id) : base(id)
        {

        }
    }
}