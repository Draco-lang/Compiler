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
        if (attributeData.ConstructorArguments.Length != 1) return null;
        var arg = attributeData.ConstructorArguments[0];
        if (arg.Value is not INamedTypeSymbol greenRootType) return null;
        return new RedTreeGenerator.Settings(greenRootType, targetType);
    }

    protected override string GenerateCode(RedTreeGenerator.Settings settings) =>
        RedTreeGenerator.Generate(settings);
}
