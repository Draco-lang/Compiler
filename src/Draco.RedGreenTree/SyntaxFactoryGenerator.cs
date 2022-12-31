using System;
using System.Collections.Generic;
using System.Linq;
using Draco.RedGreenTree.Attributes;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Generates factory functions for the tree.
/// </summary>
public sealed class SyntaxFactoryGenerator : GeneratorBase
{
    public sealed class Settings
    {
        public INamedTypeSymbol GreenRootType { get; set; }
        public INamedTypeSymbol RedRootType { get; set; }
        public INamedTypeSymbol FactoryType { get; set; }
        public Func<INamedTypeSymbol, string> GetRedName { get; set; } = x => x.Name;
        public string GreenPropertyName { get; set; } = "Green";
        public string ToRedMethodName { get; set; } = "ToRed";
        public string ToGreenMethodName { get; set; } = "ToGreen";

        public Settings(
            INamedTypeSymbol greenRootType,
            INamedTypeSymbol redRootType,
            INamedTypeSymbol factoryType)
        {
            this.GreenRootType = greenRootType;
            this.RedRootType = redRootType;
            this.FactoryType = factoryType;
        }
    }

    public static string Generate(Settings settings)
    {
        var generator = new SyntaxFactoryGenerator(settings);
        return generator.Generate();
    }

    private readonly Settings settings;
    private readonly HashSet<INamedTypeSymbol> greenTreeNodes;
    private readonly HashSet<INamedTypeSymbol> typesInParseTree = new(SymbolEqualityComparer.Default);
    private readonly CodeWriter headerWriter = new();
    private readonly CodeWriter contentWriter = new();

