using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.RedGreenTree.Attributes;

[Flags]
public enum IgnoreFlags
{
    Transformer = 1 << 0,
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
