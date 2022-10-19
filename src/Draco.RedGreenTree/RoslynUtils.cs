using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

internal static class RoslynUtils
{
    public static bool IsSubtypeOf(this INamedTypeSymbol derived, INamedTypeSymbol? @base)
    {
        if (@base is null) return false;
        if (SymbolEqualityComparer.Default.Equals(@base, derived)) return true;
        if (derived.BaseType is null) return false;
        return derived.BaseType.IsSubtypeOf(@base);
    }

    public static bool HidesInherited(this ITypeSymbol symbol)
    {
        bool Impl(ITypeSymbol? context)
        {
            if (context is null) return false;
            if (context.ToDisplayString() == "object") return false;
            if (context.Name == symbol.Name) return true;
            if (context.GetMembers(symbol.Name).Any(s => !SymbolEqualityComparer.Default.Equals(s, symbol))) return true;
            return Impl(context.BaseType);
        }
        return Impl(symbol.BaseType);
    }

    private static readonly string[] keywords = new[]
    {
        "if", "else", "while", "for", "foreach",
        "params", "ref", "out",
        "object",
    };
    public static string EscapeKeyword(string name)
    {
        if (keywords.Contains(name)) return $"@{name}";
        return name;
    }

    public static IEnumerable<INamedTypeSymbol> EnumerateContainedTypeTree(this INamedTypeSymbol symbol)
    {
        yield return symbol;
        foreach (var item in symbol.GetTypeMembers().SelectMany(s => s.EnumerateContainedTypeTree())) yield return item;
    }

    public static bool HasMember(this ITypeSymbol type, string name) => type.GetMembers(name).Length > 0;

    public static string GetTypeKind(this ITypeSymbol type, bool partial = false)
    {
        var result = new StringBuilder();
        if (type.IsAbstract) result.Append("abstract ");
        if (type.IsSealed) result.Append("sealed ");
        if (type.IsReadOnly) result.Append("readonly ");
        if (partial) result.Append("partial ");
        if (type.IsRecord) result.Append("record ");
        result.Append(type.IsValueType ? "struct" : "class");
        return result.ToString();
    }

    public static IEnumerable<INamedTypeSymbol> EnumerateNestingChain(this INamedTypeSymbol symbol)
    {
        if (symbol.ContainingType is not null)
        {
            foreach (var item in symbol.ContainingType.EnumerateNestingChain()) yield return item;
        }
        yield return symbol;
    }

    public static IEnumerable<IPropertySymbol> GetSanitizedProperties(this INamedTypeSymbol symbol) => symbol
        .GetMembers()
        .OfType<IPropertySymbol>()
        .Where(p => p.Name != "EqualityContract");
}
