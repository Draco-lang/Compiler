using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.RedGreenTree.Attributes;

[Flags]
public enum IgnoreFlags
{
    TransformerTransform = 1 << 0,
    TransformerAll = 1 << 1,
    VisitorVisit = 1 << 2,
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreAttribute : Attribute
{
    public IgnoreFlags Flags { get; }

    public IgnoreAttribute(IgnoreFlags flags)
    {
        this.Flags = flags;
    }
}
