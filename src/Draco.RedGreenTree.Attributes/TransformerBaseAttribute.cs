using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.RedGreenTree.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TransformerBaseAttribute : Attribute
{
    public Type GreenRootType { get; }
    public Type RedRootType { get; }

    public TransformerBaseAttribute(Type greenRootType, Type redRootType)
    {
        this.GreenRootType = greenRootType;
        this.RedRootType = redRootType;
    }
}
