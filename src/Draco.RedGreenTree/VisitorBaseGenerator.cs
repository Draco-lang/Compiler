using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.RedGreenTree.Attributes;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Generates the visitor base class for the red tree based on the green tree.
/// </summary>
public sealed class VisitorBaseGenerator : GeneratorBase
{
    public sealed class Settings
    {
        public INamedTypeSymbol GreenRootType { get; set; }
        public INamedTypeSymbol RedRootType { get; set; }
        public INamedTypeSymbol VisitorType { get; set; }
        public string DefaultPropertyName { get; set; } = "Default";
        public Func<INamedTypeSymbol, string> GetRedName { get; set; } = x => x.Name;

        public Settings(
            INamedTypeSymbol greenRootType,
            INamedTypeSymbol redRootType,
            INamedTypeSymbol visitorType)
        {
            this.GreenRootType = greenRootType;
            this.RedRootType = redRootType;
            this.VisitorType = visitorType;
        }
    }

    public static string Generate(Settings settings)
    {
        var generator = new VisitorBaseGenerator(settings);
        return generator.Generate();
    }

    private readonly Settings settings;
    private readonly HashSet<INamedTypeSymbol> greenTreeNodes;
    private readonly HashSet<string> generatedMethodNames = new();
    private readonly HashSet<string> customMethodNames;
    private readonly List<string> skippedProperties = new();
    private readonly CodeWriter headerWriter = new();
    private readonly CodeWriter contentWriter = new();

    private INamedTypeSymbol GreenRootType => this.settings.GreenRootType;
    private INamedTypeSymbol RedRootType => this.settings.RedRootType;
    private INamedTypeSymbol VisitorType => this.settings.VisitorType;
    private string DefaultPropertyName => this.settings.DefaultPropertyName;

    private VisitorBaseGenerator(Settings settings)
    {
        this.settings = settings;

        this.greenTreeNodes = new(SymbolEqualityComparer.Default);
        this.ExtractGreenTreeNodes();

        this.customMethodNames = new();
        this.ExtractCustomVisitorMethods();
    }

    private string GetRedClassName(INamedTypeSymbol greenType) => SymbolEquals(greenType, this.GreenRootType)
        ? this.RedRootType.Name
        : this.settings.GetRedName(greenType);
    private string GetFullRedClassName(INamedTypeSymbol greenType)
    {
        if (!this.greenTreeNodes.Contains(greenType) && !greenType.IsSubtypeOf(this.settings.GreenRootType))
        {
            return greenType.ToDisplayString();
        }
        var typeName = string.Join(".", greenType.EnumerateNestingChain().Select(this.GetRedClassName));
        if (this.RedRootType.ContainingNamespace is null) return typeName;
        return $"{this.RedRootType.ContainingNamespace.ToDisplayString()}.{typeName}";
    }

    private void ExtractGreenTreeNodes()
    {
        var relevantNodes = this.GreenRootType
            .EnumerateContainedTypeTree();
        foreach (var n in relevantNodes) this.greenTreeNodes.Add(n);
        foreach (var n in this.greenTreeNodes) this.generatedMethodNames.Add(this.GenerateVisitorMethodName(n));
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
        this.GenerateBaseClass();
        this.GenerateSkippedProperties();

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

    private void GenerateSkippedProperties()
    {
        if (this.skippedProperties.Count == 0) return;
        this.headerWriter.Write("//");
        this.headerWriter.Write("// Skipped properties:");
        foreach (var prop in this.skippedProperties) this.headerWriter.Write($"// - {prop}");
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
        if (!this.greenTreeNodes.Contains(symbol)) return $"Visit{symbol.Name}";
        // For anything else, we read up the names in reverse order, excluding the root
        var parts = symbol.EnumerateNestingChain().Skip(1).Reverse().Select(this.GetRedClassName);
        return $"Visit{string.Join("", parts)}";
    }

    private void GenerateBaseClass()
    {
        this.contentWriter
            .Write(this.VisitorType.DeclaredAccessibility)
            .Write($"abstract partial class {this.VisitorType.Name}<T>")
            .Write("{")
            .Write($"protected virtual T {this.DefaultPropertyName} => default!;");
        foreach (var node in this.greenTreeNodes)
        {
            var methodName = this.GenerateVisitorMethodName(node);
            if (this.customMethodNames.Contains(methodName)) continue;
            if (!node.IsSubtypeOf(this.GreenRootType)) continue;
            this.GenerateVisitorMethodForType(node);
        }
        this.contentWriter.Write("}");
    }

    private void GenerateVisitorMethodForType(INamedTypeSymbol type)
    {
        static int AbstractFirst(INamedTypeSymbol s) => s.IsAbstract ? 0 : 1;

        // NOTE: We order the subtypes abstract first, not to hide any members
        var subtypes = type
            .EnumerateContainedTypeTree()
            .Where(n => !SymbolEquals(n, type))
            .Where(this.greenTreeNodes.Contains)
            .Where(n => SymbolEquals(n.BaseType, type))
            .OrderBy(x => x, Comparer<INamedTypeSymbol>.Create((a, b) => AbstractFirst(a) - AbstractFirst(b)))
            .ToList();

        this.contentWriter
            .Write("public virtual T")
            .Write(this.GenerateVisitorMethodName(type))
            .Write($"({this.GetFullRedClassName(type)} node)");
        if (type.IsAbstract)
        {
            this.contentWriter
                .Write("=>")
                .Write("node switch")
                .Write("{");
            foreach (var subtype in subtypes)
            {
                this.contentWriter
                    .Write(this.GetFullRedClassName(subtype))
                    .Write("n")
                    .Write("=>")
                    .Write($"this.{this.GenerateVisitorMethodName(subtype)}(n),");
            }
            this.contentWriter
                .Write("_ => throw new System.ArgumentOutOfRangeException(nameof(node)),")
                .Write("};");
        }
        else
        {
            // NOTE: For now we don't handle this
            Debug.Assert(subtypes.Count == 0);

            this.contentWriter.Write("{");
            foreach (var prop in type.GetSanitizedProperties())
            {
                if (prop.Type is not INamedTypeSymbol propType) continue;
                if (prop.IsGenerated()) continue;

                if (!this.IsVisitableProperty(prop))
                {
                    this.skippedProperties.Add($"{type.ToDisplayString()}.{prop.Name}");
                    continue;
                }

                var accessor = string.Empty;
                if (propType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    if (propType.IsValueType) accessor = ".Value";
                    this.contentWriter.Write($"if (node.{prop.Name} is not null)");
                }
                var methodName = this.GenerateVisitorMethodName(propType);
                this.contentWriter.Write($"this.{methodName}(node.{prop.Name}{accessor});");
            }
            this.contentWriter
                .Write($"return this.{this.DefaultPropertyName};")
                .Write("}");
        }
    }

    private bool IsVisitableProperty(IPropertySymbol prop)
    {
        if (prop.IsStatic) return false;
        if (prop.Type is not INamedTypeSymbol propType) return false;
        if (HasIgnoreFlag(prop, IgnoreFlags.VisitorVisit)) return false;

        // Don't leak types
        if ((int)prop.DeclaredAccessibility < (int)this.RedRootType.DeclaredAccessibility) return false;

        var methodName = this.GenerateVisitorMethodName(propType);
        if (this.generatedMethodNames.Contains(methodName)) return true;

        if (this.customMethodNames.Contains(methodName))
        {
            // TODO: Some custom checking?
            return true;
        }

        return false;
    }
}
