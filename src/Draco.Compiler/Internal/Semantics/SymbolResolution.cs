using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Query;
using Draco.Query.Tasks;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Implements lazy symbol resolution.
/// </summary>
internal static class SymbolResolution
{
    /// <summary>
    /// Retrieves the containing <see cref="Scope"/> of a <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that we need the surrounding <see cref="Scope"/> of.</param>
    /// <returns>The surrounding <see cref="Scope"/> of <paramref name="tree"/>.</returns>
    public static async Task<Scope?> GetContainingScope(QueryDatabase db, ParseTree tree)
    {
        while (true)
        {
            if (tree.Parent is null) return null;
            tree = tree.Parent;

            var scope = await GetDefinedScope(db, tree);
            if (scope is not null) return scope;
        }
    }

    /// <summary>
    /// Retrieves the <see cref="Scope"/> that is introduced by a <see cref="ParseTree"/> node.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to retrieve the <see cref="Scope"/> for.</param>
    /// <returns>The <see cref="Scope"/> associated with <paramref name="tree"/>, or null
    /// if it does not define a scope.</returns>
    public static async QueryValueTask<Scope?> GetDefinedScope(QueryDatabase db, ParseTree tree)
    {
        // First get the kind of scope this tree can define
        // If the kind is null, this node simply does not define a scope
        var scopeKind = GetScopeKind(tree);
        if (scopeKind is null) return null;

        var result = new List<Declaration>();
        var position = 0;

        async Task Impl(ParseTree tree)
        {
            // We go through each child of the current tree
            foreach (var child in tree.Children)
            {
                var symbol = await GetDefinedSymbol(db, child);

                // See if the child defines any symbol
                if (symbol is not null)
                {
                    // Yes, calculate position and add it
                    // If we don't allow recursive binding for the symbol, we simply shift position
                    var symbolPosition = symbol.AllowsRecursiveBinding
                        ? position
                        : position + 1;
                    result!.Add(new(symbolPosition, symbol));
                }

                // If the child does not define its own scope, we can recursively collect symbols
                // If it does define its own scope, we assume they are contained within that scope
                if (GetScopeKind(child) is null) await Impl(child);

                ++position;
            }
        }

        await Impl(tree);

        // Construct the scope
        return new Scope(
            Kind: scopeKind.Value,
            Timelines: result
                .GroupBy(d => d.Name)
                .ToImmutableDictionary(g => g.Key, g => new DeclarationTimeline(g)));
    }

    /// <summary>
    /// Retrieves the <see cref="Symbol"/> defined by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that is asked if it defines a <see cref="Symbol"/>.</param>
    /// <returns>The <see cref="Symbol"/> that <paramref name="tree"/> defines, or null if
    /// it does not define any symbol.</returns>
    public static async QueryValueTask<Symbol?> GetDefinedSymbol(QueryDatabase db, ParseTree tree) => tree switch
    {
        ParseTree.Decl.Variable variable => new Symbol.Variable(
            db: db,
            definition: tree,
            name: variable.Identifier.Text,
            isMutable: variable.Keyword.Type == TokenType.KeywordVar),
        ParseTree.Decl.Func func => new Symbol.Function(
            db: db,
            definition: tree,
            name: func.Identifier.Text),
        ParseTree.Decl.Label label => new Symbol.Label(
            db: db,
            definition: tree,
            name: label.Identifier.Text),
        // NOTE: We might want a different symbol for parameters?
        ParseTree.FuncParam fparam => new Symbol.Variable(
            db: db,
            definition: tree,
            name: fparam.Identifier.Text,
            isMutable: false),
        _ => null,
    };

    // TODO: This API swallows errors
    /// <summary>
    /// Retrieves the <see cref="Symbol"/> referenced by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="Symbol"/>.</param>
    /// <returns>The <see cref="Symbol"/> that <paramref name="tree"/> references, or null if
    /// it does not reference any.</returns>
    public static async QueryValueTask<Symbol?> GetReferencedSymbol(QueryDatabase db, ParseTree tree) => tree switch
    {
        ParseTree.Expr.Name name => await ReferenceSymbol(db, tree, name.Identifier.Text),
        ParseTree.TypeExpr.Name name => await ReferenceSymbol(db, tree, name.Identifier.Text),
        _ => null,
    };

    // TODO: This API swallows errors
    /// <summary>
    /// Resolves a <see cref="Symbol"/> reference.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="Symbol"/>.</param>
    /// <param name="name">The name <paramref name="tree"/> references by.</param>
    /// <returns>The referenced <see cref="Symbol"/>, or null if not resolved.</returns>
    private static async QueryValueTask<Symbol?> ReferenceSymbol(QueryDatabase db, ParseTree tree, string name)
    {
        var scope = await GetDefinedScope(db, tree);
        if (scope is null)
        {
            if (tree.Parent is null) return null;
            return await ReferenceSymbol(db, tree.Parent, name);
        }
        var referencePositon = GetPosition(tree);
        var declaration = scope.LookUp(name, referencePositon);
        if (declaration is not null) return declaration.Value.Symbol;
        if (tree.Parent is null) return null;
        return await ReferenceSymbol(db, tree.Parent, name);
    }

    // NOTE: The thing does not work yet because position calculations are a little trickier to get right
    private static int GetPosition(ParseTree tree)
    {
        if (tree.Parent is null) return 0;
        var position = 0;
        foreach (var child in tree.Parent.Children)
        {
            if (ReferenceEquals(child.Green, tree.Green)) return position;
            ++position;
        }
        // NOTE: This should not happen...
        throw new InvalidOperationException();
    }

    private static ScopeKind? GetScopeKind(ParseTree tree) => tree switch
    {
        _ when tree.Parent is null => ScopeKind.Global,
        ParseTree.Expr.Block => ScopeKind.Local,
        ParseTree.Decl.Func => ScopeKind.Function,
        _ => null,
    };
}
