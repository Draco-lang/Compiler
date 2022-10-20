using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Implements the width property for green nodes.
/// </summary>
public sealed class GreenTreeGenerator
{
    public static string Generate(INamedTypeSymbol rootType)
    {
        var generator = new GreenTreeGenerator(rootType);
        generator.Generate();
        return generator.writer.Code;
    }

    private readonly INamedTypeSymbol root;
    private readonly CodeWriter writer = new();

    private GreenTreeGenerator(INamedTypeSymbol root)
    {
        this.root = root;
    }

    private void Generate()
    {
        foreach (var type in this.root.EnumerateContainedTypeTree())
        {
            if (!type.IsSubtypeOf(this.root)) continue;
            this.GenerateForType(type);
        }
    }

    private void GenerateForType(INamedTypeSymbol type)
    {
        // Wrapping types
        foreach (var nest in type.EnumerateNestingChain())
        {
            this.writer
                .Write(nest.DeclaredAccessibility)
                .Write(nest.GetTypeKind(partial: true))
                .Write(nest.Name)
                .Write("{");
        }

        if (!type.IsAbstract) this.GenerateWidthForType(type);

        // Close braces
        foreach (var _ in type.EnumerateNestingChain()) this.writer.Write("}");
    }

    private void GenerateWidthForType(INamedTypeSymbol type)
    {
        // Sum up each member that has a Width attribute
        // For simplicity, we call a GetWidth that the user can roll themselves
        var memberWidths = type
            .GetSanitizedProperties()
            .Select(m => $"GetWidth(this.{m.Name})");
        this.writer
            .Write("private int? width;")
            .Write($$"""
            public override int Width
            {
                get
                {
                    if (this.width is null)
                    {
                        var acc = 0;
                        {{string.Join("", memberWidths.Select(w => $"acc += {w};"))}}
                        this.width = acc;
                    }
                    return this.width.Value;
                }
            }
            """);
    }
}
