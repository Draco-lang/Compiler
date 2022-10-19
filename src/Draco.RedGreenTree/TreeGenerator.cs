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
        generator.GenerateMappingFunction();
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

    private string ToFullRedClassName(INamedTypeSymbol greenNode)
    {
        if (greenNode.BaseType is not null && IsBaseOf(this.root, greenNode.BaseType))
        {
            return $"{this.ToFullRedClassName(greenNode.BaseType)}.{this.ToRedClassName(greenNode)}";
        }
        return this.ToRedClassName(greenNode);
    }

    private IEnumerable<INamedTypeSymbol> EnumerateAllNestedTypes(INamedTypeSymbol symbol)
    {
        yield return symbol;
        foreach (var item in symbol.GetTypeMembers())
        {
            if (!IsBaseOf(this.root, item)) continue;
            foreach (var subElement in this.EnumerateAllNestedTypes(item)) yield return subElement;
        }
    }

    public void GenerateMappingFunction()
    {
        this.writer.Write(this.settings.RedAccessibility);
        if (this.root.IsAbstract) this.writer.Separate().Write("abstract");
        if (this.root.IsSealed) this.writer.Separate().Write("sealed");
        if (this.root.IsReadOnly) this.writer.Separate().Write("readonly");
        this.writer.Separate().Write("partial");
        if (this.root.IsRecord) this.writer.Separate().Write("record");
        this.writer.Separate().Write(this.root.IsValueType ? "struct" : "class");
        this.writer.Separate().Write(this.ToRedClassName(this.root));

        this.writer.OpenBrace();

        this.writer
            .Write("internal").Separate()
            .Write("static").Separate()
            .Write(this.ToRedClassName(this.root)).Separate()
            .Write(this.settings.ToRedMethodName)
            .Write('(')
            .Write(this.ToRedClassName(this.root))
            .Write(' ')
            .Write("parent")
            .Write(", ")
            .Write(this.root.ToDisplayString())
            .Write(' ')
            .Write("green")
            .WriteLine(")")
            .OpenBrace();

        this.writer
            .WriteLine("switch (node)")
            .OpenBrace();

        foreach (var type in this.EnumerateAllNestedTypes(this.root))
        {
            if (type.IsAbstract) continue;
            this.writer.WriteLine($"case {type.ToDisplayString()} sub:");
            this.writer.OpenBrace();
            this.writer.WriteLine($"return new {this.ToFullRedClassName(type)}(parent, green);");
            this.writer.CloseBrace();
        }

        this.writer.Write($"default:");
        this.writer.OpenBrace();
        this.writer.WriteLine("throw new System.InvalidOperationException();");
        this.writer.CloseBrace();

        this.writer.CloseBrace();
        this.writer.CloseBrace();
        this.writer.CloseBrace();
    }

    public void GenerateClass(INamedTypeSymbol greenType)
    {
        // Check if part of the tree
        if (!IsBaseOf(this.root, greenType)) return;

        var isRoot = SymbolEqualityComparer.Default.Equals(this.root, greenType);

        // Class header
        this.writer.Write(this.settings.RedAccessibility);
        if (HidesInherited(greenType)) this.writer.Separate().Write("new");
        if (greenType.IsAbstract) this.writer.Separate().Write("abstract");
        if (greenType.IsSealed) this.writer.Separate().Write("sealed");
        if (greenType.IsReadOnly) this.writer.Separate().Write("readonly");
        this.writer.Separate().Write("partial");
        if (greenType.IsRecord) this.writer.Separate().Write("record");
        this.writer.Separate().Write(greenType.IsValueType ? "struct" : "class");
        this.writer.Separate().Write(this.ToRedClassName(greenType));

        if (greenType.BaseType is not null && IsBaseOf(this.root, greenType.BaseType))
        {
            this.writer.Write(" : ").Write(this.ToRedClassName(greenType.BaseType));
        }

        this.writer.OpenBrace();

        // Data members
        // First off, the parent node and a green node, in case this is the root
        if (isRoot)
        {
            // Parent
            this.writer
                .WriteDocs("""
                <summary>
                The parent of this tree node.
                </summary>
                """);
            this.writer
                .Write("public").Separate()
                .Write(this.ToFullRedClassName(this.root)).Write('?').Separate()
                .Write(this.settings.ParentName)
                .WriteLine(" { get; }");
            // Green node
            this.writer
                .Write("private").Separate()
                .Write("readonly").Separate()
                .Write(this.root).Separate()
                .Write(this.settings.GreenName)
                .WriteLine(";");
        }
        // Other members
        foreach (var member in greenType.GetMembers())
        {
            // We only consider public properties
            if (member is not IPropertySymbol prop) continue;
            if (prop.DeclaredAccessibility != Accessibility.Public) continue;
            // Already implemented in base
            if (prop.IsOverride) continue;
            // Record-stuff
            if (greenType.IsRecord && prop.Name == "EqualityContract") continue;
            // Cached field
            var (redType, hasGreen) = this.TranslareToRedType(prop.Type);
            if (hasGreen)
            {
                this.writer.Write("private").Separate();
                this.writer.Write(redType);
                if (!redType.EndsWith("?")) this.writer.Write('?');
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
                    .WriteLine(")!;");
                this.writer.CloseBrace();
                this.writer.Write($"return this.{UnCapitalize(prop.Name)}");
                if (prop.Type.IsValueType) this.writer.Write(".Value");
                this.writer.WriteLine(";");
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
                    .WriteLine($"(({greenType.ToDisplayString()})this.{this.settings.GreenName}).{prop.Name};");
            }
        }

        // Constructor
        this.writer
            .Write("public").Separate()
            .Write(this.ToRedClassName(greenType)).Write('(')
            // Parent
            .Write(this.ToRedClassName(this.root)).Write('?')
            .Write($" {UnCapitalize(this.settings.ParentName)}, ")
            // Green
            .Write(this.root).Write(' ').Write(this.settings.GreenName)
            .Write(')');
        if (isRoot)
        {
            this.writer.OpenBrace();
            this.writer
                .WriteLine($"this.{this.settings.ParentName} = {UnCapitalize(this.settings.ParentName)};")
                .WriteLine($"this.{this.settings.GreenName} = {this.settings.GreenName};");
            this.writer.CloseBrace();
        }
        else
        {
            // Call to base
            this.writer
                .Write($" : base({UnCapitalize(this.settings.ParentName)}, {this.settings.GreenName})");
            this.writer.OpenBrace().CloseBrace();
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
        if (IsBaseOf(this.root, namedSymbol)) return (this.ToFullRedClassName(namedSymbol), true);
        // Anything else
        return (symbol.ToDisplayString(), false);
    }

    private static string UnCapitalize(string name)
    {
        var result = $"{char.ToLower(name[0])}{name.Substring(1)}";
        return EscapeKeyword(result);
    }

    private static readonly string[] keywords = new[]
    {
        "else", "object", "params"
    };

    private static string EscapeKeyword(string name)
    {
        if (keywords.Contains(name)) return $"@{name}";
        return name;
    }

    private static bool HidesInherited(ITypeSymbol what)
    {
        bool Impl(ITypeSymbol? context)
        {
            if (context is null) return false;
            if (context.ToDisplayString() == "object") return false;
            if (context.Name == what.Name) return true;
            if (context.GetMembers(what.Name).Any(s => !SymbolEqualityComparer.Default.Equals(s, what))) return true;
            return Impl(context.BaseType);
        }
        return Impl(what.BaseType);
    }

    private static bool IsBaseOf(INamedTypeSymbol? @base, INamedTypeSymbol derived)
    {
        if (@base is null) return false;
        if (SymbolEqualityComparer.Default.Equals(@base, derived)) return true;
        if (derived.BaseType is null) return false;
        return IsBaseOf(@base, derived.BaseType);
    }
}
