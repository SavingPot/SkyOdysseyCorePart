using System;

[AttributeUsage(AttributeTargets.Class)]
public class IdAttribute : Attribute
{
    public string id;

    public IdAttribute(string id)
    {
        this.id = id;
    }
}