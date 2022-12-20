using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGenerator;

[Generator]
public sealed class SyntaxFactorySourceGenerator : SourceGeneratorBase<SyntaxFactoryGenerator.Settings>
{
    public override string TopLevelAttributeFullName => "Draco.RedGreenTree.Attributes.SyntaxFactoryAttribute";

    protected override SyntaxFactoryGenerator.Settings? ReadSettings(
        INamedTypeSymbol targetType,
        AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length != 2) return null;
        var arg1 = attributeData.ConstructorArguments[0];
        var arg2 = attributeData.ConstructorArguments[1];
        if (arg1.Value is not INamedTypeSymbol greenRootType) return null;
        if (arg2.Value is not INamedTypeSymbol redRootType) return null;
        return new SyntaxFactoryGenerator.Settings(greenRootType, redRootType, targetType);
    }

    protected override string GenerateCode(SyntaxFactoryGenerator.Settings settings) =>
        SyntaxFactoryGenerator.Generate(settings);
}
