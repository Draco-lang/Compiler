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
public sealed class GreenTreeSourceGenerator : SourceGeneratorBase<GreenTreeGenerator.Settings>
{
    public override string TopLevelAttributeFullName => "Draco.RedGreenTree.Attributes.GreenTreeAttribute";

    protected override GreenTreeGenerator.Settings? ReadSettings(
        INamedTypeSymbol targetType,
        AttributeData attributeData) => new GreenTreeGenerator.Settings(targetType);

    protected override string GenerateCode(GreenTreeGenerator.Settings settings) =>
        GreenTreeGenerator.Generate(settings);
}
