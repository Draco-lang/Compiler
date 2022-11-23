using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Implements lazy symbol resolution.
/// </summary>
internal static class SymbolResolution
{
    /// <summary>
    /// Categorizes positioning info.
    /// </summary>
    private enum BindingKind
    {
        /// <summary>
        /// Accessible from everywhere.
        /// </summary>
        OrderIndependent,

        /// <summary>
        /// Accessible from the definition point and onwards.
        /// </summary>
        Recursive,

        /// <summary>
        /// Accessible from only after the definition point.
        /// </summary>
        NonRecursive,
    }

    /// <summary>
    /// Checks, if the given subtree references a symbol.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to check.</param>
    /// <returns>True, if <paramref name="tree"/> references a symbol.</returns>
    public static bool ReferencesSymbol(ParseTree tree) => TryGetReferencedSymbolName(tree, out _);

    /// <summary>
    /// Retrieves the referenced symbol of a subtree.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a symbol.</param>
    /// <returns>The referenced <see cref="Symbol"/>, which can represent a reference error.</returns>
    public static Symbol GetReferencedSymbol(QueryDatabase db, ParseTree tree)
    {
        Symbol Impl(ParseTree tree)
        {
            if (!TryGetReferencedSymbolName(tree, out var name)) throw new InvalidOperationException();
            var symbol = ReferenceSymbolOrNull(db, tree, name);
            if (symbol is null)
            {
                // Emplace an error
                var diag = Diagnostic.Create(
                    template: SemanticErrors.UndefinedReference,
                    location: new Location.ToTree(tree),
                    formatArgs: name);
                symbol = new Symbol.Error(name, ImmutableArray.Create(diag));
            }
            return symbol;
        }

        return db.GetOrUpdate(tree, Impl);
    }

    /// <summary>
    /// Retrieves the containing <see cref="Scope"/> of a <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that we need the surrounding <see cref="Scope"/> of.</param>
    /// <returns>The surrounding <see cref="Scope"/> of <paramref name="tree"/>.</returns>
    public static Scope? GetContainingScopeOrNull(QueryDatabase db, ParseTree tree)
    {
        Scope? Impl(ParseTree tree)
        {
            var parent = GetScopeDefiningAncestor(tree);
            if (parent is null) return null;
            return GetDefinedScopeOrNull(db, parent);
        }

        return db.GetOrUpdate(tree, Impl);
    }

    /// <summary>
    /// Retrieves the <see cref="Scope"/> that is introduced by a <see cref="ParseTree"/> node.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to retrieve the <see cref="Scope"/> for.</param>
    /// <returns>The <see cref="Scope"/> associated with <paramref name="tree"/>, or null
    /// if it does not define a scope.</returns>
    public static Scope? GetDefinedScopeOrNull(QueryDatabase db, ParseTree tree)
    {
        Scope? Impl(ParseTree tree)
        {
            // First get the kind of scope this tree can define
            // If the kind is null, this node simply does not define a scope
            var scopeKind = GetScopeKind(tree);
            if (scopeKind is null) return null;

            var result = new List<Declaration>();

            // We inject intrinsics at global scope
            if (scopeKind == ScopeKind.Global) InjectIntrinsics(result);

            foreach (var (subtree, position) in EnumerateSubtreeInScope(tree))
            {
                // See if the child defines any symbol
                var symbol = GetDefinedSymbolOrNull(db, subtree);
                if (symbol is null) continue;

                // Yes, calculate position and add it
                var symbolPosition = GetBindingKind(symbol) switch
                {
                    // Order independent always just gets thrown to the beginning
                    BindingKind.OrderIndependent => 0,
                    // Recursive ones stay in-place
                    BindingKind.Recursive => position,
                    // Non-recursive ones simply get shifted after the subtree
                    BindingKind.NonRecursive => position + subtree.Width,
                    _ => throw new InvalidOperationException(),
                };
                // Add to results
                result!.Add(new(symbolPosition, symbol));
            }

            // Construct the scope
            return new Scope(
                Definition: tree,
                Kind: scopeKind.Value,
                Timelines: result
                    .GroupBy(d => d.Name)
                    .ToImmutableDictionary(g => g.Key, g => new DeclarationTimeline(g)));
        }

        return db.GetOrUpdate(tree, Impl);
    }

    /// <summary>
    /// Retrieves the <see cref="Symbol"/> defined by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that is asked if it defines a <see cref="Symbol"/>.</param>
    /// <returns>The <see cref="Symbol"/> that <paramref name="tree"/> defines, or null if
    /// it does not define any symbol.</returns>
    public static Symbol? GetDefinedSymbolOrNull(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        tree,
        Symbol? (tree) => tree switch
        {
            ParseTree.Decl.Variable variable => new Symbol.Variable(
                db: db,
                name: variable.Identifier.Text,
                definition: tree,
                isMutable: variable.Keyword.Type == TokenType.KeywordVar),
            ParseTree.Decl.Func func => new Symbol.Function(
                db: db,
                name: func.Identifier.Text,
                definition: tree),
            ParseTree.Decl.Label label => new Symbol.Label(
                db: db,
                name: label.Identifier.Text,
                definition: tree),
            // NOTE: We might want a different symbol for parameters?
            ParseTree.FuncParam fparam => new Symbol.Variable(
                db: db,
                name: fparam.Identifier.Text,
                definition: tree,
                isMutable: false),
            _ => null,
        });

