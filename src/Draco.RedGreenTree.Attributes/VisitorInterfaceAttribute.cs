using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.RedGreenTree.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class VisitorInterfaceAttribute : Attribute
{
    public Type RootType { get; }

    public VisitorInterfaceAttribute(Type rootType)
    {
        this.RootType = rootType;
    }
}
