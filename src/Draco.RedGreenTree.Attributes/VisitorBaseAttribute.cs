using System;

namespace Draco.RedGreenTree.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class VisitorBaseAttribute : Attribute
{
    public Type GreenRootType { get; }
    public Type RedRootType { get; }

    public VisitorBaseAttribute(Type greenRootType, Type redRootType)
    {
        this.GreenRootType = greenRootType;
        this.RedRootType = redRootType;
    }
}
