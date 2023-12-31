using System;

namespace GameCore
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SyncGetterAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SyncSetterAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SyncAttribute : Attribute
    {
        public readonly string hook;

        public SyncAttribute()
        {
            hook = null;
        }

        public SyncAttribute(string hook)
        {
            this.hook = hook;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SyncDefaultValueAttribute : Attribute
    {
        public readonly object defaultValue;

        public SyncDefaultValueAttribute(object defaultValue)
        {
            this.defaultValue = defaultValue;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SyncDefaultValueFromMethodAttribute : Attribute
    {
        public readonly string methodName;
        public readonly bool getValueUntilRegister;

        public SyncDefaultValueFromMethodAttribute(string methodName, bool getValueUntilRegister)
        {
            this.methodName = methodName;
            this.getValueUntilRegister = getValueUntilRegister;
        }
    }
}