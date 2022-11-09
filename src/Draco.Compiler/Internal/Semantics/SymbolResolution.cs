using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;
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

        foreach (var (subtree, position) in EnumerateSubtreeInScope(tree))
        {
            var symbol = await GetDefinedSymbol(db, subtree);

            // See if the child defines any symbol
            if (symbol is not null)
            {
                // Yes, calculate position and add it
                var symbolPosition = position;
                // If we don't allow recursive binding for the symbol, we simply shift position
                if (!symbol.AllowsRecursiveBinding) symbolPosition += subtree.Width;
                // Add to results
                result!.Add(new(symbolPosition, symbol));
            }
        }

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
        var scope = await GetContainingScope(db, tree);
        if (scope is null) return null;
        var referencePositon = GetPosition(tree);
        var declaration = scope.LookUp(name, referencePositon);
        if (declaration is not null) return declaration.Value.Symbol;
        if (tree.Parent is null) return null;
        return await ReferenceSymbol(db, tree.Parent, name);
    }

    private static int GetPosition(ParseTree tree)
    {
        // Step up at least once
        var treeParent = tree.Parent;
        if (treeParent is null) return 0;
        // Walk up to the nearest scope owner
        while (GetScopeKind(treeParent) is null)
        {
            treeParent = treeParent.Parent;
            if (treeParent is null) return 0;
        }
        // Search for this subtree
        foreach (var (child, position) in EnumerateSubtreeInScope(treeParent))
        {
            if (ReferenceEquals(tree.Green, child.Green)) return position;
        }
        // NOTE: This should not happen...
        throw new InvalidOperationException();
    }

    private static IEnumerable<(ParseTree Tree, int Position)> EnumerateSubtreeInScope(ParseTree tree)
    {
        // This method must be called on a scope-owning subtree
        Debug.Assert(GetScopeKind(tree) is not null);

        static IEnumerable<(ParseTree Tree, int Position)> Impl(ParseTree tree, int offset)
        {
            // We go through each child of the current tree
            foreach (var child in tree.Children)
            {
                // We yield the child first
                yield return (child, offset);

                // If the child defines a scope, we don't recurse
                if (GetScopeKind(child) is not null) continue;

                // Otherwise, we can recurse
                foreach (var item in Impl(child, offset)) yield return item;

                offset += child.Width;
            }
        }

        return Impl(tree, 0);
    }

    private static ScopeKind? GetScopeKind(ParseTree tree) => tree switch
    {
        _ when tree.Parent is null => ScopeKind.Global,
        ParseTree.Expr.Block => ScopeKind.Local,
        ParseTree.Decl.Func => ScopeKind.Function,
        _ => null,
    };
}
