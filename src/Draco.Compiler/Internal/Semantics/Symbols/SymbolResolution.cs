using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;

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
    /// <param name="tree">The <see cref="SyntaxNode"/> to check.</param>
    /// <returns>The <see cref="Diagnostic"/>s related to <paramref name="tree"/>.</returns>
    public static IEnumerable<Diagnostic> GetDiagnostics(QueryDatabase db, SyntaxNode tree)
    {
        var referencedSymbolDiags = ReferencesSymbol(tree)
            ? GetReferencedSymbol(db, tree).Diagnostics
            : ImmutableArray<Diagnostic>.Empty;
        var definedSymbolDiags = GetDefinedSymbolOrNull(db, tree)?.Diagnostics ?? ImmutableArray<Diagnostic>.Empty;
        return referencedSymbolDiags
            .Concat(definedSymbolDiags);
    }

    // Referenced symbol ///////////////////////////////////////////////////////

    /// <summary>
    /// Checks, if the given subtree references a symbol.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> to check.</param>
    /// <returns>True, if <paramref name="tree"/> references a symbol.</returns>
    public static bool ReferencesSymbol(SyntaxNode tree) => TryGetReferencedSymbolName(tree, out _);

    /// <summary>
    /// Retrieves the referenced symbol of a subtree.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that references a symbol.</param>
    /// <returns>The referenced <see cref="ISymbol"/>, which can represent a reference error.</returns>
    public static ISymbol GetReferencedSymbol(QueryDatabase db, SyntaxNode tree) => tree switch
    {
        NameExpressionSyntax => GetReferencedSymbol<ISymbol.ITyped>(db, tree),
        NameTypeSyntax => GetReferencedSymbol<ISymbol.ITypeDefinition>(db, tree),
        NameLabelSyntax => GetReferencedSymbol<ISymbol.ILabel>(db, tree),
        UnaryExpressionSyntax or BinaryExpressionSyntax or ComparisonElementSyntax =>
            // NOTE: Names are not type-able, we can rely on any symbol
            GetReferencedSymbol<ISymbol>(db, tree),
        _ => throw new InvalidOperationException(),
    };

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that references a <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="ISymbol"/> that <paramref name="tree"/> references, or null if
    /// it does not reference any.</returns>
    public static ISymbol? GetReferencedSymbolOrNull(QueryDatabase db, SyntaxNode tree) => tree switch
    {
        NameExpressionSyntax => GetReferencedSymbolOrNull<ISymbol.ITyped>(db, tree),
        NameTypeSyntax => GetReferencedSymbolOrNull<ISymbol.ITypeDefinition>(db, tree),
        UnaryExpressionSyntax or BinaryExpressionSyntax or ComparisonElementSyntax =>
            // NOTE: Names are not type-able, we can rely on any symbol
            GetReferencedSymbolOrNull<ISymbol>(db, tree),
        _ => null,
    };

    /// <summary>
    /// Retrieves the referenced symbol of a subtree.
    /// </summary>
    /// <typeparam name="TSymbol">The symbol type to look up.</typeparam>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that references a symbol.</param>
    /// <returns>The referenced <see cref="ISymbol"/>, which can represent a reference error.</returns>
    public static TSymbol GetReferencedSymbol<TSymbol>(QueryDatabase db, SyntaxNode tree)
         where TSymbol : class, ISymbol => db.GetOrUpdate(
        tree,
        TSymbol (tree) =>
        {
            if (!TryGetReferencedSymbolName(tree, out var name)) throw new InvalidOperationException();
            var symbol = ReferenceSymbolOrNull<TSymbol>(db, tree, name);
            if (symbol is null)
            {
                // Emplace an error
                var diag = Diagnostic.Create(
                    template: SymbolResolutionErrors.UndefinedReference,
                    location: new Location.TreeReference(tree),
                    formatArgs: name);
                symbol = (TSymbol)Symbol.MakeReferenceError(name, ImmutableArray.Create(diag));
            }
            return symbol;
        });

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> referenced by the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <typeparam name="TSymbol">The symbol type to look up.</typeparam>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that references a <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="ISymbol"/> that <paramref name="tree"/> references, or null if
    /// it does not reference any.</returns>
    public static TSymbol? GetReferencedSymbolOrNull<TSymbol>(QueryDatabase db, SyntaxNode tree)
        where TSymbol : class, ISymbol => db.GetOrUpdate(
        tree,
        tree => TryGetReferencedSymbolName(tree, out var name)
            ? ReferenceSymbolOrNull<TSymbol>(db, tree, name)
            : null);

    /// <summary>
    /// Resolves a <see cref="ISymbol"/> reference.
    /// </summary>
    /// <typeparam name="TSymbol">The symbol type to look up.</typeparam>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that references a <see cref="ISymbol"/>.</param>
    /// <param name="name">The name <paramref name="tree"/> references by.</param>
    /// <returns>The referenced <see cref="ISymbol"/>, or null if not resolved.</returns>
    public static TSymbol? ReferenceSymbolOrNull<TSymbol>(QueryDatabase db, SyntaxNode tree, string name)
        where TSymbol : class, ISymbol => db.GetOrUpdate(
        (tree, name),
        TSymbol? (tree, name) =>
        {
            static TSymbol? NonVariable(TSymbol? symbol) => symbol is ISymbol.IVariable ? null : symbol;

            var crossedFunctionBoundary = false;
            while (true)
            {
                // Walk up the tree for the scope owner
                var ancestor = GetScopeDefiningAncestor(tree);
                if (ancestor is null) return null;
                // Get the scope
                var scope = GetDefinedScopeOrNull(db, ancestor);
                if (scope is null) return null;
                crossedFunctionBoundary = crossedFunctionBoundary || scope.Kind == ScopeKind.Function;
                // Compute reference position
                var referencePositon = GetPosition(ancestor, tree);
                // Look up symbol
                var symbol = scope.Kind == ScopeKind.Global && !crossedFunctionBoundary
                    // We have hit global scope from a global context, we can't reference other globals
                    ? scope.LookUp(name, referencePositon, x => NonVariable(x as TSymbol))
                    // Anything of type plays
                    : scope.LookUp(name, referencePositon, x => x as TSymbol);
                if (symbol is not null) return symbol;
                // Not found, try in ancestor
                tree = ancestor;
            }
        });

    /// <summary>
    /// Attempts to retrieve the name of the referenced symbol by a tree node.
    /// </summary>
    /// <param name="tree">The tree that might reference a symbol.</param>
    /// <param name="name">The referenced symbol name gets written here.</param>
    /// <returns>True, if <paramref name="tree"/> references a symbol and the result is written to <paramref name="name"/>.</returns>
    private static bool TryGetReferencedSymbolName(SyntaxNode tree, [MaybeNullWhen(false)] out string name)
    {
        switch (tree)
        {
        case NameExpressionSyntax nameExpr:
            name = nameExpr.Name.Text;
            return true;

        case NameTypeSyntax nameTypeExpr:
            name = nameTypeExpr.Name.Text;
            return true;

        case NameLabelSyntax labelName:
            name = labelName.Name.Text;
            return true;

        case UnaryExpressionSyntax uryExpr:
            name = GetUnaryOperatorName(uryExpr.Operator.Kind);
            return true;

        case BinaryExpressionSyntax binExpr:
            // NOTE: Assignment is also denoted as binary, but that does not reference an operator
            name = GetBinaryOperatorName(binExpr.Operator.Kind);
            return name is not null;

        case ComparisonElementSyntax cmpElement:
            name = GetRelationalOperatorName(cmpElement.Operator.Kind);
            return true;

        default:
            name = null;
            return false;
        }
    }

    // Defined symbol //////////////////////////////////////////////////////////

    /// <summary>
    /// Utility for internal API to expect a symbol defined by a certain type of tree.
    /// See <see cref="GetDefinedSymbolOrNull(QueryDatabase, SyntaxNode)"/>.
    /// </summary>
    public static TSymbol GetDefinedSymbolExpected<TSymbol>(QueryDatabase db, SyntaxNode tree)
        where TSymbol : ISymbol
    {
        var symbol = GetDefinedSymbolOrNull(db, tree);
        if (symbol is null) throw new InvalidOperationException("The syntax tree does not define a symbol");
        if (symbol is not TSymbol tSymbol) throw new InvalidOperationException("The syntax tree defines a differen kind of symbol");
        return tSymbol;
    }

    /// <summary>
    /// Retrieves the <see cref="ISymbol"/> defined by the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that is asked if it defines a <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="ISymbol"/> that <paramref name="tree"/> defines, or null if
    /// it does not define any symbol.</returns>
    public static ISymbol? GetDefinedSymbolOrNull(QueryDatabase db, SyntaxNode tree) => db.GetOrUpdate(
        tree,
        ISymbol? (tree) =>
        {
            var scopeDefiningAncestor = GetScopeDefiningAncestor(tree);
            if (scopeDefiningAncestor is null) return null;
            var scope = GetDefinedScopeOrNull(db, scopeDefiningAncestor);
            if (scope is null) return null;
            return scope.Declarations.TryGetValue(tree, out var symbol)
                ? symbol
                : null;
        });

    /// <summary>
    /// Constructs the <see cref="ISymbol"/> defined by <paramref name="tree"/>, or null, if
    /// it defines no symbol.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="ISymbol"/> defined by <paramref name="tree"/>, or null.</returns>
    private static ISymbol? ConstructDefinedSymbolOrNull(QueryDatabase db, SyntaxNode tree) => tree switch
    {
        VariableDeclarationSyntax variable => Symbol.MakeVariable(
            db: db,
            name: variable.Name.Text,
            definition: tree,
            isMutable: variable.Keyword.Kind == TokenKind.KeywordVar),
        FunctionDeclarationSyntax func => Symbol.MakeFunction(
            db: db,
            name: func.Name.Text,
            definition: tree),
        LabelDeclarationSyntax label => Symbol.MakeLabel(
            db: db,
            name: label.Name.Text,
            definition: tree),
        ParameterSyntax fparam => Symbol.MakeParameter(
            db: db,
            name: fparam.Name.Text,
            definition: tree),
        _ => null,
    };

    /// <summary>
    /// Retrieves the break and continue label symbols for the given loop.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The loop node.</param>
    /// <returns>The break and continue <see cref="ISymbol.ILabel"/>s for <paramref name="tree"/>.</returns>
    public static (ISymbol.ILabel Break, ISymbol.ILabel Continue) GetBreakAndContinueLabels(
        QueryDatabase db,
        WhileExpressionSyntax tree) => db.GetOrUpdate(
            tree,
            (ISymbol.ILabel Break, ISymbol.ILabel Continue) (WhileExpressionSyntax tree) =>
            {
                var breakLabel = Symbol.SynthetizeLabel("break");
                var continueLabel = Symbol.SynthetizeLabel("continue");
                return (breakLabel, continueLabel);
            });

    /// <summary>
    /// Checks, if the given tree-node is the body of a while-expression.
    /// </summary>
    /// <param name="tree">The tree to check.</param>
    /// <param name="while">The while-node gets written here, in case the parent is a while-loop and <paramref name="tree"/>
    /// is the body.</param>
    /// <returns>True, if <paramref name="tree"/> is the body of a while-loop.</returns>
    private static bool IsWhileBody(SyntaxNode tree, [MaybeNullWhen(false)] out WhileExpressionSyntax @while)
    {
        if (tree.Parent is WhileExpressionSyntax parentWhile && ReferenceEquals(tree.Green, parentWhile.Then.Green))
        {
            @while = parentWhile;
            return true;
        }
        else
        {
            @while = null;
            return false;
        }
    }

    // Scope ///////////////////////////////////////////////////////////////////

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
    /// Retrieves the containing <see cref="Scope"/> of a <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> that we need the surrounding <see cref="Scope"/> of.</param>
    /// <returns>The surrounding <see cref="Scope"/> of <paramref name="tree"/>.</returns>
    public static IScope? GetContainingScopeOrNull(QueryDatabase db, SyntaxNode tree) => db.GetOrUpdate(
        tree,
        IScope? (tree) =>
        {
            var parent = GetScopeDefiningAncestor(tree);
            if (parent is null) return null;
            return GetDefinedScopeOrNull(db, parent);
        });

    /// <summary>
    /// Retrieves the <see cref="Scope"/> that is introduced by a <see cref="SyntaxNode"/> node.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> to retrieve the <see cref="Scope"/> for.</param>
    /// <returns>The <see cref="Scope"/> associated with <paramref name="tree"/>, or null
    /// if it does not define a scope.</returns>
    public static IScope? GetDefinedScopeOrNull(QueryDatabase db, SyntaxNode tree) => db.GetOrUpdate(
        tree,
        IScope? (tree) =>
        {
            // First get the kind of scope this tree can define
            // If the kind is null, this node simply does not define a scope
            var scopeKind = GetScopeKind(tree);
            if (scopeKind is null) return null;

            var scopeBuilder = new Scope.Builder(db, scopeKind.Value, tree);

            // We inject intrinsics at global scope
            if (scopeKind == ScopeKind.Global) InjectIntrinsics(scopeBuilder);

            if (IsWhileBody(tree, out var whileParent))
            {
                // We inject break and continue
                var (breakLabel, continueLabel) = GetBreakAndContinueLabels(db, whileParent);
                scopeBuilder.Add(new(Position: 0, Symbol: breakLabel));
                scopeBuilder.Add(new(Position: 0, Symbol: continueLabel));
            }

            foreach (var (subtree, position) in EnumerateSubtreeInScope(tree))
            {
                // See if the child defines any symbol
                var symbol = ConstructDefinedSymbolOrNull(db, subtree);
                if (symbol is null) continue;

                // Yes, calculate position and add it
                var symbolPosition = GetBindingKind(scopeKind.Value, symbol) switch
                {
                    // Order independent always just gets thrown to the beginning
                    BindingKind.OrderIndependent => 0,
                    // Recursive ones stay in-place
                    BindingKind.Recursive => position,
                    // Non-recursive ones simply get shifted after the subtree
                    BindingKind.NonRecursive => position + EnumerateNodesInSameScope(subtree).Count(),
                    _ => throw new InvalidOperationException(),
                };

                // Add to timeline pre-declarations
                scopeBuilder.Add(new(Position: symbolPosition, Symbol: symbol));
            }

            // Construct the scope
            return scopeBuilder.Build();
        });

    /// <summary>
    /// Retrieves the closest ancestor of <paramref name="tree"/> that defines a scope.
    /// </summary>
    /// <param name="tree">The tree to get the scope defining ancestor of.</param>
    /// <returns>The closest ancestor of <paramref name="tree"/> that defines a scope.</returns>
    private static SyntaxNode? GetScopeDefiningAncestor(SyntaxNode tree)
    {
        while (true)
        {
            if (tree.Parent is null) return null;
            tree = tree.Parent;

            if (GetScopeKind(tree) is not null) return tree;
        }
    }

    /// <summary>
    /// Retrieves the kind of scope that a tree node defines.
    /// </summary>
    /// <param name="tree">The tree to get the <see cref="ScopeKind"/> for.</param>
    /// <returns>The <see cref="ScopeKind"/> for the scope that <paramref name="tree"/> defines, or null if it does not define a scope.</returns>
    private static ScopeKind? GetScopeKind(SyntaxNode tree) => tree switch
    {
        _ when tree.Parent is null => ScopeKind.Global,
        BlockExpressionSyntax => ScopeKind.Local,
        BlockFunctionBodySyntax => ScopeKind.Local,
        FunctionDeclarationSyntax => ScopeKind.Function,
        // We wrap up loop bodies in a scope, so the labels get defined in a proper, sanitized scope
        _ when IsWhileBody(tree, out _) => ScopeKind.Local,
        _ => null,
    };

    // General utilities ///////////////////////////////////////////////////////

    /// <summary>
    /// Retrieves the relative position of a tree node.
    /// </summary>
    /// <param name="ancestor">The ancestor of <paramref name="tree"/> to get the position relative to.</param>
    /// <param name="tree">The tree to get the relative position of.</param>
    /// <returns>The relative position of <paramref name="tree"/> in <paramref name="ancestor"/>.</returns>
    private static int GetPosition(SyntaxNode ancestor, SyntaxNode tree)
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
    private static IEnumerable<(SyntaxNode Tree, int Position)> EnumerateSubtreeInScope(SyntaxNode tree)
    {
        // This method must be called on a scope-owning subtree
        Debug.Assert(GetScopeKind(tree) is not null);

        // We just append each relevant node an index
        var offset = 0;
        foreach (var child in EnumerateNodesInSameScope(tree))
        {
            // Yield the child with the given offset
            yield return (child, offset);
            // Increase offset
            ++offset;
        }
    }

    /// <summary>
    /// Enumerates the subtree of a given node without crossing scope boundaries.
    /// </summary>
    /// <param name="tree">The root of the subtree to enumerate.</param>
    /// <returns>An enumerable containing all subtree nodes without including the children of the ones that define a scope.</returns>
    private static IEnumerable<SyntaxNode> EnumerateNodesInSameScope(SyntaxNode tree)
    {
        // We go through each child of the current tree
        foreach (var child in tree.Children)
        {
            // We yield the child first
            yield return child;

            // If the child defines a scope, we don't recurse
            if (GetScopeKind(child) is null)
            {
                // Otherwise, we can recurse
                foreach (var item in EnumerateNodesInSameScope(child)) yield return item;
            }
        }
    }

    /// <summary>
    /// Retrieves the <see cref="BindingKind"/> a symbol introduces.
    /// </summary>
    /// <param name="scopeKind">The <see cref="ScopeKind"/> that <paramref name="symbol"/> is defined in.</param>
    /// <param name="symbol">The <see cref="ISymbol"/> that was defined.</param>
    /// <returns>The <see cref="BindingKind"/> of <paramref name="symbol"/> declared in <paramref name="scopeKind"/>.</returns>
    private static BindingKind GetBindingKind(ScopeKind scopeKind, ISymbol symbol) => symbol switch
    {
        ISymbol.ILabel or ISymbol.IFunction => BindingKind.OrderIndependent,
        ISymbol.IParameter => BindingKind.OrderIndependent,
        ISymbol.IVariable when scopeKind == ScopeKind.Global => BindingKind.OrderIndependent,
        ISymbol.IVariable => BindingKind.NonRecursive,
        _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
    };

    internal static string GetUnaryOperatorName(TokenKind op) => op switch
    {
        TokenKind.Plus => "unary operator +",
        TokenKind.Minus => "unary operator -",
        TokenKind.KeywordNot => "unary operator not",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    internal static string? GetBinaryOperatorName(TokenKind op) => op switch
    {
        TokenKind.Assign or TokenKind.KeywordAnd or TokenKind.KeywordOr => null,
        TokenKind.Plus or TokenKind.PlusAssign => "binary operator +",
        TokenKind.Minus or TokenKind.MinusAssign => "binary operator -",
        TokenKind.Star or TokenKind.StarAssign => "binary operator *",
        TokenKind.Slash or TokenKind.SlashAssign => "binary operator /",
        TokenKind.KeywordMod => "binary operator mod",
        TokenKind.KeywordRem => "binary operator mod",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    internal static string GetRelationalOperatorName(TokenKind op) => op switch
    {
        TokenKind.LessThan => "binary operator <",
        TokenKind.GreaterThan => "binary operator >",
        TokenKind.LessEqual => "binary operator <=",
        TokenKind.GreaterEqual => "binary operator >=",
        TokenKind.Equal => "binary operator ==",
        TokenKind.NotEqual => "binary operator !=",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    private static void InjectIntrinsics(Scope.Builder builder)
    {
        void Add(ISymbol symbol) => builder.Add(new(0, symbol));

        // Types
        Add(Intrinsics.Types.Unit);
        Add(Intrinsics.Types.Int32);
        Add(Intrinsics.Types.Float64);
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

        Add(Intrinsics.Functions.Println);
    }
}
