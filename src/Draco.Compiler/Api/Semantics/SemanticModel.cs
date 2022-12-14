using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Semantics.Types;

namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// The semantic model of a subtree.
/// </summary>
public sealed class SemanticModel
{
    /// <summary>
    /// The the tree that the semantic model is for.
    /// </summary>
    public ParseTree Tree { get; }

    /// <summary>
    /// The semantic <see cref="Diagnostic"/>s in this model.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => this.GetAllDiagnostics();

    internal QueryDatabase QueryDatabase => this.db;

    private readonly QueryDatabase db;

    internal SemanticModel(QueryDatabase db, ParseTree tree)
    {
        this.db = db;
        this.Tree = tree;
    }

    /// <summary>
    /// Prints this model as a scope tree in a DOT graph format.
    /// </summary>
    /// <returns>The DOT graph of the symbols and scopes of <see cref="Tree"/>.</returns>
    public string ToScopeTreeDotGraphString() =>
        ScopeTreePrinter.Print(this.db, this.Tree);

    /// <summary>
    /// Retrieves all semantic <see cref="Diagnostic"/>s.
    /// </summary>
    /// <returns>All <see cref="Diagnostic"/>s produced during semantic analysis.</returns>
    internal IEnumerable<Diagnostic> GetAllDiagnostics()
    {
        IEnumerable<Diagnostic> Impl(ParseNode tree)
        {
            // Symbol
            foreach (var diag in SymbolResolution.GetDiagnostics(this.db, tree)) yield return diag.ToApiDiagnostic(tree);

            // Type
            foreach (var diag in TypeChecker.GetDiagnostics(this.db, tree)) yield return diag.ToApiDiagnostic(tree);

            // Children
            foreach (var diag in tree.Children.SelectMany(Impl)) yield return diag;
        }

        return Impl(this.Tree.Root);
    }

    // NOTE: These OrNull functions are not too pretty
    // For now public API is not that big of a concern, so they can stay

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> defined by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The defined <see cref="ISymbol"/> by <paramref name="subtree"/>, or null if it does not
    /// define any.</returns>
    public ISymbol? GetDefinedSymbolOrNull(ParseNode subtree) =>
        SymbolResolution.GetDefinedSymbolOrNull(this.db, subtree)?.ToApiSymbol();

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="subtree"/>, or null if it does not
    /// reference any.</returns>
    public ISymbol? GetReferencedSymbolOrNull(ParseNode subtree) =>
        SymbolResolution.GetReferencedSymbolOrNull(this.db, subtree)?.ToApiSymbol();

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="subtree"/>.</returns>
    public ISymbol GetReferencedSymbol(ParseNode subtree) =>
        SymbolResolution.GetReferencedSymbol(this.db, subtree).ToApiSymbol();
}
