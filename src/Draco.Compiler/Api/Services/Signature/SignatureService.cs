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
        var call = tree.Root
            .TraverseSubtreesAtCursorPosition(cursor)
            .OfType<CallExpressionSyntax>()
            .LastOrDefault();
        if (call is null) return null;

        // Get all overloads
        var symbols = semanticModel
            .GetReferencedOverloads(call.Function)
            .Cast<IFunctionSymbol>()
            .OrderBy(x => x.Parameters.Length)
            .ToImmutableArray();
        if (symbols.Length == 0) return null;
        // Figure out which param should be active
        var paramCount = call.ArgumentList.Values.Count();
        var separatorCount = call.ArgumentList.Separators.Count();
        var activeParam = separatorCount == paramCount - 1 ? paramCount - 1 : paramCount;

        // Select the best overload to show as default in the signature
        // TODO: Improve this, this is really primitive
        var currentOverload = symbols.FirstOrDefault(x => x.Parameters.Length == paramCount && (separatorCount == paramCount - 1 || paramCount == 0));
        currentOverload ??= symbols.FirstOrDefault(x => x.Parameters.Length > paramCount);
        currentOverload ??= symbols.First();
        IParameterSymbol? currentParameter = null;
        if (currentOverload.Parameters.Length != 0)
        {
            currentParameter = currentOverload.Parameters.Length > activeParam
                ? currentOverload.Parameters[activeParam]
                : currentOverload.Parameters[^1];
        }
        // Return all the overloads
        return new SignatureItem(symbols, currentOverload, currentParameter);
    }
}
