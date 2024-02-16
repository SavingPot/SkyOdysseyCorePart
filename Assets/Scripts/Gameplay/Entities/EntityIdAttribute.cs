using System;

namespace GameCore
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityBindingAttribute : Attribute
    {
        public readonly string entityId;

        public EntityBindingAttribute(string entityId)
        {
            this.entityId = entityId;
        }
    }
}
