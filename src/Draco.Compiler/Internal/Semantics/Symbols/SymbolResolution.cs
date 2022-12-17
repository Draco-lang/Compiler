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
    /// Represents a symbol declaration that is not instantiated yet.
    /// </summary>
    /// <param name="Name">The name of the symbol to be declared.</param>
    /// <param name="Tree">The tree the symbol will originate from.</param>
    /// <param name="Kind">The <see cref="SymbolKind"/> that the declaration will declare.</param>
    /// <param name="Position">The relative position of the to-be declared symbol.</param>
    private readonly record struct PreDeclaration(
        string Name,
        ParseTree Tree,
        SymbolKind Kind,
        int Position);

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

            var timelinePreDeclarations = new List<PreDeclaration>();

            // TODO
            // We inject intrinsics at global scope
            // if (scopeKind == ScopeKind.Global) InjectIntrinsics(timelineDeclarations);
            // throw new NotImplementedException();

            foreach (var (subtree, position) in EnumerateSubtreeInScope(tree))
            {
                // See if the child defines any symbol
                var kindAndName = GetDefinedSymbolKindAndName(subtree);
                if (kindAndName is null) continue;
                var (kind, name) = kindAndName.Value;

                // Yes, calculate position and add it
                var symbolPosition = GetBindingKind(scopeKind.Value, kind) switch
                {
                    // Order independent always just gets thrown to the beginning
                    BindingKind.OrderIndependent => 0,
                    // Recursive ones stay in-place
                    BindingKind.Recursive => position,
                    // Non-recursive ones simply get shifted after the subtree
                    BindingKind.NonRecursive => position + subtree.Width,
                    _ => throw new InvalidOperationException(),
                };

                // Add to timeline pre-declarations
                timelinePreDeclarations.Add(new(name, subtree, kind, symbolPosition));
            }

            // Construct the scope
            var declarations = ImmutableDictionary.CreateBuilder<ParseTree, ISymbol>();
            var timelines = timelinePreDeclarations
                .GroupBy(d => d.Name)
                .ToImmutableDictionary(g => g.Key, g => ConstructTimeline(db, g, declarations));
            return IScope.Make(
                db: db,
                kind: scopeKind.Value,
                definition: tree,
                timelines: timelines,
                declarations: declarations.ToImmutable());
        });

    /// <summary>
    /// Constructs a <see cref="DeclarationTimeline"/> from the given <see cref="PreDeclaration"/>s belonging to the same name.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="preDeclarations">The <see cref="PreDeclaration"/>s that are declared under the same name.</param>
    /// <param name="declarations">The declarations get written here mapped from the syntax to the individual unmerged symbols.</param>
    /// <returns>The constructed <see cref="DeclarationTimeline"/>.</returns>
    private static DeclarationTimeline ConstructTimeline(
        QueryDatabase db,
        IEnumerable<PreDeclaration> preDeclarations,
        ImmutableDictionary<ParseTree, ISymbol>.Builder declarations)
    {
        // We expect the declarations to all relate to the same name and ordered by position
        Debug.Assert(!preDeclarations.Any() || preDeclarations.All(d => d.Name == preDeclarations.First().Name));
        Debug.Assert(preDeclarations.Select(d => d.Position).IsOrdered());

        var result = ImmutableArray.CreateBuilder<Declaration>();
        foreach (var preDeclsAtPosition in preDeclarations.GroupBy(d => d.Position))
        {
            // Merge
            var merged = MergeDeclarationsInGroup(db, preDeclsAtPosition, declarations);
            // Add to the results
            result.Add(merged);
        }

        return new(result.ToImmutable());
    }

    /// <summary>
    /// Constructs <see cref="ISymbol"/>s from a group of <see cref="PreDeclaration"/>s that happen to be under the same position.
    /// This method merges symbols that can be overloaded.
    /// The illegal overloads (like trying to overload global variables) are also ruled out here.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="preDeclarations">The <see cref="PreDeclaration"/>s under the same position.</param>
    /// <param name="declarations">The declarations get written here mapped from the syntax to the individual unmerged symbols.</param>
    /// <returns>The <see cref="Declaration"/> that can be used in the <see cref="DeclarationTimeline"/>.</returns>
    private static Declaration MergeDeclarationsInGroup(
        QueryDatabase db,
        IEnumerable<PreDeclaration> preDeclarations,
        ImmutableDictionary<ParseTree, ISymbol>.Builder declarations)
    {
        Debug.Assert(preDeclarations.Any());

        var preDeclsList = preDeclarations.ToList();

        // Based on the first symbol we decide what we do
        if (preDeclsList[0].Kind == SymbolKind.Function)
        {
            // Possible overloading
            var overloadSet = ImmutableArray.CreateBuilder<ISymbol.IFunction>();
            // Look at all functions
            foreach (var preDecl in preDeclsList)
            {
                if (preDecl.Kind == SymbolKind.Function)
                {
                    // Add to the set
                    var symbol = ConstructDefinedSymbolOrNull(db, preDecl.Tree);
                    Debug.Assert(symbol is ISymbol.IFunction);
                    overloadSet.Add((ISymbol.IFunction)symbol);
                    declarations.Add(preDecl.Tree, symbol);
                }
                else
                {
                    // Error
                    // TODO
                    throw new NotImplementedException();
                }
            }
            // Look for overloads in the parent
            if (preDeclsList[0].Tree.Parent is not null)
            {
                var symbolInParent = ReferenceSymbolOrNull(db, preDeclsList[0].Tree.Parent!, preDeclsList[0].Name);
                if (symbolInParent is ISymbol.IFunction f)
                {
                    // One function, add it
                    overloadSet.Add(f);
                }
                else if (symbolInParent is ISymbol.IOverloadSet o)
                {
                    // Add all functions
                    overloadSet.AddRange(o.Functions);
                }
            }
            // Overload set done
            // If there was only one, unwrap
            var resultSymbol = overloadSet.Count == 1
                ? overloadSet[0] as ISymbol
                : ISymbol.SynthetizeOverloadSet(overloadSet.ToImmutable());
            return new(Position: preDeclsList[0].Position, Symbol: resultSymbol);
        }
        else
        {
            // Disallow overloading
            for (var i = 1; i < preDeclsList.Count; ++i)
            {
                // TODO: Error on the rest
                throw new NotImplementedException();
            }
            // Only the first one is considered legal
            var result = ConstructDefinedSymbolOrNull(db, preDeclsList[0].Tree);
            Debug.Assert(result is not null);
            declarations.Add(preDeclsList[0].Tree, result);
            return new(Position: preDeclsList[0].Position, Symbol: result);
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
        ISymbol? (tree) =>
        {
            var scopeDefiningAncestor = GetScopeDefiningAncestor(tree);
            Debug.Assert(scopeDefiningAncestor is not null);
            var scope = GetDefinedScopeOrNull(db, scopeDefiningAncestor);
            Debug.Assert(scope is not null);
            return scope.Declarations.TryGetValue(tree, out var symbol)
                ? symbol
                : null;
        });

    /// <summary>
    /// Constructs the <see cref="ISymbol"/> defined by <paramref name="tree"/>, or null, if
    /// it defines no symbol.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="ISymbol"/> defined by <paramref name="tree"/>, or null.</returns>
    private static ISymbol? ConstructDefinedSymbolOrNull(QueryDatabase db, ParseTree tree) => tree switch
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
    };

    /// <summary>
    /// Retrieves the <see cref="SymbolKind"/> and name defined by <paramref name="tree"/>, or null if it does not define a symbol.
    /// </summary>
    /// <param name="tree">The <see cref="ParseTree"/> that is asked for the defined <see cref="ISymbol"/>.</param>
    /// <returns>The <see cref="SymbolKind"/> and name defined by <paramref name="tree"/>, or null.</returns>
    private static (SymbolKind Kind, string Name)? GetDefinedSymbolKindAndName(ParseTree tree) => tree switch
    {
        ParseTree.Decl.Variable var => (SymbolKind.Variable, var.Identifier.Text),
        ParseTree.Decl.Func func => (SymbolKind.Function, func.Identifier.Text),
        ParseTree.Decl.Label lbl => (SymbolKind.Label, lbl.Identifier.Text),
        ParseTree.FuncParam param => (SymbolKind.Parameter, param.Identifier.Text),
        _ => null,
    };

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
    /// <param name="symbolKind">The <see cref="SymbolKind"/> that introduces a binding.</param>
    /// <returns>The <see cref="BindingKind"/> of <paramref name="symbolKind"/> declared in <paramref name="scopeKind"/>.</returns>
    private static BindingKind GetBindingKind(ScopeKind scopeKind, SymbolKind symbolKind) => symbolKind switch
    {
        SymbolKind.Label or SymbolKind.Function => BindingKind.OrderIndependent,
        SymbolKind.Parameter => BindingKind.OrderIndependent,
        SymbolKind.Variable when scopeKind == ScopeKind.Global => BindingKind.OrderIndependent,
        SymbolKind.Variable => BindingKind.NonRecursive,
        _ => throw new ArgumentOutOfRangeException(nameof(symbolKind)),
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
