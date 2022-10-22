using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

/// <summary>
/// Generates the entire red tree hierarchy.
/// </summary>
public sealed class RedTreeGenerator
{
    public static string Generate(INamedTypeSymbol rootType)
    {
        var generator = new RedTreeGenerator(rootType);
        generator.GenerateClasses();
        generator.GenerateToRedMethod();
        return generator.writer.Code;
    }

    private readonly INamedTypeSymbol greenRoot;
    private readonly CodeWriter writer = new();

    private RedTreeGenerator(INamedTypeSymbol root)
    {
        this.greenRoot = root;
    }

    private static string ToCamelCase(string text) => $"{char.ToLower(text[0])}{text.Substring(1)}";
    private static string GetRedClassName(INamedTypeSymbol type) => $"Red{type.Name}";
    private static string GetFullRedClassName(INamedTypeSymbol type) =>
        string.Join(".", type.EnumerateNestingChain().Select(GetRedClassName));

    private void GenerateClasses()
    {
        foreach (var type in this.greenRoot.EnumerateContainedTypeTree())
        {
            if (!type.IsSubtypeOf(this.greenRoot)) continue;
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
                .Write(GetRedClassName(nest));
            // Inheritance
            if (SymbolEqualityComparer.Default.Equals(nest, type) && (type.BaseType?.IsSubtypeOf(this.greenRoot) ?? false))
            {
                this.writer
                    .Write(":")
                    .Write(GetRedClassName(type.BaseType));
            }
            this.writer
                .Write("{");
        }

        this.GenerateGreenPropertyForType(type);
        this.GenerateProjectedPropertiesForType(type);
        this.GenerateConstructorForType(type);

        // Close braces
        foreach (var _ in type.EnumerateNestingChain()) this.writer.Write("}");
    }

    private void GenerateProjectedPropertiesForType(INamedTypeSymbol type)
    {
        var relevantProps = type
            .GetSanitizedProperties()
            // Only care about public
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            // Overriden ones are implemented in base already
            .Where(p => !p.IsOverride);
        foreach (var prop in relevantProps)
        {
            this.GenerateProjectedProperty(prop);
        }
    }

    private void GenerateProjectedProperty(IPropertySymbol prop)
    {
        var (redType, hasGreen) = this.TranslareToRedType(prop.Type);
        if (hasGreen)
        {
            // We need a cache property
            var cachedRedType = redType.EndsWith("?") ? redType : $"{redType}?";
            var cachedRedName = RoslynUtils.EscapeKeyword(ToCamelCase(prop.Name));
            this.writer
                .Write("private")
                .Write(cachedRedType)
                .Write(cachedRedName)
                .Write(";");

            // Write the cached projection
            var accessorSuffix = prop.Type.IsValueType ? ".Value" : string.Empty;
            this.writer
                .Write(prop.DeclaredAccessibility)
                .Write(redType)
                .Write(prop.Name)
                .Write("=>")
                .Write($"this.{cachedRedName} ??= ({redType})ToRedNode(this, this.Green.{prop.Name});");
        }
        else
        {
            // Just map one-to-one from the green node
            this.writer
                .Write(prop.DeclaredAccessibility)
                .Write(redType)
                .Write(prop.Name)
                .Write("=>")
                .Write($"this.Green.{prop.Name};");
        }
    }

    private void GenerateGreenPropertyForType(INamedTypeSymbol type)
    {
        this.writer
            .Write("internal");
        if (!SymbolEqualityComparer.Default.Equals(this.greenRoot, type)) this.writer.Write("new");
        this.writer
            .Write(type.ToDisplayString())
            .Write($"Green => ({type.ToDisplayString()})this.green;");
    }

    private void GenerateConstructorForType(INamedTypeSymbol type)
    {
        this.writer
            .Write("internal")
            .Write(GetRedClassName(type))
            .Write($"({GetRedClassName(this.greenRoot)}? parent, {this.greenRoot.ToDisplayString()} green)");
        if (SymbolEqualityComparer.Default.Equals(type, this.greenRoot))
        {
            this.writer
                .Write("""
                {
                    this.Parent = parent;
                    this.green = green;
                }
                """);
        }
        else
        {
            this.writer
                .Write(": base(parent, green) {}");
        }
    }

    private void GenerateToRedMethod()
    {
        var redRoot = GetRedClassName(this.greenRoot);

        this.writer
            .Write(this.greenRoot.DeclaredAccessibility)
            .Write(this.greenRoot.GetTypeKind(partial: true))
            .Write(redRoot)
            .Write("{");

        this.writer
            .Write("[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(green))]")
            .Write("internal")
            .Write($"{redRoot}?")
            .Write($"ToRedNode({redRoot}? parent, {this.greenRoot.ToDisplayString()}? green) => green switch")
            .Write("{");

        foreach (var greenType in this.greenRoot.EnumerateContainedTypeTree())
        {
            if (!greenType.IsSubtypeOf(this.greenRoot)) continue;
            if (greenType.IsAbstract) continue;
            this.writer
                .Write(greenType.ToDisplayString())
                .Write("=>")
                .Write($"new {GetFullRedClassName(greenType)}(parent, green),");
        }

        this.writer
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
            return ($"{nameRoot}<{string.Join(", ", typeArgs.Select(e => e.Text))}>{nullableSuffix}", typeArgs.Any(e => e.HasGreen));
        }
        // Green subclasses
        if (namedSymbol.IsSubtypeOf(this.greenRoot)) return ($"{GetFullRedClassName(namedSymbol)}{nullableSuffix}", true);
        // Anything else
        return (symbol.ToDisplayString(), false);
    }
}
