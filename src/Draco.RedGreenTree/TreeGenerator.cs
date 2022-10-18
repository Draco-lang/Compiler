using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

public sealed class TreeGenerator
{
    public static string Generate(TreeGeneratorSettings settings, INamedTypeSymbol rootType)
    {
        var generator = new TreeGenerator(settings, rootType);
        generator.GenerateClass(rootType);
        return generator.writer.Code;
    }

    private readonly TreeGeneratorSettings settings;
    private readonly INamedTypeSymbol root;
    private readonly CodeWriter writer = new();

    private TreeGenerator(TreeGeneratorSettings settings, INamedTypeSymbol root)
    {
        this.settings = settings;
        this.root = root;
    }

    private string ToRedClassName(INamedTypeSymbol greenNode) =>
        this.settings.GreenToRedName(greenNode.Name);

    public void GenerateClass(INamedTypeSymbol greenType)
    {
        // Check if part of the tree
        if (!IsBaseOf(this.root, greenType)) return;

        // Class header
        this.writer.Write(this.settings.RedAccessibility);
        if (greenType.IsAbstract) this.writer.Separate().Write("abstract");
        if (greenType.IsSealed) this.writer.Separate().Write("sealed");
        if (greenType.IsReadOnly) this.writer.Separate().Write("readonly");
        if (this.settings.RedIsPartial) this.writer.Separate().Write("partial");
        if (greenType.IsRecord) this.writer.Separate().Write("record");
        this.writer.Separate().Write(greenType.IsValueType ? "struct" : "class");
        this.writer.Separate().Write(this.ToRedClassName(greenType));

        if (greenType.BaseType is not null && IsBaseOf(this.root, greenType.BaseType))
        {
            this.writer.Write(" : ").Write(this.ToRedClassName(greenType.BaseType));
        }

        this.writer.OpenBrace();

#if false
        // Data members
        // First off, the parent node and a green node, in case this is the root
        if (SymbolEqualityComparer.Default.Equals(this.root, greenType))
        {
            // Parent
            this.Indent();
            this.builder
                .Append("public ")
                .Append(this.settings.GreenToRedName(this.root.Name))
                .Append("? ")
                .Append(this.settings.ParentName)
                .AppendLine(" { get; }");
            // Green node
            this.Indent();
            this.builder
                .Append("private readonly ")
                .Append(this.root.ToDisplayString())
                .Append(' ')
                .Append(this.settings.GreenName)
                .AppendLine(";");
        }
#endif
        // Other members
        foreach (var member in greenType.GetMembers())
        {
            // We only consider properties
            if (member is not IPropertySymbol prop) continue;
            // Record-stuff
            if (greenType.IsRecord && member.Name == "EqualityContract") continue;
            // Cached field
            var (redType, hasGreen) = this.TranslareToRedType(prop.Type);
            if (hasGreen)
            {
                this.writer
                    .Write("private").Separate()
                    .Write(redType);
                if (prop.Type.NullableAnnotation != NullableAnnotation.Annotated) this.writer.Write('?');
                this.writer.Separate()
                    .Write(UnCapitalize(prop.Name)).WriteLine(";");
            }
            // Write the property
            if (hasGreen)
            {
                // Has to be projected, write the caching logic
                this.writer
                    .Write("public").Separate()
                    .Write(redType).Separate()
                    .Write(prop.Name).WriteLine()
                    .OpenBrace()
                    .WriteLine("get")
                    .OpenBrace();
                this.writer
                    .Write($"if (this.{UnCapitalize(prop.Name)} is null)")
                    .OpenBrace();
                this.writer
                    .Write($"this.{UnCapitalize(prop.Name)} = ({redType})")
                    .Write(this.settings.ToRedMethodName)
                    .Write('(')
                    .Write("this, ")
                    .Write($"(({greenType.ToDisplayString()}){this.settings.GreenName}).{prop.Name}")
                    .WriteLine(");");
                this.writer.CloseBrace();
                this.writer.WriteLine($"return this.{UnCapitalize(prop.Name)};");
                this.writer
                    .CloseBrace()
                    .CloseBrace();
            }
            else
            {
                // Just access the property from the green node
                this.writer
                    .Write("public").Separate()
                    .Write(redType).Separate()
                    .Write(prop.Name).Separate()
                    .Write("=>").Separate()
                    .WriteLine($"this.{this.settings.GreenName}.{prop.Name};");
            }
        }

        // Nested types
        foreach (var nestedClass in greenType.GetTypeMembers()) this.GenerateClass(nestedClass);

        this.writer.CloseBrace();
    }

    private (string Text, bool HasGreen) TranslareToRedType(ITypeSymbol symbol)
    {
        // Not named, we don't deal with it
        if (symbol is not INamedTypeSymbol namedSymbol) return (symbol.ToDisplayString(), false);
        // Any nullable value type, special case
        if (namedSymbol.IsValueType && namedSymbol.NullableAnnotation == NullableAnnotation.Annotated)
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
            return ($"{nameRoot}<{string.Join(", ", typeArgs.Select(e => e.Text))}>", typeArgs.Any(e => e.HasGreen));
        }
        // Green subclasses
        if (IsBaseOf(this.root, namedSymbol)) return (this.ToRedClassName(namedSymbol), true);
        // Anything else
        return (symbol.ToDisplayString(), false);
    }

    private static string UnCapitalize(string name) =>
        $"{char.ToLower(name[0])}{name.Substring(1)}";

    private static bool IsBaseOf(INamedTypeSymbol? @base, INamedTypeSymbol derived)
    {
        if (@base is null) return false;
        if (SymbolEqualityComparer.Default.Equals(@base, derived)) return true;
        if (derived.BaseType is null) return false;
        return IsBaseOf(@base, derived.BaseType);
    }
}
