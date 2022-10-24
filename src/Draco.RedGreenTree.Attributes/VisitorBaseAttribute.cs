using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.RedGreenTree.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class VisitorBaseAttribute : Attribute
{
    public Type RootType { get; }

    public VisitorBaseAttribute(Type rootType)
    {
        this.RootType = rootType;
    }
}
