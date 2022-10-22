using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.RedGreenTree.SourceGenerator;

[Generator]
public sealed class VisitorBaseSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        const string attributeName = "Draco.RedGreenTree.VisitorBaseAttribute";

        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(
                "VisitorBaseAttribute.g.cs",
                """
                namespace Draco.RedGreenTree;

                [global::System.AttributeUsage(global::System.AttributeTargets.Class)]
                public sealed class VisitorBaseAttribute : global::System.Attribute
                {
                    public global::System.Type RootType { get; }

                    public VisitorBaseAttribute(global::System.Type rootType)
                    {
                        this.RootType = rootType;
                    }
                }
                """);
        });

        var visitors = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: attributeName,
                predicate: (node, ct) => node is TypeDeclarationSyntax,
                transform: (ctx, ct) =>
                {
                    if (ctx.TargetSymbol is not INamedTypeSymbol visitorType) return null;
                    var attribute = ctx.Attributes
                        .First(a => a.AttributeClass?.ToDisplayString() == attributeName);
                    if (attribute.ConstructorArguments.Length != 1) return null;
                    var arg = attribute.ConstructorArguments[0];
                    if (arg.Value is not INamedTypeSymbol rootType) return null;
                    return new VisitorBaseGenerator.Settings(rootType, visitorType);
                })
                .Where(n => n is not null)
                .Select((n, _) => n!);

        context.RegisterSourceOutput(visitors, (ctx, settings) =>
        {
            var source = VisitorBaseGenerator.Generate(settings);
            ctx.AddSource($"{settings.VisitorType.Name}.g.cs", source);
        });
    }
}
