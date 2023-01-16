using System;

namespace Draco.RedGreenTree.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RedTreeAttribute : Attribute
{
    public Type GreenTreeType { get; }
    public Type RedTreeType { get; }
    public Type RootType { get; }

    public RedTreeAttribute(Type greenTreeType, Type redTreeType, Type rootType)
    {
        this.GreenTreeType = greenTreeType;
        this.RedTreeType = redTreeType;
        this.RootType = rootType;
    }
}
