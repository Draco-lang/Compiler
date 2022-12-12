using Microsoft.CodeAnalysis;

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
