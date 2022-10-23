using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Generates the entire red tree hierarchy.
/// </summary>
public sealed class RedTreeGenerator : GeneratorBase
{
    public sealed class Settings
    {
        public INamedTypeSymbol GreenRootType { get; set; }
        public INamedTypeSymbol RedRootType { get; set; }
        public bool GenerateGreenProperty { get; set; } = true;
        public bool GenerateProjectedProperties { get; set; } = true;
        public bool GenerateConstructor { get; set; } = true;
        public bool GenerateToRedMethod { get; set; } = true;
        public Func<INamedTypeSymbol, string> GetRedName { get; set; } = x => x.Name;
        public string GreenPropertyName { get; set; } = "Green";
        public string ParentPropertyName { get; set; } = "Parent";
        public string ToRedMethodName { get; set; } = "ToRed";

        public Settings(INamedTypeSymbol greenRootType, INamedTypeSymbol redRootType)
        {
            this.GreenRootType = greenRootType;
            this.RedRootType = redRootType;
        }
    }

    public static string Generate(Settings settings)
    {
        var generator = new RedTreeGenerator(settings);
        return generator.Generate();
    }

    private readonly Settings settings;
    private readonly CodeWriter headerWriter = new();
    private readonly CodeWriter contentWriter = new();

    private INamedTypeSymbol GreenRootType => this.settings.GreenRootType;
    private INamedTypeSymbol RedRootType => this.settings.RedRootType;
    private bool DoGenerateGreenProperty => this.settings.GenerateGreenProperty;
    private bool DoGenerateProjectedProperties => this.settings.GenerateProjectedProperties;
    private bool DoGenerateConstructor => this.settings.GenerateConstructor;
    private bool DoGenerateToRedMethod => this.settings.GenerateToRedMethod;
    private string GreenPropertyName => this.settings.GreenPropertyName;
    private string GreenFieldName => ToCamelCase(this.GreenPropertyName);
    private string ParentPropertyName => this.settings.ParentPropertyName;
    private string ToRedMethodName => this.settings.ToRedMethodName;
    private string GetRedClassName(INamedTypeSymbol type) => SymbolEquals(type, this.GreenRootType)
        ? this.RedRootType.Name
        : this.settings.GetRedName(type);
    private string GetFullRedClassName(INamedTypeSymbol type)
    {
        var typeName = string.Join(".", type.EnumerateNestingChain().Select(this.GetRedClassName));
        if (this.RedRootType.ContainingNamespace is null) return typeName;
        return $"{this.RedRootType.ContainingNamespace.ToDisplayString()}.{typeName}";
    }

    private RedTreeGenerator(Settings settings)
    {
        this.settings = settings;
    }

