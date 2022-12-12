using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;

namespace Draco.Compiler.Internal.Semantics.Types;

/// <summary>
/// Implements type-checking.
/// </summary>
internal static class TypeChecker
{
    /// <summary>
    /// Retrieves the <see cref="Diagnostic"/> messages relating to types.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to check.</param>
    /// <returns>The <see cref="Diagnostic"/>s related to <paramref name="tree"/>.</returns>
    public static IEnumerable<Diagnostic> GetDiagnostics(QueryDatabase db, ParseTree tree)
    {
        if (tree is ParseTree.Expr expr)
        {
            var ty = TypeOf(db, expr);
            return ty.Diagnostics;
        }
        else if (tree is ParseTree.Decl.Variable)
        {
            var symbol = SymbolResolution.GetDefinedSymbolOrNull(db, tree);
            if (symbol is null) return Enumerable.Empty<Diagnostic>();
            var type = TypeOf(db, symbol);
            return type.Diagnostics;
        }
        else if (tree is ParseTree.Decl.Func)
        {
            var scope = SymbolResolution.GetDefinedScopeOrNull(db, tree) ?? throw new InvalidOperationException();
            var definition = scope.Definition;
            Debug.Assert(definition is not null);
            var result = InferLocalTypes(db, definition);
            return result.Diagnostics;
        }
        else
        {
            // TODO: Do we need to consider anything else explicitly?
            return Enumerable.Empty<Diagnostic>();
        }
    }

    /// <summary>
    /// Evaluates the given type expression node to the compiler representation of a type.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ParseTree.TypeExpr"/> to evaluate.</param>
    /// <returns>The <see cref="Type"/> representation of <paramref name="expr"/>.</returns>
    public static Type Evaluate(QueryDatabase db, ParseTree.TypeExpr expr) => db.GetOrUpdate(
        expr,
        Type (expr) => expr switch
        {
            // TODO: This is a temporary solution
            // Later, we'll need symbol resolution to be able to reference type-symbols only and such
            // For now this is a simple, greedy workaround
            ParseTree.TypeExpr.Name namedType => SymbolResolution.GetReferencedSymbol(db, namedType) is ISymbol.ITypeDefinition typeDef
                ? typeDef.DefinedType
                : throw new InvalidOperationException(),
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        });

    /// <summary>
    /// Determines the type of an expression node.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ParseTree.Expr"/> to determine the type of.</param>
    /// <returns>The <see cref="Type"/> of <paramref name="expr"/>.</returns>
    public static Type TypeOf(QueryDatabase db, ParseTree.Expr expr) => expr switch
    {
        ParseTree.Expr.Literal lit => lit.Value.Type switch
        {
            TokenType.LiteralInteger => Type.Int32,
            TokenType.KeywordTrue or TokenType.KeywordFalse => Type.Bool,
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        },
        ParseTree.Expr.String => Type.String,
        ParseTree.Expr.Block block => block.Enclosed.Value.Value is null
            ? Type.Unit
            : TypeOf(db, block.Enclosed.Value.Value),
        ParseTree.Expr.Name name => TypeOf(db, SymbolResolution.GetReferencedSymbol(db, name)),
        ParseTree.Expr.If @if => GetTypeOfLocal(db, @if),
        ParseTree.Expr.Unary ury => GetTypeOfLocal(db, ury),
        ParseTree.Expr.Binary bin => GetTypeOfLocal(db, bin),
        ParseTree.Expr.Call call => GetTypeOfLocal(db, call),
        // TODO: Type errors?
        ParseTree.Expr.Relational => Type.Bool,
        // TODO: Type errors?
        ParseTree.Expr.While => Type.Unit,
        ParseTree.Expr.UnitStmt => Type.Unit,
        // TODO: Type errors?
        ParseTree.Expr.Return => Type.Unit,
        _ => throw new ArgumentOutOfRangeException(nameof(expr)),
    };

    /// <summary>
    /// Retrieves the <see cref="Type"/> of a <see cref="ISymbol"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="symbol">The <see cref="ISymbol"/> to get the <see cref="Type"/> of.</param>
    /// <returns>The <see cref="Type"/> of <paramref name="symbol"/>.</returns>
    public static Type TypeOf(QueryDatabase db, ISymbol symbol)
    {
        if (symbol.IsError) return Type.Error.Empty;
        if (symbol is not ISymbol.ITyped typed) throw new InvalidOperationException();
        return db.GetOrUpdate(
            typed,
            Type (typed) =>
            {
                if (typed.Definition is null) return typed.Type;
                if (typed.IsGlobal)
                {
                    var definition = typed.Definition;
                    Debug.Assert(definition is not null);
                    var inferenceResult = InferLocalTypes(db, definition);
                    return inferenceResult.Symbols[typed];
                }
                else
                {
                    var definingFunc = typed.DefiningFunction;
                    Debug.Assert(definingFunc is not null);
                    Debug.Assert(definingFunc.Definition is not null);
                    var inferenceResult = InferLocalTypes(db, definingFunc.Definition);
                    return inferenceResult.Symbols[typed];
                }
            });
    }

    /// <summary>
    /// Retrieves the <see cref="Type"/> of a local <see cref="ParseTree.Expr"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ParseTree.Expr"/> to get the <see cref="Type"/> of.</param>
    /// <returns>The <see cref="Type"/> of <paramref name="expr"/>.</returns>
    private static Type GetTypeOfLocal(QueryDatabase db, ParseTree.Expr expr)
    {
        var scope = GetInferrableAncestor(db, expr);
        // TODO: Not necessarily a variable
        // Infer the variables from the scope
        var inferenceResult = InferLocalTypes(db, scope);
        return inferenceResult.Expressions[expr];
    }

    /// <summary>
    /// Infers the <see cref="Type"/>s of entities in a given function <see cref="IScope"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The subtree to infer types in.</param>
    /// <returns>The result of type inference.</returns>
    private static TypeInferenceResult InferLocalTypes(QueryDatabase db, ParseTree tree) => db.GetOrUpdate(
        args: tree,
        createContext: () => new TypeInferenceVisitor(db),
        recompute: (visitor, tree) => visitor.Infer(tree),
        handleCycle: (visitor, tree) => visitor.PartialResult);

    /// <summary>
    /// Retrieves the <see cref="ParseTree"/> ancestor that can be used for type inference for <paramref name="tree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="ParseTree"/> to get the ancestor of.</param>
    /// <returns>The inferrable ancestor of <paramref name="tree"/>.</returns>
    private static ParseTree GetInferrableAncestor(QueryDatabase db, ParseTree tree)
    {
        var scope = SymbolResolution.GetContainingScopeOrNull(db, tree);
        Debug.Assert(scope is not null);
        // Walk up to the nearest scope that's either global or function
        while (scope.Kind != ScopeKind.Global && scope.Kind != ScopeKind.Function)
        {
            scope = scope.Parent ?? throw new InvalidOperationException();
        }
        Debug.Assert(scope.Definition is not null);
        return scope.Definition;
    }
}