    private INamedTypeSymbol FactoryType => this.settings.FactoryType;
    private INamedTypeSymbol GreenRootType => this.settings.GreenRootType;
    private INamedTypeSymbol RedRootType => this.settings.RedRootType;
    private string GreenPropertyName => this.settings.GreenPropertyName;
    private string ToRedMethodName => this.settings.ToRedMethodName;
    private string ToGreenMethodName => this.settings.ToGreenMethodName;
    private string GetRedClassName(INamedTypeSymbol type) => SymbolEquals(type, this.GreenRootType)
        ? this.RedRootType.Name
        : this.settings.GetRedName(type);
    private string GetFullRedClassName(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) return type.ToDisplayString();
        if (!this.typesInParseTree.Contains(namedType) && !namedType.IsSubtypeOf(this.settings.GreenRootType))
        {
            return type.ToDisplayString();
        }
        var typeName = string.Join(".", namedType.EnumerateNestingChain().Select(this.GetRedClassName));
        if (this.RedRootType.ContainingNamespace is null) return typeName;
        return $"{this.RedRootType.ContainingNamespace.ToDisplayString()}.{typeName}";
    }

    private SyntaxFactoryGenerator(Settings settings)
    {
        this.settings = settings;
        foreach (var item in settings.GreenRootType.EnumerateContainedTypeTree()) this.typesInParseTree.Add(item);

        this.greenTreeNodes = new(SymbolEqualityComparer.Default);
        this.ExtractGreenTreeNodes();
    }

    private void ExtractGreenTreeNodes()
    {
        var relevantNodes = this.GreenRootType
            .EnumerateContainedTypeTree();
        foreach (var n in relevantNodes) this.greenTreeNodes.Add(n);
    }

    protected override string Generate()
    {
        this.GenerateHeader();
        this.GenerateNamespace();
        this.GenerateFactory();

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
        if (this.RedRootType.ContainingNamespace is null) return;
        this.contentWriter
            .Write("namespace ")
            .Write(this.RedRootType.ContainingNamespace)
            .Write(";");
    }

    private void GenerateFactory()
    {
        // Opening
        this.contentWriter
            .Write(this.FactoryType.DeclaredAccessibility)
            .Write(this.FactoryType.GetTypeKind(partial: true))
            .Write(this.FactoryType.Name)
            .Write("{");

        foreach (var type in this.GreenRootType.EnumerateContainedTypeTree())
        {
            if (!type.IsSubtypeOf(this.GreenRootType)) continue;
            this.GenerateRedFactory(type);
        }

        // Closing brace
        this.contentWriter
            .Write("}");
    }

    private void GenerateRedFactory(INamedTypeSymbol greenType)
    {
        if (greenType.IsAbstract) return;
        if (HasIgnoreFlag(greenType, IgnoreFlags.SyntaxFactoryConstruct)) return;

        var redName = this.GetFullRedClassName(greenType);
        var factoryName = this.GenerateFactoryMethodName(greenType);

        foreach (var ctor in greenType
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor))
        {
            var ctorParams = ctor.Parameters;

            // Skip copy ctor
            if (ctorParams.Length == 1 && SymbolEquals(ctorParams[0].Type, greenType)) continue;

            // Header
            this.contentWriter
                .Write(Accessibility.Public)
                .Write("static")
                .Write(redName)
                .Write(factoryName)
                .Write("(");
            // Args
            this.contentWriter.Write(string.Join(
                ", ",
                ctorParams.Select(param => $"{this.TranslareToRedType(param.Type)} {ToCamelCaseEscaped(param.Name)}")));
            this.contentWriter.Write(")");

            // Body
            // => (RedNode)ToRed(null, new GreenNode(...))
            this.contentWriter
                .Write("=>")
                .Write($"({redName})")
                .Write(this.RedRootType)
                .Write(".")
                .Write(this.ToRedMethodName)
                .Write("(null,");
            this.contentWriter
                .Write("new")
                .Write(greenType)
                .Write("(")
                .Write(string.Join(
                    ",",
                    ctorParams.Select(param => $"({param.Type.ToDisplayString()}){this.ToGreenMethodName}({ToCamelCaseEscaped(param.Name)})")))
                .Write(")");
            this.contentWriter
                .Write(");");
        }
    }

    private string GenerateFactoryMethodName(INamedTypeSymbol symbol)
    {
        // Nullable value types are unwrapped
        if (symbol.IsValueType
         && symbol.NullableAnnotation == NullableAnnotation.Annotated
         && symbol.TypeArguments[0] is INamedTypeSymbol namedTypeArg)
        {
            symbol = namedTypeArg;
        }
        // For anything not part of the tree, we just generate a NAME
        if (!this.greenTreeNodes.Contains(symbol)) return symbol.Name;
        // For anything else, we read up the names in reverse order, excluding the root
        var parts = symbol.EnumerateNestingChain().Skip(1).Reverse().Select(this.GetRedClassName);
        return string.Join("", parts);
    }

    private string TranslareToRedType(ITypeSymbol symbol)
    {
        // Not named, we don't deal with it
        if (symbol is not INamedTypeSymbol namedSymbol) return symbol.ToDisplayString();

        var isNullable = namedSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        var nullableSuffix = isNullable ? "?" : string.Empty;
        // Any nullable value type, special case
        if (namedSymbol.IsValueType && isNullable)
        {
            var subtype = this.TranslareToRedType(namedSymbol.TypeArguments[0]);
            return $"{subtype}?";
        }
        // Tuple types
        if (namedSymbol.IsTupleType)
        {
            var elements = namedSymbol
                .TupleElements
                .Select(e => $"{this.TranslareToRedType(e.Type)} {e.Name}")
                .ToList();
            return $"({string.Join(", ", elements)})";
        }
        // Generic types
        if (namedSymbol.IsGenericType)
        {
            var originalDef = namedSymbol.OriginalDefinition;
            var name = this.GetFullRedClassName(originalDef);
            var genericsStart = name.IndexOf('<');
            if (genericsStart >= 0) name = name.Substring(0, genericsStart);
            var typeArgs = namedSymbol.TypeArguments.Select(this.TranslareToRedType).ToList();
            return $"{name}<{string.Join(", ", typeArgs)}>{nullableSuffix}";
        }
        // Green subclasses
        if (namedSymbol.IsSubtypeOf(this.GreenRootType))
        {
            return $"{this.GetFullRedClassName(namedSymbol)}{nullableSuffix}";
        }
        // Anything else
        return symbol.ToDisplayString();
    }
}
