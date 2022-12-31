using System;

namespace Draco.RedGreenTree.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SyntaxFactoryAttribute : Attribute
{
    public Type GreenTree { get; }
    public Type RedTree { get; }

    public SyntaxFactoryAttribute(Type greenTree, Type redTree)
    {
        this.GreenTree = greenTree;
        this.RedTree = redTree;
    }
}
