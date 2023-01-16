using System;
using System.Diagnostics;

namespace Draco.RedGreenTree.Attributes;

[Conditional("DRACO_SOURCEGENERATOR_ATTRIBUTE")]
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
