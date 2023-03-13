using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Utilities for binder logic.
/// </summary>
internal static class BinderFacts
{
    /// <summary>
    /// Checks, if a given symbol can be referenced in a value-context.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True, if <paramref name="symbol"/> can be referenced in a value-context.</returns>
    public static bool IsValueSymbol(Symbol symbol) => symbol
        is LocalSymbol
        or GlobalSymbol
        or FunctionSymbol;

    /// <summary>
    /// Checks, if a given symbol can be referenced in a type-context.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True, if <paramref name="symbol"/> can be referenced in a type-context.</returns>
    public static bool IsTypeSymbol(Symbol symbol) => symbol
        is TypeSymbol;

    /// <summary>
    /// Retrieves the first scope defining ancestor of a given syntax node.
    /// </summary>
    /// <param name="node">The node to get the scope defining ancestor of.</param>
    /// <returns>The first scope defining ancestor of <paramref name="node"/>, or null, if there is no such ancestor.</returns>
    public static SyntaxNode? GetScopeDefiningAncestor(SyntaxNode? node)
    {
        if (node is null) return null;
        var result = node;
        while (!DefinesScope(result))
        {
            result = result.Parent;
            if (result is null) return null;
        }
        return result;
    }

    /// <summary>
    /// Checks, if a given syntax node is responsible for defining a scope.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <returns>True, if <paramref name="node"/> defines its own scope.</returns>
    public static bool DefinesScope(SyntaxNode node) => node
        is CompilationUnitSyntax
        or FunctionDeclarationSyntax
        or FunctionBodySyntax
        or BlockExpressionSyntax;

    /// <summary>
    /// Checks, if a given syntax node references a symbol.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <returns>True, if <paramref name="node"/> references a symbol.</returns>
    public static bool ReferencesSymbol(SyntaxNode node) => node
        is NameExpressionSyntax
        or NameTypeSyntax;
}