    protected override string Generate()
    {
        this.GenerateHeader();
        this.GenerateNamespace();
        this.GenerateTree();
        if (this.DoGenerateToRedMethod) this.GenerateToRedMethod();

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

    private void GenerateTree()
    {
        foreach (var type in this.GreenRootType.EnumerateContainedTypeTree())
        {
            if (!type.IsSubtypeOf(this.GreenRootType)) continue;
            this.GenerateRedNode(type);
        }
    }

    private void GenerateRedNode(INamedTypeSymbol greenType)
    {
        // Wrapping types
        foreach (var nest in greenType.EnumerateNestingChain())
        {
            if (SymbolEquals(nest, greenType)) this.contentWriter.Write(this.GeneratedAttribute);
            this.contentWriter
                .Write(this.RedRootType.DeclaredAccessibility)
                .Write(nest.GetTypeKind(partial: true))
                .Write(this.GetRedClassName(nest));
            // Inheritance
            if (SymbolEquals(nest, greenType) && (greenType.BaseType?.IsSubtypeOf(this.GreenRootType) ?? false))
            {
                this.contentWriter
                    .Write(":")
                    .Write(this.GetRedClassName(greenType.BaseType));
            }
            this.contentWriter.Write("{");
        }

        if (this.DoGenerateGreenProperty) this.GenerateGreenProperty(greenType);
        if (this.DoGenerateProjectedProperties) this.GenerateProjectedProperties(greenType);
        if (this.DoGenerateConstructor) this.GenerateConstructor(greenType);

        // Close braces
        foreach (var _ in greenType.EnumerateNestingChain()) this.contentWriter.Write("}");
    }

    private void GenerateGreenProperty(INamedTypeSymbol greenType)
    {
        this.contentWriter.Write(this.GreenRootType.DeclaredAccessibility);
        if (!SymbolEquals(this.GreenRootType, greenType)) this.contentWriter.Write("new");
        this.contentWriter
            .Write(greenType.ToDisplayString())
            .Write($"{this.GreenPropertyName} => ({greenType.ToDisplayString()})this.{this.GreenFieldName};");
    }

    private void GenerateProjectedProperties(INamedTypeSymbol greenType)
    {
        var relevantProps = greenType
            .GetSanitizedProperties()
            // Only care about public
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            // Overriden ones are implemented in base already
            .Where(p => !p.IsOverride);
        foreach (var prop in relevantProps) this.GenerateProjectedProperty(prop);
    }

    private void GenerateProjectedProperty(IPropertySymbol prop)
    {
        var (redType, hasGreen) = this.TranslareToRedType(prop.Type);
        if (hasGreen)
        {
            // We need a cache property
            var cachedRedType = redType.EndsWith("?") ? redType : $"{redType}?";
            var cachedRedName = RoslynUtils.EscapeKeyword(ToCamelCase(prop.Name));
            this.contentWriter
                .Write("private")
                .Write(cachedRedType)
                .Write(cachedRedName)
                .Write(";");

            // Write the cached projection
            this.contentWriter
                .Write(prop.DeclaredAccessibility)
                .Write(redType)
                .Write(prop.Name)
                .Write("=>")
                .Write($"this.{cachedRedName}")
                .Write("??=")
                .Write($"({redType}){this.ToRedMethodName}(this, this.{this.GreenPropertyName}.{prop.Name});");
        }
        else
        {
            // Just map one-to-one from the green node
            this.contentWriter
                .Write(prop.DeclaredAccessibility)
                .Write(redType)
                .Write(prop.Name)
                .Write("=>")
                .Write($"this.{this.GreenPropertyName}.{prop.Name};");
        }
    }

    private void GenerateConstructor(INamedTypeSymbol greenType)
    {
        this.contentWriter
            .Write("internal")
            .Write(this.GetRedClassName(greenType))
            .Write("(")
            .Write($"{this.GetRedClassName(this.GreenRootType)}? parent")
            .Write(", ")
            .Write($"{this.GreenRootType.ToDisplayString()} green")
            .Write(")");
        if (SymbolEquals(greenType, this.GreenRootType))
        {
            this.contentWriter
                .Write($$"""
                {
                    this.{{this.ParentPropertyName}} = parent;
                    this.{{this.GreenFieldName}} = green;
                }
                """);
        }
        else
        {
            this.contentWriter.Write(": base(parent, green) {}");
        }
    }

    private void GenerateToRedMethod()
    {
        var redRoot = this.GetRedClassName(this.GreenRootType);

        this.contentWriter
            .Write(this.GreenRootType.DeclaredAccessibility)
            .Write(this.GreenRootType.GetTypeKind(partial: true))
            .Write(redRoot)
            .Write("{");

        this.contentWriter
            .Write("[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(green))]")
            .Write("internal static")
            .Write($"{redRoot}?")
            .Write(this.ToRedMethodName)
            .Write("(")
            .Write($"{redRoot}? parent")
            .Write(", ")
            .Write($"{this.GreenRootType.ToDisplayString()}? green")
            .Write(") => green switch")
            .Write("{");

        foreach (var greenType in this.GreenRootType.EnumerateContainedTypeTree())
        {
            if (!greenType.IsSubtypeOf(this.GreenRootType)) continue;
            if (greenType.IsAbstract) continue;
            this.contentWriter
                .Write(greenType.ToDisplayString())
                .Write("=>")
                .Write($"new {this.GetFullRedClassName(greenType)}(parent, green),");
        }

        this.contentWriter
            .Write("null => null,")
            .Write("_ => throw new System.ArgumentOutOfRangeException(nameof(green)),")
            .Write("};")
            .Write("}");
    }

    private (string Text, bool HasGreen) TranslareToRedType(ITypeSymbol symbol)
    {
        // Not named, we don't deal with it
        if (symbol is not INamedTypeSymbol namedSymbol) return (symbol.ToDisplayString(), false);

        var isNullable = namedSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        var nullableSuffix = isNullable ? "?" : string.Empty;
        // Any nullable value type, special case
        if (namedSymbol.IsValueType && isNullable)
        {
            var (subtype, hasGreen) = this.TranslareToRedType(namedSymbol.TypeArguments[0]);
            return ($"{subtype}?", hasGreen);
        }
        // Tuple types
        if (namedSymbol.IsTupleType)
        {
            var elements = namedSymbol
                .TupleElements
                .Select(e =>
                {
                    var (type, hasGreen) = this.TranslareToRedType(e.Type);
                    return (Text: $"{type} {e.Name}", HasGreen: hasGreen);
                }).ToList();
            return ($"({string.Join(", ", elements.Select(e => e.Text))})", elements.Any(e => e.HasGreen));
        }
        // Generic types
        if (namedSymbol.IsGenericType)
        {
            var name = symbol.ToDisplayString();
            var nameRoot = name.Substring(0, name.IndexOf('<'));
            var typeArgs = namedSymbol.TypeArguments.Select(this.TranslareToRedType).ToList();
            return (
                $"{nameRoot}<{string.Join(", ", typeArgs.Select(e => e.Text))}>{nullableSuffix}",
                typeArgs.Any(e => e.HasGreen)
            );
        }
        // Green subclasses
        if (namedSymbol.IsSubtypeOf(this.GreenRootType))
        {
            return ($"{this.GetFullRedClassName(namedSymbol)}{nullableSuffix}", true);
        }
        // Anything else
        return (symbol.ToDisplayString(), false);
    }
}
