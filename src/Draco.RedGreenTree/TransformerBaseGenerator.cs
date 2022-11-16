using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Generates the transformer base class for the red tree based on the green tree.
/// </summary>
public sealed class TransformerBaseGenerator : GeneratorBase
{
    public sealed class Settings
    {
        public INamedTypeSymbol GreenRootType { get; set; }
        public INamedTypeSymbol RedRootType { get; set; }
        public INamedTypeSymbol TransformerType { get; set; }
        public Func<INamedTypeSymbol, string> GetRedName { get; set; } = x => x.Name;

        public Settings(
            INamedTypeSymbol greenRootType,
            INamedTypeSymbol redRootType,
            INamedTypeSymbol transformerType)
        {
            this.GreenRootType = greenRootType;
            this.RedRootType = redRootType;
            this.TransformerType = transformerType;
        }
    }

    public static string Generate(Settings settings)
    {
        var generator = new TransformerBaseGenerator(settings);
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
    private INamedTypeSymbol TransformerType => this.settings.TransformerType;

    private TransformerBaseGenerator(Settings settings)
    {
        this.settings = settings;

        this.greenTreeNodes = new(SymbolEqualityComparer.Default);
        this.ExtractGreenTreeNodes();

        this.customMethodNames = new();
        this.ExtractCustomTransformerMethods();
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
        foreach (var n in this.greenTreeNodes) this.generatedMethodNames.Add(this.GenerateTransformerMethodName(n));
    }

    private void ExtractCustomTransformerMethods()
    {
        var result = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
        var methods = this.TransformerType
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => !m.IsStatic)
            .Where(m => m.Parameters.Length == 2)
            .Where(m => m.Name.StartsWith("Transform"));
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
        if (this.TransformerType.ContainingNamespace is null) return;
        this.contentWriter
            .Write("namespace ")
            .Write(this.TransformerType.ContainingNamespace)
            .Write(";");
    }

    private void GenerateSkippedProperties()
    {
        if (this.skippedProperties.Count == 0) return;
        this.headerWriter.Write("//");
        this.headerWriter.Write("// Skipped properties:");
        foreach (var prop in this.skippedProperties) this.headerWriter.Write($"// - {prop}");
    }

    private string GenerateTransformerMethodName(INamedTypeSymbol symbol)
    {
        // Nullable value types are unwrapped
        if (symbol.IsValueType
         && symbol.NullableAnnotation == NullableAnnotation.Annotated
         && symbol.TypeArguments[0] is INamedTypeSymbol namedTypeArg)
        {
            symbol = namedTypeArg;
        }
        // For anything not part of the tree, we just generate a TransformNAME
        if (!this.greenTreeNodes.Contains(symbol)) return $"Transform{symbol.Name}";
        // For anything else, we read up the names in reverse order, excluding the root
        var parts = symbol.EnumerateNestingChain().Skip(1).Reverse().Select(this.GetRedClassName);
        return $"Transform{string.Join("", parts)}";
    }

    private void GenerateBaseClass()
    {
        this.contentWriter
            .Write(this.TransformerType.DeclaredAccessibility)
            .Write($"abstract partial class {this.TransformerType.Name}")
            .Write("{");
        foreach (var node in this.greenTreeNodes)
        {
            var methodName = this.GenerateTransformerMethodName(node);
            if (this.customMethodNames.Contains(methodName)) continue;
            if (!node.IsSubtypeOf(this.GreenRootType)) continue;
            this.GenerateTransformerMethodForType(node);
        }
        this.contentWriter.Write("}");
    }

    private void GenerateTransformerMethodForType(INamedTypeSymbol type)
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

        var typeName = this.GetFullRedClassName(type);
        this.contentWriter
            .Write("public virtual")
            .Write(typeName)
            .Write(this.GenerateTransformerMethodName(type))
            .Write("(")
            .Write($"{typeName} node")
            .Write(",")
            .Write("out bool changed")
            .Write(")");
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
                    .Write($"this.{this.GenerateTransformerMethodName(subtype)}(n, out changed),");
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

            // Collect transformed elements
            var transformedMembers = new List<(string Transformed, string HasChanged)>();

            foreach (var prop in type.GetSanitizedProperties())
            {
                if (prop.Type is not INamedTypeSymbol propType) continue;
                if (prop.IsGenerated()) continue;

                if (!this.IsTransformableProperty(prop))
                {
                    this.skippedProperties.Add($"{type.ToDisplayString()}.{prop.Name}");
                    continue;
                }

                var transformedProp = $"tr{prop.Name}";
                var changedFlag = $"{ToCamelCase(prop.Name)}Changed";
                transformedMembers.Add((transformedProp, changedFlag));

                this.contentWriter.Write($"var {changedFlag} = false;");

                this.contentWriter.Write($"var {transformedProp} =");

                var accessor = string.Empty;
                if (propType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    if (propType.IsValueType) accessor = ".Value";
                    this.contentWriter.Write($"node.{prop.Name} is null ? null :");
                }
                var methodName = this.GenerateTransformerMethodName(propType);
                this.contentWriter.Write($"this.{methodName}(node.{prop.Name}{accessor}, out {changedFlag});");
            }

            // Changed arg
            if (transformedMembers.Count > 0)
            {
                this.contentWriter
                    .Write("changed =")
                    .Write(string.Join("||", transformedMembers.Select(m => m.HasChanged)))
                    .Write(";");
            }
            else
            {
                this.contentWriter.Write("changed = false;");
            }

            // Optimization, if all are equal to their original, we return the old reference
            this.contentWriter.Write("if (!changed) return node;");

            // Construct new instance
            this.contentWriter
                .Write($"return new {typeName}(")
                .Write(string.Join(", ", transformedMembers.Select(m => m.Transformed)))
                .Write(");");

            this.contentWriter
                .Write("}");
        }
    }

    private bool IsTransformableProperty(IPropertySymbol prop)
    {
        if (prop.Type is not INamedTypeSymbol propType) return false;

        // Don't leak types
        if ((int)prop.DeclaredAccessibility < (int)this.RedRootType.DeclaredAccessibility) return false;

        var methodName = this.GenerateTransformerMethodName(propType);
        if (this.generatedMethodNames.Contains(methodName)) return true;

        if (this.customMethodNames.Contains(methodName))
        {
            // TODO: Some custom checking?
            return true;
        }

        return false;
    }
}
