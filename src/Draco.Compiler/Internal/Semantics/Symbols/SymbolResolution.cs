using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Utilities;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.Symbols;

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
    /// Retrieves the <see cref="Diagnostic"/> messages relating to symbol resolution.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to check.</param>
    /// <returns>The <see cref="Diagnostic"/>s related to <paramref name="tree"/>.</returns>
    public static IEnumerable<Diagnostic> GetDiagnostics(QueryDatabase db, ParseTree tree)
    {
        if (ReferencesSymbol(tree))
        {
            var sym = GetReferencedSymbol(db, tree);
            return sym.Diagnostics;
        }
        else
        {
            return Enumerable.Empty<Diagnostic>();
        }
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
    /// <returns>The referenced <see cref="ISymbol"/>, which can represent a reference error.</returns>
    public static ISymbol GetReferencedSymbol(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        tree,
        ISymbol (tree) =>
        {
            if (!TryGetReferencedSymbolName(tree, out var name)) throw new InvalidOperationException();
            var symbol = ReferenceSymbolOrNull(db, tree, name);
            if (symbol is null)
            {
                // Emplace an error
                var diag = Diagnostic.Create(
                    template: SemanticErrors.UndefinedReference,
                    location: new Location.TreeReference(tree),
                    formatArgs: name);
                symbol = ISymbol.MakeReferenceError(name, ImmutableArray.Create(diag));
            }
            return symbol;
        });

    /// <summary>
    /// Retrieves the parent <see cref="Scope"/> of another scope.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="scope">The scope to retieve the parent of.</param>
    /// <returns>The parent scope of <paramref name="scope"/>.</returns>
    public static IScope? GetParentScopeOrNull(QueryDatabase db, IScope scope)
    {
        if (scope.Definition is null) throw new InvalidOperationException();
        var ancestor = GetScopeDefiningAncestor(scope.Definition);
        if (ancestor is null) return null;
        return GetDefinedScopeOrNull(db, ancestor);
    }

    /// <summary>
    /// Retrieves the containing <see cref="Scope"/> of a <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that we need the surrounding <see cref="Scope"/> of.</param>
    /// <returns>The surrounding <see cref="Scope"/> of <paramref name="tree"/>.</returns>
    public static IScope? GetContainingScopeOrNull(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        tree,
        IScope? (tree) =>
        {
            var parent = GetScopeDefiningAncestor(tree);
            if (parent is null) return null;
            return GetDefinedScopeOrNull(db, parent);
        });

    /// <summary>
    /// Retrieves the <see cref="Scope"/> that is introduced by a <see cref="ParseTree"/> node.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to retrieve the <see cref="Scope"/> for.</param>
    /// <returns>The <see cref="Scope"/> associated with <paramref name="tree"/>, or null
    /// if it does not define a scope.</returns>
    public static IScope? GetDefinedScopeOrNull(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        tree,
        IScope? (tree) =>
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
                var symbolPosition = GetBindingKind(scopeKind.Value, symbol) switch
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
            return IScope.Make(
                db: db,
                kind: scopeKind.Value,
                definition: tree,
                timelines: result
                    .GroupBy(d => d.Name)
                    .ToImmutableDictionary(g => g.Key, ConstructTimeline));
        });

    /// <summary>
    /// Constructs a <see cref="DeclarationTimeline"/> from the given <see cref="Declaration"/>s belonging to the same name.
    /// </summary>
    /// <param name="declarations">The <see cref="Declaration"/>s that are declared under the same name.</param>
    /// <returns>The constructed <see cref="DeclarationTimeline"/>.</returns>
    private static DeclarationTimeline ConstructTimeline(IEnumerable<Declaration> declarations)
    {
        // We expect the declarations to all relate to the same name and ordered by position
        Debug.Assert(!declarations.Any() || declarations.All(d => d.Name == declarations.First().Name));
        Debug.Assert(declarations.Select(d => d.Position).IsOrdered());

        var result = ImmutableArray.CreateBuilder<Declaration>();
        foreach (var declsAtPosition in declarations.GroupBy(d => d.Position))
        {
            var symbols = declsAtPosition.Select(d => d.Symbol);
            // Map
            var replacements = ConstructSymbolsFromSymbolGroup(symbols)
                .Select(sym => new Declaration(declsAtPosition.Key, sym));
            // Add to the results
            foreach (var item in replacements) result.Add(item);
        }

        return new(result.ToImmutable());
    }

    // TODO: Flaw
    // Illegal entries will still be part of resolution and be under the same name and same position as non-error entries
    // This needs to be solved somehow, to at least prefer the non-error entries, when preferrable
    /// <summary>
    /// Constructs <see cref="ISymbol"/>s from a group of <see cref="ISymbol"/>s that happen to be under the same position.
    /// This method merges symbols that can be overloaded, so it might return less symbols than given.
    /// The illegal overloads (like trying to overload global variables) are also ruled out here.
    /// </summary>
    /// <param name="symbols">The <see cref="ISymbol"/>s under the same position.</param>
    /// <returns>The sequence of <see cref="ISymbol"/>s that can be used in a <see cref="DeclarationTimeline"/>.</returns>
    private static IEnumerable<ISymbol> ConstructSymbolsFromSymbolGroup(IEnumerable<ISymbol> symbols)
    {
        Debug.Assert(symbols.Any());

        var enumerator = symbols.GetEnumerator();
        enumerator.MoveNext();

        // Based on the first symbol we decide what we do
        var first = enumerator.Current;
        switch (first)
        {
        case ISymbol.IFunction:
        case ISymbol.IUnaryOperator:
        case ISymbol.IBinaryOperator:
        {
            // Overloading
            // TODO: Search in parent for existing overloads to merge
            // TODO: Only yield first if it's alone
            yield return first;
            while (enumerator.MoveNext())
            {
                // TODO
                throw new NotImplementedException();
            }
            break;
        }

        default:
        {
            // Disallow overloading
            yield return first;
            // The rest are considered illegal redeclaration
            while (enumerator.MoveNext())
            {
                // TODO
                throw new NotImplementedException();
            }
            break;
        }
        }
    }

    /// <summary>
    /// Utility for internal API to expect a symbol defined by a certain type of tree.
    /// See <see cref="GetDefinedSymbolOrNull(QueryDatabase, ParseTree)"/>.
    /// </summary>
    public static TSymbol GetDefinedSymbolExpected<TSymbol>(QueryDatabase db, ParseTree tree)
        where TSymbol : ISymbol
    {
        var symbol = GetDefinedSymbolOrNull(db, tree);
        if (symbol is null) throw new InvalidOperationException("The parse tree does not define a symbol");
        if (symbol is not TSymbol tSymbol) throw new InvalidOperationException("The parse tree defines a differen kind of symbol");
        return tSymbol;
    }

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> defined by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that is asked if it defines a <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="ISymbol"/> that <paramref name="tree"/> defines, or null if
    /// it does not define any symbol.</returns>
    public static ISymbol? GetDefinedSymbolOrNull(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        tree,
        ISymbol? (tree) => tree switch
        {
            ParseTree.Decl.Variable variable => ISymbol.MakeVariable(
                db: db,
                name: variable.Identifier.Text,
                definition: tree,
                isMutable: variable.Keyword.Type == TokenType.KeywordVar),
            ParseTree.Decl.Func func => ISymbol.MakeFunction(
                db: db,
                name: func.Identifier.Text,
                definition: tree),
            ParseTree.Decl.Label label => ISymbol.MakeLabel(
                db: db,
                name: label.Identifier.Text,
                definition: tree),
            ParseTree.FuncParam fparam => ISymbol.MakeParameter(
                db: db,
                name: fparam.Identifier.Text,
                definition: tree),
            _ => null,
        });

    /// <summary>
    /// Utility for internal API to expect a symbol referenced by a certain type of tree.
    /// See <see cref="GetReferencedSymbolOrNull(QueryDatabase, ParseTree)"/>.
    /// </summary>
    public static TSymbol GetReferencedSymbolExpected<TSymbol>(QueryDatabase db, ParseTree tree)
        where TSymbol : ISymbol
    {
        var symbol = GetReferencedSymbolOrNull(db, tree);
        if (symbol is null) throw new InvalidOperationException("The parse tree does not reference a symbol");
        if (symbol is not TSymbol tSymbol) throw new InvalidOperationException("The parse tree references a differen kind of symbol");
        return tSymbol;
    }

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="ISymbol"/> that <paramref name="tree"/> references, or null if
    /// it does not reference any.</returns>
    public static ISymbol? GetReferencedSymbolOrNull(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        tree,
        tree => TryGetReferencedSymbolName(tree, out var name)
            ? ReferenceSymbolOrNull(db, tree, name)
            : null);

    /// <summary>
    /// Resolves a <see cref="ISymbol"/> reference.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that references a <see cref="ISymbol"/>.</param>
    /// <param name="name">The name <paramref name="tree"/> references by.</param>
    /// <returns>The referenced <see cref="ISymbol"/>, or null if not resolved.</returns>
    private static ISymbol? ReferenceSymbolOrNull(QueryDatabase db, ParseTree tree, string name) => db.GetOrUpdate(
        (tree, name),
        ISymbol? (tree, name) =>
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
        });

    /// <summary>
    /// Attempts to retrieve the name of the referenced symbol by a tree node.
    /// </summary>
    /// <param name="tree">The tree that might reference a symbol.</param>
    /// <param name="name">The referenced symbol name gets written here.</param>
    /// <returns>True, if <paramref name="tree"/> references a symbol and the result is written to <paramref name="name"/>.</returns>
    private static bool TryGetReferencedSymbolName(ParseTree tree, [MaybeNullWhen(false)] out string name)
    {
        switch (tree)
        {
        case ParseTree.Expr.Name nameExpr:
            name = nameExpr.Identifier.Text;
            return true;

        case ParseTree.TypeExpr.Name nameTypeExpr:
            name = nameTypeExpr.Identifier.Text;
            return true;

        case ParseTree.Expr.Unary uryExpr:
            name = GetUnaryOperatorName(uryExpr.Operator.Type);
            return true;

        case ParseTree.Expr.Binary binExpr:
            // NOTE: Assignment is also denoted as binary, but that does not reference an operator
            name = GetBinaryOperatorName(binExpr.Operator.Type);
            return name is not null;

        case ParseTree.ComparisonElement cmpElement:
            name = GetRelationalOperatorName(cmpElement.Operator.Type);
            return true;

        default:
            name = null;
            return false;
        }
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
    /// <param name="scopeKind">The <see cref="ScopeKind"/> that <paramref name="symbol"/> is defined in.</param>
    /// <param name="symbol">The symbol that introduces a binding.</param>
    /// <returns>The <see cref="BindingKind"/> of <paramref name="symbol"/>.</returns>
    private static BindingKind GetBindingKind(ScopeKind scopeKind, ISymbol symbol) => symbol switch
    {
        ISymbol.ILabel or ISymbol.IFunction => BindingKind.OrderIndependent,
        ISymbol.IParameter => BindingKind.OrderIndependent,
        ISymbol.IVariable when scopeKind == ScopeKind.Global => BindingKind.OrderIndependent,
        ISymbol.IVariable => BindingKind.NonRecursive,
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

    internal static string GetUnaryOperatorName(TokenType op) => op switch
    {
        TokenType.Plus => "unary operator +",
        TokenType.Minus => "unary operator -",
        TokenType.KeywordNot => "unary operator not",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    internal static string? GetBinaryOperatorName(TokenType op) => op switch
    {
        TokenType.Assign or TokenType.KeywordAnd or TokenType.KeywordOr => null,
        TokenType.Plus or TokenType.PlusAssign => "binary operator +",
        TokenType.Minus or TokenType.MinusAssign => "binary operator -",
        TokenType.Star or TokenType.StarAssign => "binary operator *",
        TokenType.Slash or TokenType.SlashAssign => "binary operator /",
        TokenType.KeywordMod => "binary operator mod",
        TokenType.KeywordRem => "binary operator mod",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    internal static string GetRelationalOperatorName(TokenType op) => op switch
    {
        TokenType.LessThan => "binary operator <",
        TokenType.GreaterThan => "binary operator >",
        TokenType.LessEqual => "binary operator <=",
        TokenType.GreaterEqual => "binary operator >=",
        TokenType.Equal => "binary operator ==",
        TokenType.NotEqual => "binary operator !=",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    private static void InjectIntrinsics(List<Declaration> declarations)
    {
        void Add(ISymbol symbol) =>
            declarations.Add(new(0, symbol));

        void AddBuiltinFunction(string name, ImmutableArray<Type> @params, Type ret) =>
            Add(ISymbol.MakeIntrinsicFunction(name, @params, ret));

        // Types
        Add(Intrinsics.Types.Unit);
        Add(Intrinsics.Types.Int32);
        Add(Intrinsics.Types.String);
        Add(Intrinsics.Types.Bool);
        Add(Intrinsics.Types.Char);

        // Operators
        Add(Intrinsics.Operators.Not_Bool);
        Add(Intrinsics.Operators.Pos_Int32);
        Add(Intrinsics.Operators.Neg_Int32);

        Add(Intrinsics.Operators.Add_Int32);
        Add(Intrinsics.Operators.Sub_Int32);
        Add(Intrinsics.Operators.Mul_Int32);
        Add(Intrinsics.Operators.Div_Int32);
        Add(Intrinsics.Operators.Mod_Int32);

        Add(Intrinsics.Operators.Less_Int32);
        Add(Intrinsics.Operators.Greater_Int32);
        Add(Intrinsics.Operators.LessEqual_Int32);
        Add(Intrinsics.Operators.GreaterEqual_Int32);
        Add(Intrinsics.Operators.Equal_Int32);
        Add(Intrinsics.Operators.NotEqual_Int32);

        // TODO: Temporary
        AddBuiltinFunction("println", ImmutableArray.Create(Type.String), Type.Unit);
    }
}
