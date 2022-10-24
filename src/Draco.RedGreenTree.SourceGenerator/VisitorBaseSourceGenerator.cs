using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.RedGreenTree.SourceGenerator;

[Generator]
public sealed class VisitorBaseSourceGenerator : SourceGeneratorBase<VisitorBaseGenerator.Settings>
{
    public override string TopLevelAttributeFullName => "Draco.RedGreenTree.Attributes.VisitorBaseAttribute";

    protected override VisitorBaseGenerator.Settings? ReadSettings(
        INamedTypeSymbol targetType,
        AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length != 1) return null;
        var arg = attributeData.ConstructorArguments[0];
        if (arg.Value is not INamedTypeSymbol rootType) return null;
        return new VisitorBaseGenerator.Settings(rootType, targetType);
    }

    protected override string GenerateCode(VisitorBaseGenerator.Settings settings) =>
        VisitorBaseGenerator.Generate(settings);
}
