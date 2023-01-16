using System;
using System.Diagnostics;

namespace Draco.RedGreenTree.Attributes;

[Conditional("DRACO_SOURCEGENERATOR_ATTRIBUTE")]
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
