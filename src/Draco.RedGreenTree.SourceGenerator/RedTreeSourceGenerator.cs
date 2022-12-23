using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGenerator;

[Generator]
public sealed class RedTreeSourceGenerator : SourceGeneratorBase<RedTreeGenerator.Settings>
{
    public override string TopLevelAttributeFullName => "Draco.RedGreenTree.Attributes.RedTreeAttribute";

    protected override RedTreeGenerator.Settings? ReadSettings(
        INamedTypeSymbol targetType,
        AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length != 3) return null;
        var arg0 = attributeData.ConstructorArguments[0];
        var arg1 = attributeData.ConstructorArguments[1];
        var arg2 = attributeData.ConstructorArguments[2];
        if (arg0.Value is not INamedTypeSymbol greenTreeType) return null;
        if (arg1.Value is not INamedTypeSymbol redTreeType) return null;
        if (arg2.Value is not INamedTypeSymbol rootType) return null;
        return new RedTreeGenerator.Settings(
            greenTreeType: greenTreeType,
            greenRootType: rootType,
            redTreeType: redTreeType,
            redRootType: targetType);
    }

    protected override string GenerateCode(RedTreeGenerator.Settings settings) =>
        RedTreeGenerator.Generate(settings);
}
