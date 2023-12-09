using System;

namespace GameCore
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ByteWriterAttribute : Attribute
    {
        public readonly string targetType;

        public ByteWriterAttribute(string targetType)
        {
            this.targetType = targetType;
        }
    }



    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ByteReaderAttribute : Attribute
    {
        public readonly string targetType;

        public ByteReaderAttribute(string targetType)
        {
            this.targetType = targetType;
        }
    }
}