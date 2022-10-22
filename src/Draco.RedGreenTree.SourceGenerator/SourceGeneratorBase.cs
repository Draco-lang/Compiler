using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.RedGreenTree.SourceGenerator;

/// <summary>
/// Utility base class for source generators.
/// </summary>
/// <typeparam name="TSettings">The settings type the generator reads up from source.</typeparam>
public abstract class SourceGeneratorBase<TSettings> : IIncrementalGenerator
{
    public static readonly string AttributeNamespace = "Draco.RedGreenTree";

    /// <summary>
    /// The attribute name (without the namespace) that initiates this source generator.
    /// </summary>
    public abstract string TopLevelAttributeName { get; }

    /// <summary>
    /// The source (without the namespace declaration) of the attribute named <see cref="TopLevelAttributeName"/>.
    /// </summary>
    public abstract string TopLevelAttributeSource { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fullyQualifiedAttributeName = $"{AttributeNamespace}.{this.TopLevelAttributeName}";

        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(
                $"{this.TopLevelAttributeName}.g.cs",
                $$"""
                namespace {{AttributeNamespace}};

                {{this.TopLevelAttributeSource}}
                """);
        });

        var settingsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: fullyQualifiedAttributeName,
            predicate: (node, ct) => node is TypeDeclarationSyntax,
            transform: (ctx, ct) =>
            {
                if (ctx.TargetSymbol is not INamedTypeSymbol targetType) return default;
                var attribute = ctx.Attributes
                    .First(a => a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName);
                return (TargetType: targetType, Settings: this.ReadSettings(targetType, attribute));
            })
            .Where(pair => pair.Settings is not null)
            .Select((pair, _) => (TargetType: pair.TargetType, Settings: pair.Settings!));

        context.RegisterSourceOutput(settingsProvider, (ctx, pair) =>
        {
            var source = this.GenerateCode(pair.Settings);
            ctx.AddSource($"{pair.TargetType.Name}.g.cs", source);
        });
    }

    protected abstract TSettings? ReadSettings(INamedTypeSymbol targetType, AttributeData attributeData);

    protected abstract string GenerateCode(TSettings settings);
}
