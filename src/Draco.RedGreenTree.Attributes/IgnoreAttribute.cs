using System;

namespace Draco.RedGreenTree.Attributes;

[Flags]
public enum IgnoreFlags
{
    TransformerTransform = 1 << 0,
    TransformerAll = 1 << 1,
    VisitorVisit = 1 << 2,
    SyntaxFactoryConstruct = 1 << 3,
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class IgnoreAttribute : Attribute
{
    public IgnoreFlags Flags { get; }

    public IgnoreAttribute(IgnoreFlags flags)
    {
        this.Flags = flags;
    }
}
