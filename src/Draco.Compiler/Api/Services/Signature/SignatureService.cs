using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Services.Signature;

/// <summary>
/// Provides <see cref="SignatureItem"/>s.
/// </summary>
public sealed class SignatureService
{
    /// <summary>
    /// Creates a new <see cref="SignatureService"/> with default settings.
    /// </summary>
    /// <returns>A new <see cref="SignatureService"/> with default settings.</returns>
    public static SignatureService CreateDefault() => new();

    /// <summary>
    /// Gets <see cref="SignatureItem"/> for the current context.
    /// </summary>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for the tree.</param>
    /// <param name="cursorIndex">The cursors index in the tree.</param>
    /// <returns>A <see cref="SignatureItem"/> created based on the current context or null, if the context doesn't have any signature information to display.</returns>
    public SignatureItem? GetSignature(SemanticModel semanticModel, int cursorIndex)
    {
        var tree = semanticModel.Tree;
        var cursor = tree.IndexToSyntaxPosition(cursorIndex);

        // Check if this is a call expression
        var callSyntax = tree.Root
            .TraverseSubtreesAtCursorPosition(cursor)
            .OfType<CallExpressionSyntax>()
            .LastOrDefault();
        if (callSyntax is null) return null;

        // Get all overloads
        var overloads = semanticModel.GetReferencedOverloadsInternal(callSyntax.Function);
        if (overloads.Length == 0) return null;

        // Figure out the best match
        var currentOverload = this.FindBestMatch(overloads, callSyntax);

        // Get the current parameter
        var currentParameter = this.GetCurrentParameter(currentOverload, callSyntax, cursorIndex);

        return new SignatureItem(
            overloads.Select(s => s.ToApiSymbol()).ToImmutableArray(),
            currentOverload.ToApiSymbol(),
            currentParameter?.ToApiSymbol());
    }

    private Internal.Symbols.FunctionSymbol FindBestMatch(
        ImmutableArray<Internal.Symbols.FunctionSymbol> functions,
        CallExpressionSyntax callSyntax)
    {

    }

    private Internal.Symbols.ParameterSymbol? GetCurrentParameter(
        Internal.Symbols.FunctionSymbol function,
        CallExpressionSyntax callSyntax,
        int cursorIndex)
    {

    }
}
