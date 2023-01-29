using System;
using System.Diagnostics;

namespace Draco.RedGreenTree.Attributes;

[Conditional("DRACO_SOURCEGENERATOR_ATTRIBUTE")]
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
