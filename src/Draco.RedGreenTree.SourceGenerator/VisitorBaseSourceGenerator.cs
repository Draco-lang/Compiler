using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGenerator;

[Generator]
public sealed class VisitorBaseSourceGenerator : SourceGeneratorBase<VisitorBaseGenerator.Settings>
{
    public override string TopLevelAttributeFullName => "Draco.RedGreenTree.Attributes.VisitorBaseAttribute";

    protected override VisitorBaseGenerator.Settings? ReadSettings(
        INamedTypeSymbol targetType,
        AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length != 2) return null;
        var arg1 = attributeData.ConstructorArguments[0];
        var arg2 = attributeData.ConstructorArguments[1];
        if (arg1.Value is not INamedTypeSymbol greenRootType) return null;
        if (arg2.Value is not INamedTypeSymbol redRootType) return null;
        return new VisitorBaseGenerator.Settings(greenRootType, redRootType, targetType);
    }

    protected override string GenerateCode(VisitorBaseGenerator.Settings settings) =>
        VisitorBaseGenerator.Generate(settings);
}
