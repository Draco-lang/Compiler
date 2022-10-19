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

    private static string GetRedClassName(INamedTypeSymbol type) => $"Red{type.Name}";

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
        this.GenerateConstructorForType(type);

        // Close braces
        foreach (var _ in type.EnumerateNestingChain()) this.writer.Write("}");
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
                .Write("{")
                .Write("this.Parent = parent;")
                .Write("this.green = green;")
                .Write("}");
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
            .Write("internal")
            .Write(redRoot)
            .Write($"ToRedNode({redRoot}? parent, {this.greenRoot.ToDisplayString()} green) => green switch")
            .Write("{");

        foreach (var greenType in this.greenRoot.EnumerateContainedTypeTree())
        {
            if (!greenType.IsSubtypeOf(this.greenRoot)) continue;
            if (greenType.IsAbstract) continue;
            var redFullName = string.Join(".", greenType.EnumerateNestingChain().Select(GetRedClassName));
            this.writer
                .Write(greenType.ToDisplayString())
                .Write("=>")
                .Write($"new {redFullName}(parent, green),");
        }

        this.writer
            .Write("_ => throw new System.ArgumentOutOfRangeException(nameof(green)),")
            .Write("};")
            .Write("}");
    }
}
