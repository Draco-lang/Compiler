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
    /// Retrieves the <see cref="Scope"/> a <see cref="ParseTree"/> defines.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that might define a scope.</param>
    /// <returns>The <see cref="Scope"/> defined by <paramref name="tree"/>, or null if
    /// it does not define any.</returns>
    public static async QueryValueTask<Scope?> GetDefinedScope(QueryDatabase db, ParseTree tree) => tree switch
    {
        _ when tree.Parent is null => new Scope(ScopeKind.Global, await CollectSymbolsWithin(db, tree)),
        ParseTree.Expr.Block => new Scope(ScopeKind.Local, await CollectSymbolsWithin(db, tree)),
        ParseTree.Decl.Func => new Scope(ScopeKind.Function, await CollectSymbolsWithin(db, tree)),
        _ => null,
    };

    /// <summary>
    /// Utility to collect <see cref="Symbol"/>s defined within a scope.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that is asked for the symbols within.</param>
    /// <returns>The array of <see cref="Symbol"/> defined.</returns>
    private static async Task<ImmutableDictionary<string, Symbol>> CollectSymbolsWithin(QueryDatabase db, ParseTree tree)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, Symbol>();

        async Task Impl(ParseTree tree)
        {
            foreach (var child in tree.Children)
            {
                var symbol = await GetDefinedSymbol(db, child);
                var scopeDefinedByChild = await GetDefinedScope(db, child);

                // See if the child defines any symbol
                if (symbol is not null) builder.Add(symbol.Name, symbol);

                // If the child does not define its own scope, we can recursively collect symbols
                // If it does define its own scope, we assume they are contained within that scope
                if (scopeDefinedByChild is null) await Impl(child);
            }
        }

        await Impl(tree);
        return builder.ToImmutable();
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
        // TODO: This does not obey order-dependent symbols or even scope boundaries
        // It's just a start to get something up and running
        var scope = await GetContainingScope(db, tree);
        if (scope is null) return null;
        if (scope.Symbols.TryGetValue(name, out var symbol)) return symbol;
        if (tree.Parent is null) return null;
        return await ReferenceSymbol(db, tree.Parent, name);
    }
}
