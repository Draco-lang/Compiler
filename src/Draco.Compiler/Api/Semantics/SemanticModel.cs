using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Semantics;
using Draco.Query;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// The semantic model of a subtree.
/// </summary>
public sealed class SemanticModel
{
    /// <summary>
    /// The root of the tree that the semantic model is for.
    /// </summary>
    public ParseTree Root { get; }

    private readonly QueryDatabase db;

    internal SemanticModel(QueryDatabase db, ParseTree root)
    {
        this.db = db;
        this.Root = root;
    }

    /// <summary>
    /// Prints this model as a scope tree in a DOT graph format.
    /// </summary>
    /// <returns>The DOT graph of the symbols and scopes of <see cref="Root"/>.</returns>
    public string ToScopeTreeDotGraphString() =>
        ScopeTreePrinter.Print(this.db, this.Root);

    /// <summary>
    /// Retrieves all semantic <see cref="Diagnostic"/>s.
    /// </summary>
    /// <returns>All <see cref="Diagnostic"/>s produced during semantic analysis.</returns>
    public IEnumerable<Diagnostic> GetAllDiagnostics()
    {
        IEnumerable<Diagnostic> Impl(ParseTree tree)
        {
            if (SymbolResolution.ReferencesSymbol(tree))
            {
                // TODO: .Result
                var sym = SymbolResolution.GetReferencedSymbol(this.db, tree).Result;
                foreach (var diag in (sym as ISymbol).Diagnostics) yield return diag;
            }

            // Children
            foreach (var diag in tree.Children.SelectMany(Impl)) yield return diag;
        }

        return Impl(this.Root);
    }

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> defined by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The defined <see cref="ISymbol"/> by <paramref name="subtree"/>, or null if it does not
    /// define any.</returns>
    public async Task<ISymbol?> GetDefinedSymbolOrNull(ParseTree subtree) =>
        await SymbolResolution.GetDefinedSymbolOrNull(this.db, subtree);

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="subtree"/>, or null if it does not
    /// reference any.</returns>
    public async Task<ISymbol?> GetReferencedSymbolOrNull(ParseTree subtree) =>
        await SymbolResolution.GetReferencedSymbolOrNull(this.db, subtree);

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="subtree"/>.</returns>
    public async Task<ISymbol> GetReferencedSymbol(ParseTree subtree) =>
        await SymbolResolution.GetReferencedSymbol(this.db, subtree);
}
