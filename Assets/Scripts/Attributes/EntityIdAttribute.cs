using System;

[AttributeUsage(AttributeTargets.Class)]
public class EntityBindingAttribute : IdAttribute
{
    public EntityBindingAttribute(string id) : base(id)
    {

    }
}