    /// <summary>
    /// Retrieves the <see cref="Symbol"/> referenced by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="Symbol"/>.</param>
    /// <returns>The <see cref="Symbol"/> that <paramref name="tree"/> references, or null if
    /// it does not reference any.</returns>
    public static Symbol? GetReferencedSymbolOrNull(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        tree,
        tree => TryGetReferencedSymbolName(tree, out var name)
            ? ReferenceSymbolOrNull(db, tree, name)
            : null);

    /// <summary>
    /// Resolves a <see cref="Symbol"/> reference.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="Symbol"/>.</param>
    /// <param name="name">The name <paramref name="tree"/> references by.</param>
    /// <returns>The referenced <see cref="Symbol"/>, or null if not resolved.</returns>
    private static Symbol? ReferenceSymbolOrNull(QueryDatabase db, ParseTree tree, string name)
    {
        Symbol? Impl(ParseTree tree, string name)
        {
            // Walk up the tree for the scope owner
            var ancestor = GetScopeDefiningAncestor(tree);
            if (ancestor is null) return null;
            // Get the scope
            var scope = GetDefinedScopeOrNull(db, ancestor); ;
            if (scope is null) return null;
            // Compute reference position
            var referencePositon = GetPosition(ancestor, tree);
            // Look up declaration
            var declaration = scope.LookUp(name, referencePositon);
            if (declaration is not null) return declaration.Value.Symbol;
            // Not found, try in ancestor
            return ReferenceSymbolOrNull(db, ancestor, name);
        }

        return db.GetOrUpdate((tree, name), Impl);
    }

    /// <summary>
    /// Attempts to retrieve the name of the referenced symbol by a tree node.
    /// </summary>
    /// <param name="tree">The tree that might reference a symbol.</param>
    /// <param name="name">The referenced symbol name gets written here.</param>
    /// <returns>True, if <paramref name="tree"/> references a symbol and the result is written to <paramref name="name"/>.</returns>
    private static bool TryGetReferencedSymbolName(ParseTree tree, [MaybeNullWhen(false)] out string name)
    {
        if (tree is ParseTree.Expr.Name nameExpr)
        {
            name = nameExpr.Identifier.Text;
            return true;
        }
        if (tree is ParseTree.TypeExpr.Name nameTypeExpr)
        {
            name = nameTypeExpr.Identifier.Text;
            return true;
        }
        name = null;
        return false;
    }

    /// <summary>
    /// Retrieves the relative position of a tree node.
    /// </summary>
    /// <param name="ancestor">The ancestor of <paramref name="tree"/> to get the position relative to.</param>
    /// <param name="tree">The tree to get the relative position of.</param>
    /// <returns>The relative position of <paramref name="tree"/> in <paramref name="ancestor"/>.</returns>
    private static int GetPosition(ParseTree ancestor, ParseTree tree)
    {
        // Search for this subtree
        foreach (var (child, position) in EnumerateSubtreeInScope(ancestor))
        {
            if (ReferenceEquals(tree.Green, child.Green)) return position;
        }
        // NOTE: This should not happen...
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Retrieves all subtrees with their position.
    /// </summary>
    /// <param name="tree">The tree to get all subtrees of.</param>
    /// <returns>The pairs of subtrees and relative positions of <paramref name="tree"/>.</returns>
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
                if (GetScopeKind(child) is null)
                {
                    // Otherwise, we can recurse
                    foreach (var item in Impl(child, offset)) yield return item;
                }

                offset += child.Width;
            }
        }

        return Impl(tree, 0);
    }

    /// <summary>
    /// Retrieves the closest ancestor of <paramref name="tree"/> that defines a scope.
    /// </summary>
    /// <param name="tree">The tree to get the scope defining ancestor of.</param>
    /// <returns>The closest ancestor of <paramref name="tree"/> that defines a scope.</returns>
    private static ParseTree? GetScopeDefiningAncestor(ParseTree tree)
    {
        while (true)
        {
            if (tree.Parent is null) return null;
            tree = tree.Parent;

            if (GetScopeKind(tree) is not null) return tree;
        }
    }

    /// <summary>
    /// Retrieves the <see cref="BindingKind"/> a symbol introduces.
    /// </summary>
    /// <param name="symbol">The symbol that introduces a binding.</param>
    /// <returns>The <see cref="BindingKind"/> of <paramref name="symbol"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static BindingKind GetBindingKind(Symbol symbol) => symbol switch
    {
        Symbol.Label or Symbol.Function => BindingKind.OrderIndependent,
        Symbol.Variable => BindingKind.NonRecursive,
        _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
    };

    /// <summary>
    /// Retrieves the kind of scope that a tree node defines.
    /// </summary>
    /// <param name="tree">The tree to get the <see cref="ScopeKind"/> for.</param>
    /// <returns>The <see cref="ScopeKind"/> for the scope that <paramref name="tree"/> defines, or null if it does not define a scope.</returns>
    private static ScopeKind? GetScopeKind(ParseTree tree) => tree switch
    {
        _ when tree.Parent is null => ScopeKind.Global,
        ParseTree.Expr.Block => ScopeKind.Local,
        ParseTree.Decl.Func => ScopeKind.Function,
        _ => null,
    };

    private static void InjectIntrinsics(List<Declaration> declarations)
    {
        void AddBuiltinType(string name, System.Type type) =>
            declarations.Add(new(0, new Symbol.TypeAlias(name, new Type.Builtin(type))));

        AddBuiltinType("int32", typeof(int));
        AddBuiltinType("string", typeof(string));
        AddBuiltinType("char", typeof(char));
        AddBuiltinType("bool", typeof(bool));
    }
}
