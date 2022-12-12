using System;

namespace Draco.RedGreenTree.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RedTreeAttribute : Attribute
{
    public Type RootType { get; }

    public RedTreeAttribute(Type rootType)
    {
        this.RootType = rootType;
    }
}
