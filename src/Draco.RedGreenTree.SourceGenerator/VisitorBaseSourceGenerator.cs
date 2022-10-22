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
    public override string TopLevelAttributeName => "VisitorBaseAttribute";

    public override string TopLevelAttributeSource => """
        [global::System.AttributeUsage(global::System.AttributeTargets.Class)]
        public sealed class VisitorBaseAttribute : global::System.Attribute
        {
            public global::System.Type RootType { get; }
        
            public VisitorBaseAttribute(global::System.Type rootType)
            {
                this.RootType = rootType;
            }
        }
        """;

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
