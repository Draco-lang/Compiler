using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Generates the visitor interface and base class.
/// </summary>
public sealed class VisitorGenerator
{
    public static string Generate(INamedTypeSymbol rootType)
    {
        var generator = new VisitorGenerator(rootType);
        generator.GenerateVisitorInterface();
        return generator.writer.Code;
    }

    private readonly INamedTypeSymbol root;
    private readonly CodeWriter writer = new();

    private VisitorGenerator(INamedTypeSymbol root)
    {
        this.root = root;
    }

    private string GetVisitorMethodName(INamedTypeSymbol symbol)
    {
        // For anything not part of the tree, we just generate a VisitNAME
        if (!symbol.IsSubtypeOf(this.root)) return $"Visit{symbol.Name}";
        // For anything else, we read up the names in reverse order, excluding the root
        var parts = symbol.EnumerateNestingChain().Skip(1).Reverse().Select(n => n.Name);
        return $"Visit{string.Join("", parts)}";
    }

    private void GenerateVisitorInterface()
    {
        this.writer
            .Write("internal interface IParseTreeVisitor<out T>")
            .Write("{");

        foreach (var node in this.root.EnumerateContainedTypeTree())
        {
            if (node.DeclaredAccessibility != Accessibility.Public) continue;
            this.writer
                .Write("public")
                .Write("T")
                .Write(this.GetVisitorMethodName(node))
                .Write($"({node.ToDisplayString()} node);");
        }

        this.writer
            .Write("}");
    }
}
