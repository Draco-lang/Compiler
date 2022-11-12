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
    /// Checks, if the given subtree defines a scope.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to check.</param>
    /// <returns>True, if <paramref name="tree"/> defines a scope.</returns>
    public static async Task<bool> DefinesScope(QueryDatabase db, ParseTree tree) =>
        await GetDefinedScopeOrNull(db, tree) is not null;

    /// <summary>
    /// Checks, if the given subtree defines a symbol.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to check.</param>
    /// <returns>True, if <paramref name="tree"/> defines a symbol.</returns>
    public static Task<bool> DefinesSymbol(QueryDatabase db, ParseTree tree) =>
        Task.FromResult(TryGetReferencedSymbolName(tree, out _));

    /// <summary>
    /// Retrieves the referenced symbol of a subtree.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a symbol.</param>
    /// <returns>The referenced <see cref="Symbol"/>, which can represent a reference error.</returns>
    public static async Task<Symbol> GetReferencedSymbol(QueryDatabase db, ParseTree tree)
    {
        if (!TryGetReferencedSymbolName(tree, out var name)) throw new InvalidOperationException();
        var symbol = await ReferenceSymbolOrNull(db, tree, name);
        if (symbol is null)
        {
            // Emplace an error
            var diag = Diagnostic.Create(
                template: SemanticErrors.UndefinedReference,
                location: tree.Green.Location,
                formatArgs: name);
            symbol = new Symbol.Error(name, ImmutableArray.Create(diag));
        }
        return symbol;
    }

    /// <summary>
    /// Retrieves the containing <see cref="Scope"/> of a <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that we need the surrounding <see cref="Scope"/> of.</param>
    /// <returns>The surrounding <see cref="Scope"/> of <paramref name="tree"/>.</returns>
    public static async Task<Scope?> GetContainingScopeOrNull(QueryDatabase db, ParseTree tree)
    {
        var parent = GetScopeDefiningParent(tree);
        if (parent is null) return null;
        return await GetDefinedScopeOrNull(db, parent);
    }

    /// <summary>
    /// Retrieves the <see cref="Scope"/> that is introduced by a <see cref="ParseTree"/> node.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to retrieve the <see cref="Scope"/> for.</param>
    /// <returns>The <see cref="Scope"/> associated with <paramref name="tree"/>, or null
    /// if it does not define a scope.</returns>
    public static async QueryValueTask<Scope?> GetDefinedScopeOrNull(QueryDatabase db, ParseTree tree)
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
            var symbol = await GetDefinedSymbolOrNull(db, subtree);
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
            Kind: scopeKind.Value,
            Timelines: result
                .GroupBy(d => d.Name)
                .ToImmutableDictionary(g => g.Key, g => new DeclarationTimeline(g)));
    }

    /// <summary>
    /// Retrieves the <see cref="Symbol"/> defined by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="scope">The kind of the enclosing scope.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that is asked if it defines a <see cref="Symbol"/>.</param>
    /// <returns>The <see cref="Symbol"/> that <paramref name="tree"/> defines, or null if
    /// it does not define any symbol.</returns>
    public static async QueryValueTask<Symbol?> GetDefinedSymbolOrNull(QueryDatabase db, ParseTree tree) => tree switch
    {
        ParseTree.Decl.Variable variable => new Symbol.Variable(
            db: db,
            name: variable.Identifier.Text,
            definition: tree.Green,
            isMutable: variable.Keyword.Type == TokenType.KeywordVar),
        ParseTree.Decl.Func func => new Symbol.Function(
            db: db,
            name: func.Identifier.Text,
            definition: tree.Green),
        ParseTree.Decl.Label label => new Symbol.Label(
            db: db,
            name: label.Identifier.Text,
            definition: tree.Green),
        // NOTE: We might want a different symbol for parameters?
        ParseTree.FuncParam fparam => new Symbol.Variable(
            db: db,
            name: fparam.Identifier.Text,
            definition: tree.Green,
            isMutable: false),
        _ => null,
    };

    /// <summary>
    /// Retrieves the <see cref="Symbol"/> referenced by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="Symbol"/>.</param>
    /// <returns>The <see cref="Symbol"/> that <paramref name="tree"/> references, or null if
    /// it does not reference any.</returns>
    public static async QueryValueTask<Symbol?> GetReferencedSymbolOrNull(
        QueryDatabase db,
        ParseTree tree) => TryGetReferencedSymbolName(tree, out var name)
        ? await ReferenceSymbolOrNull(db, tree, name)
        : null;

    /// <summary>
    /// Resolves a <see cref="Symbol"/> reference.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="Symbol"/>.</param>
    /// <param name="name">The name <paramref name="tree"/> references by.</param>
    /// <returns>The referenced <see cref="Symbol"/>, or null if not resolved.</returns>
    private static async QueryValueTask<Symbol?> ReferenceSymbolOrNull(QueryDatabase db, ParseTree tree, string name)
    {
        // Walk up the tree for the scope owner
        var parent = GetScopeDefiningParent(tree);
        if (parent is null) return null;
        // Get the scope
        var scope = await GetDefinedScopeOrNull(db, parent);;
        if (scope is null) return null;
        // Compute reference position
        var referencePositon = GetPosition(parent, tree);
        // Look up declaration
        var declaration = scope.LookUp(name, referencePositon);
        if (declaration is not null) return declaration.Value.Symbol;
        // Not found, try in parent
        return await ReferenceSymbolOrNull(db, parent, name);
    }

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

    private static int GetPosition(ParseTree parent, ParseTree tree)
    {
        // Search for this subtree
        foreach (var (child, position) in EnumerateSubtreeInScope(parent))
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

    private static ParseTree? GetScopeDefiningParent(ParseTree tree)
    {
        while (true)
        {
            if (tree.Parent is null) return null;
            tree = tree.Parent;

            if (GetScopeKind(tree) is not null) return tree;
        }
    }

    private static BindingKind GetBindingKind(Symbol symbol) => symbol switch
    {
        Symbol.Label or Symbol.Function => BindingKind.OrderIndependent,
        Symbol.Variable => BindingKind.NonRecursive,
        _ => throw new InvalidOperationException(),
    };

    private static ScopeKind? GetScopeKind(ParseTree tree) => tree switch
    {
        _ when tree.Parent is null => ScopeKind.Global,
        ParseTree.Expr.Block => ScopeKind.Local,
        ParseTree.Decl.Func => ScopeKind.Function,
        _ => null,
    };

    // NOTE: Pretty temporary...
    private static void InjectIntrinsics(List<Declaration> declarations)
    {
        void Add(string name) => declarations.Add(new(0, new Symbol.Intrinsic(name)));

        Add("println");
        Add("print");
    }
}
