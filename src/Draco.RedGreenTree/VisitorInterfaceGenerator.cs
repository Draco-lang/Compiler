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
public sealed class VisitorInterfaceGenerator : GeneratorBase
{
    public sealed class Settings
    {
        public INamedTypeSymbol RootType { get; set; }
        public INamedTypeSymbol VisitorType { get; set; }

        public Settings(INamedTypeSymbol rootType, INamedTypeSymbol visitorType)
        {
            this.RootType = rootType;
            this.VisitorType = visitorType;
        }
    }

    public static string Generate(Settings settings)
    {
        var generator = new VisitorInterfaceGenerator(settings);
        return generator.Generate();
    }

    private readonly Settings settings;
    private readonly HashSet<INamedTypeSymbol> treeNodes;
    private readonly HashSet<string> customMethodNames;
    private readonly CodeWriter headerWriter = new();
    private readonly CodeWriter contentWriter = new();

    private INamedTypeSymbol RootType => this.settings.RootType;
    private INamedTypeSymbol VisitorType => this.settings.VisitorType;

    private VisitorInterfaceGenerator(Settings settings)
    {
        this.settings = settings;

        this.treeNodes = new(SymbolEqualityComparer.Default);
        this.ExtractTreeNodes();

        this.customMethodNames = new();
        this.ExtractCustomVisitorMethods();
    }

    private void ExtractTreeNodes()
    {
        var relevantNodes = this.RootType
            .EnumerateContainedTypeTree();
        foreach (var n in relevantNodes) this.treeNodes.Add(n);
    }

    private void ExtractCustomVisitorMethods()
    {
        var result = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
        var methods = this.VisitorType
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => !m.IsStatic)
            .Where(m => m.Parameters.Length == 1)
            .Where(m => m.Name.StartsWith("Visit"));
        foreach (var m in methods) this.customMethodNames.Add(m.Name);
    }

    protected override string Generate()
    {
        this.GenerateHeader();
        this.GenerateNamespace();
        this.GenerateInterface();

        return new CodeWriter()
            .Write(this.headerWriter)
            .Write("#nullable enable")
            .Write(this.contentWriter)
            .Write("#nullable restore")
            .ToString();
    }

    private void GenerateHeader()
    {
        this.headerWriter
            .Write(this.HeaderComment)
            .Write("//")
            .Write(SettingsToHeaderComment(this.settings));
    }

    private void GenerateNamespace()
    {
        if (this.VisitorType.ContainingNamespace is null) return;
        this.contentWriter
            .Write("namespace ")
            .Write(this.VisitorType.ContainingNamespace)
            .Write(";");
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

    private void GenerateInterface()
    {
        this.contentWriter
            .Write(this.VisitorType.DeclaredAccessibility)
            .Write($"partial interface {this.VisitorType.Name}<out T>")
            .Write("{");

        foreach (var node in this.treeNodes)
        {
            var methodName = this.GenerateVisitorMethodName(node);
            if (this.customMethodNames.Contains(methodName)) continue;
            if (!node.IsSubtypeOf(this.RootType)) continue;
            this.contentWriter
                .Write("public")
                .Write("T")
                .Write(methodName)
                .Write($"({node.ToDisplayString()} node);");
        }

        this.contentWriter.Write("}");
    }
}
