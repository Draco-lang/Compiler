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
    public static string GenerateInterface(INamedTypeSymbol rootType, INamedTypeSymbol visitorType)
    {
        var generator = new VisitorGenerator(rootType, visitorType);
        generator.GenerateVisitorInterface();
        return generator.writer.Code;
    }

    public static string GenerateBase(INamedTypeSymbol rootType, INamedTypeSymbol visitorType)
    {
        var generator = new VisitorGenerator(rootType, visitorType);
        generator.GenerateVisitorBaseClass();
        generator.AppendSkippedProperties();
        return generator.writer.Code;
    }

    private readonly INamedTypeSymbol rootType;
    private readonly INamedTypeSymbol visitorType;
    private readonly HashSet<INamedTypeSymbol> treeNodes;
    private readonly HashSet<string> customMethodNames;
    private readonly List<string> skippedProperties = new();
    private readonly CodeWriter writer = new();

    private VisitorGenerator(INamedTypeSymbol root, INamedTypeSymbol visitor)
    {
        this.rootType = root;
        this.visitorType = visitor;

        this.treeNodes = new(SymbolEqualityComparer.Default);
        this.ExtractTreeNodes();

        this.customMethodNames = new();
        this.ExtractCustomVisitorMethods();
    }

    private void ExtractTreeNodes()
    {
        var relevantNodes = this.rootType
            .EnumerateContainedTypeTree();
        foreach (var n in relevantNodes) this.treeNodes.Add(n);
    }

    private void ExtractCustomVisitorMethods()
    {
        var result = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
        var methods = this.visitorType
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => !m.IsStatic)
            .Where(m => m.Parameters.Length == 1)
            .Where(m => m.Name.StartsWith("Visit"));
        foreach (var m in methods) this.customMethodNames.Add(m.Name);
    }

    private void AppendSkippedProperties()
    {
        if (this.skippedProperties.Count == 0) return;
        this.writer.Write("//");
        this.writer.Write("// Skipped properties:");
        foreach (var prop in this.skippedProperties) this.writer.Write($"// - {prop}");
    }

    private string GenerateVisitorMethodName(INamedTypeSymbol symbol)
    {
        // Nullable value types are unwrapped
        if (symbol.IsValueType
         && symbol.NullableAnnotation == NullableAnnotation.Annotated
         && symbol.TypeArguments[0] is INamedTypeSymbol namedTypeArg)
        {
            symbol = namedTypeArg;
        }
        // For anything not part of the tree, we just generate a VisitNAME
        if (!this.treeNodes.Contains(symbol)) return $"Visit{symbol.Name}";
        // For anything else, we read up the names in reverse order, excluding the root
        var parts = symbol.EnumerateNestingChain().Skip(1).Reverse().Select(n => n.Name);
        return $"Visit{string.Join("", parts)}";
    }

    private void GenerateVisitorInterface()
    {
        this.writer
            .Write($"internal partial interface {this.visitorType.Name}<out T>")
            .Write("{");

        foreach (var node in this.treeNodes)
        {
            var methodName = this.GenerateVisitorMethodName(node);
            if (this.customMethodNames.Contains(methodName)) continue;
            if (!node.IsSubtypeOf(this.rootType)) continue;
            this.writer
                .Write("public")
                .Write("T")
                .Write(methodName)
                .Write($"({node.ToDisplayString()} node);");
        }

        this.writer
            .Write("}");
    }

    private void GenerateVisitorBaseClass()
    {
        this.writer
            .Write($"internal abstract partial class {this.visitorType.Name}<T>")
            .Write("{")
            .Write("protected virtual T Default => default!;");
        foreach (var node in this.treeNodes)
        {
            var methodName = this.GenerateVisitorMethodName(node);
            if (this.customMethodNames.Contains(methodName)) continue;
            if (!node.IsSubtypeOf(this.rootType)) continue;
            this.GenerateVisitorMethodForType(node);
        }
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
            .Where(n => SymbolEqualityComparer.Default.Equals(n.BaseType, type))
            .OrderBy(x => x, Comparer<INamedTypeSymbol>.Create((a, b) => AbstractFirst(a) - AbstractFirst(b)))
            .ToList();

        this.writer
            .Write("public virtual T")
            .Write(this.GenerateVisitorMethodName(type))
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
                    .Write($"this.{this.GenerateVisitorMethodName(subtype)}(n),");
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
                if (prop.Type is not INamedTypeSymbol propType) continue;

                var methodName = this.GenerateVisitorMethodName(propType);
                if (!this.customMethodNames.Contains(methodName) && !this.treeNodes.Contains(propType))
                {
                    this.skippedProperties.Add($"{type.ToDisplayString()}.{prop.Name}");
                    continue;
                }

                var accessor = string.Empty;
                if (propType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    if (propType.IsValueType) accessor = ".Value";
                    this.writer.Write($"if (node.{prop.Name} is not null)");
                }
                this.writer.Write($"this.{methodName}(node.{prop.Name}{accessor});");
            }
            this.writer
                .Write("return this.Default;")
                .Write("}");
        }
    }
}
