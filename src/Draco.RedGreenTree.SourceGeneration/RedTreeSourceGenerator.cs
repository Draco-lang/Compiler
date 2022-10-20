using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGeneration;

[Generator]
public sealed class RedTreeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var trees = context.ForTypesWithGreenTreeAttribute();

        context.RegisterSourceOutput(trees, (ctx, symbol) =>
        {
            var source = RedTreeGenerator.Generate(symbol);

            var redTypeName = RedTreeGenerator.GetRedClassName(symbol);
            var name = $"{redTypeName}.g.cs";

            ctx.AddSource(name, source);
        });
    }
}
