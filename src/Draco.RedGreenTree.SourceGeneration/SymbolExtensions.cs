using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGeneration;

internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat displayFormat =
        new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static string GetNamespacedName(this INamedTypeSymbol type) =>
        type.ToDisplayString(displayFormat);
}
