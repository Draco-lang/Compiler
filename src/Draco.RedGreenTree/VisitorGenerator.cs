using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        generator.GenerateVisitorBaseClass();
        return generator.writer.Code;
    }

    private readonly INamedTypeSymbol root;
    private readonly HashSet<INamedTypeSymbol> treeNodes;
    private readonly CodeWriter writer = new();

    private VisitorGenerator(INamedTypeSymbol root)
    {
        this.root = root;
        this.treeNodes = new(SymbolEqualityComparer.Default);
        var relevantNodes = root
            .EnumerateContainedTypeTree()
            .Where(n => n.DeclaredAccessibility == Accessibility.Public);
        foreach (var n in relevantNodes) this.treeNodes.Add(n);
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
            .Write("internal partial interface IParseTreeVisitor<out T>")
            .Write("{");

        foreach (var node in this.treeNodes)
        {
            this.writer
                .Write("public")
                .Write("T")
                .Write(this.GetVisitorMethodName(node))
                .Write($"({node.ToDisplayString()} node);");
        }

        this.writer
            .Write("}");
    }

    private void GenerateVisitorBaseClass()
    {
        this.writer
            .Write("internal abstract partial class ParseTreeVisitorBase<T> : IParseTreeVisitor<T>")
            .Write("{")
            .Write("protected virtual T Default => default!;");
        foreach (var node in this.treeNodes) this.GenerateVisitorMethodForType(node);
        this.writer
            .Write("}");
    }

    private void GenerateVisitorMethodForType(INamedTypeSymbol type)
    {
        static int AbstractFirst(INamedTypeSymbol s) => s.IsAbstract ? 0 : 1;

        // NOTE: We order the subtypes abstract first, not to hide any members
        var subtypes = type
            .EnumerateContainedTypeTree()
            .Where(n => !SymbolEqualityComparer.Default.Equals(n, type))
            .Where(this.treeNodes.Contains)
            .OrderBy(x => x, Comparer<INamedTypeSymbol>.Create((a, b) => AbstractFirst(a) - AbstractFirst(b)))
            .ToList();

        this.writer
            .Write("public virtual T")
            .Write(this.GetVisitorMethodName(type))
            .Write($"({type.ToDisplayString()} node)");
        if (type.IsAbstract)
        {
            this.writer
                .Write("=>")
                .Write("node switch")
                .Write("{");
            foreach (var subtype in subtypes)
            {
                this.writer
                    .Write(subtype.ToDisplayString())
                    .Write("n")
                    .Write("=>")
                    .Write($"this.{this.GetVisitorMethodName(subtype)}(n),");
            }
            this.writer
                .Write("_ => throw new System.ArgumentOutOfRangeException(nameof(node)),")
                .Write("};");
        }
        else
        {
            // NOTE: For now we don't handle this
            Debug.Assert(subtypes.Count == 0);

            this.writer
                .Write("{");
            foreach (var prop in type.GetSanitizedProperties())
            {
                // NOTE: The set receives the appropriate comparer
#pragma warning disable RS1024 // Symbols should be compared for equality
                if (!this.treeNodes.Contains(prop.Type)) continue;
#pragma warning restore RS1024 // Symbols should be compared for equality

                var propType = (INamedTypeSymbol)prop.Type;
                if (propType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    this.writer.Write($"if (node.{prop.Name} is not null)");
                }
                this.writer.Write($"this.{this.GetVisitorMethodName(propType)}(node.{prop.Name});");
            }
            this.writer
                .Write("}");
        }
    }
}
