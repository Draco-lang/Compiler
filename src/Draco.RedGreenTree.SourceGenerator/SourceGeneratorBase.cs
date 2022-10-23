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
    /// <summary>
    /// The fully qualified name of the attribute that initiates this source generator.
    /// </summary>
    public abstract string TopLevelAttributeFullName { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var settingsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: this.TopLevelAttributeFullName,
            predicate: (node, ct) => node is TypeDeclarationSyntax,
            transform: (ctx, ct) =>
            {
                if (ctx.TargetSymbol is not INamedTypeSymbol targetType) return default;
                var attribute = ctx.Attributes
                    .First(a => a.AttributeClass?.ToDisplayString() == this.TopLevelAttributeFullName);
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

    /// <summary>
    /// From the matched type and top-level attribute data constructs the settings for code generation.
    /// </summary>
    /// <param name="targetType">The type that had the attribute.</param>
    /// <param name="attributeData">The attribute data that was attached.</param>
    /// <returns>The settings for code generation, or null if the settings are invalid.</returns>
    protected abstract TSettings? ReadSettings(INamedTypeSymbol targetType, AttributeData attributeData);

    /// <summary>
    /// Generates code from the read up settings.
    /// </summary>
    /// <param name="settings">The read settings.</param>
    /// <returns>The generated C# code.</returns>
    protected abstract string GenerateCode(TSettings settings);
}
