using System.Linq;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGeneration;

[Generator]
public sealed class RedTreeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        const string attributeName = "Draco.RedGreenTree.RedTreeAttribute";

        var trees = context.ForTypesWithAttribute(attributeName)
            .Select(RedGreenTypePair? (attributeAndType, ct) =>
            {
                var attribute = attributeAndType.Attribute;
                var redType = attributeAndType.Type;

                if (attribute.ConstructorArguments.Length != 1) return null;
                var arg = attribute.ConstructorArguments[0];
                if (arg.Kind != TypedConstantKind.Type) return null;
                if (arg.Type is not INamedTypeSymbol greenType) return null;

                return new(redType, greenType);
            })
            .NotNull();

        context.RegisterSourceOutput(trees, (ctx, types) =>
        {
            var greenType = types.Green;
            var redType = types.Red;

            var source = RedTreeGenerator.Generate(greenType);

            var name = $"{redType.Name}.g.cs";

            ctx.AddSource(name, source);
        });
    }

    private readonly record struct RedGreenTypePair(INamedTypeSymbol Red, INamedTypeSymbol Green);
}
