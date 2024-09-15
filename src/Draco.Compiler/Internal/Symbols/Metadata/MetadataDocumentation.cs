using System;
using System.Linq;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Utilities for reading up metadata documentation.
/// </summary>
internal static class MetadataDocumentation
{
    /// <summary>
    /// Gets the documentation XML as text for the given <paramref name="symbol"/>.
    /// </summary>
    /// <param name="symbol">The <see cref="Symbol"/> to get documentation for.</param>
    /// <returns>The documentation, or empty string, if no documentation was found.</returns>
    public static string GetDocumentation(Symbol symbol)
    {
        var assembly = symbol.AncestorChain.OfType<MetadataAssemblySymbol>().FirstOrDefault();
        if (assembly is null) return string.Empty;
        var documentationName = GetPrefixedDocumentationName(symbol);
        var root = assembly.AssemblyDocumentation?.DocumentElement;
        var xml = root?.SelectSingleNode($"//member[@name='{documentationName}']")?.InnerXml ?? string.Empty;
        return string.Join(Environment.NewLine, xml.ReplaceLineEndings("\n").Split('\n').Select(x => x.TrimStart()));
    }

    /// <summary>
    /// Gets the full name of a <paramref name="symbol"/> used to retrieve documentation from metadata.
    /// </summary>
    /// <param name="symbol">The symbol to get documentation name of.</param>
    /// <returns>The documentation name, or empty string, if <paramref name="symbol"/> is null.</returns>
    public static string GetDocumentationName(Symbol? symbol) => symbol switch
    {
        FunctionSymbol function => GetFunctionDocumentationName(function),
        TypeParameterSymbol typeParam => GetTypeParameterDocumentationName(typeParam),
        null => string.Empty,
        _ => symbol.MetadataFullName,
    };

    /// <summary>
    /// The documentation name of <paramref name="symbol"/> with prepended documentation prefix, documentation prefix specifies the type of symbol the documentation name represents.
    /// For example <see cref="TypeSymbol"/> has the prefix "T:".
    /// </summary>
    /// <param name="symbol">The symbol to get prefixed documentation name of.</param>
    /// <returns>The prefixed documentation name, or empty string, if <paramref name="symbol"/> is null.</returns>
    public static string GetPrefixedDocumentationName(Symbol? symbol) =>
        $"{GetDocumentationPrefix(symbol)}{GetDocumentationName(symbol)}";

    private static string GetDocumentationPrefix(Symbol? symbol) => symbol switch
    {
        TypeSymbol => "T:",
        ModuleSymbol => "T:",
        FunctionSymbol => "M:",
        PropertySymbol => "P:",
        FieldSymbol => "F:",
        _ => string.Empty,
    };

    private static string GetFunctionDocumentationName(FunctionSymbol function)
    {
        var parametersJoined = function.Parameters.Length == 0
            ? string.Empty
            : $"({string.Join(",", function.Parameters.Select(x => GetDocumentationName(x.Type)))})";

        var generics = function.GenericParameters.Length == 0
            ? string.Empty
            : $"``{function.GenericParameters.Length}";
        return $"{function.MetadataFullName}{generics}{parametersJoined}";
    }

    private static string GetTypeParameterDocumentationName(TypeParameterSymbol typeParameter)
    {
        var index = typeParameter.ContainingSymbol?.GenericParameters.IndexOf(typeParameter);
        if (index is null || index.Value == -1) return typeParameter.MetadataFullName;
        return typeParameter.ContainingSymbol switch
        {
            TypeSymbol => $"`{index.Value}",
            FunctionSymbol => $"``{index.Value}",
            _ => typeParameter.MetadataFullName,
        };
    }
}
