using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Semantics;
using Draco.Query;

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
    /// Retrieves the <see cref="ISymbol"/> defined by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The defined <see cref="ISymbol"/> by <paramref name="subtree"/>, or null if it does not
    /// define any.</returns>
    public async Task<ISymbol?> GetDefinedSymbol(ParseTree subtree) =>
        await SymbolResolution.GetDefinedSymbol(this.db, subtree);

    // TODO: This API swallows errors
    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by <paramref name="subtree"/>.
    /// </summary>
    /// <param name="subtree">The tree that is asked for the referenced <see cref="ISymbol"/>.</param>
    /// <returns>The referenced <see cref="ISymbol"/> by <paramref name="subtree"/>, or null if it does not
    /// reference any.</returns>
    public async Task<ISymbol?> GetReferencedSymbol(ParseTree subtree) =>
        await SymbolResolution.GetReferencedSymbol(this.db, subtree);
}
