using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGenerator;

[Generator]
public sealed class TransformerBaseSourceGenerator : SourceGeneratorBase<TransformerBaseGenerator.Settings>
{
    public override string TopLevelAttributeFullName => "Draco.RedGreenTree.Attributes.TransformerBaseAttribute";

    protected override TransformerBaseGenerator.Settings? ReadSettings(
        INamedTypeSymbol targetType,
        AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length != 2) return null;
        var arg1 = attributeData.ConstructorArguments[0];
        var arg2 = attributeData.ConstructorArguments[1];
        if (arg1.Value is not INamedTypeSymbol greenRootType) return null;
        if (arg2.Value is not INamedTypeSymbol redRootType) return null;
        return new TransformerBaseGenerator.Settings(greenRootType, redRootType, targetType);
    }

    protected override string GenerateCode(TransformerBaseGenerator.Settings settings) =>
        TransformerBaseGenerator.Generate(settings);
}
