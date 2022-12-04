using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using static Draco.Compiler.Api.Diagnostics.Location;

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
            InferLocalTypes(db, scope);
            // TODO: Diags produced during inference?
            return Enumerable.Empty<Diagnostic>();
        }
        else
        {
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
            ParseTree.TypeExpr.Name namedType => SymbolResolution.GetReferencedSymbol(db, namedType) is Symbol.TypeAlias typeAlias
                ? typeAlias.Type
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
        ParseTree.Expr.Binary bin => GetTypeOfLocal(db, bin),
        ParseTree.Expr.Call call => GetTypeOfLocal(db, call),
        // TODO: Type errors?
        ParseTree.Expr.Relational => Type.Bool,
        // TODO: Type errors?
        ParseTree.Expr.While => Type.Unit,
        ParseTree.Expr.UnitStmt => Type.Unit,
        _ => throw new ArgumentOutOfRangeException(nameof(expr)),
    };

    /// <summary>
    /// Retrieves the <see cref="Type"/> of a <see cref="Symbol"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="symbol">The <see cref="Symbol"/> to get the <see cref="Type"/> of.</param>
    /// <returns>The <see cref="Type"/> of <paramref name="symbol"/>.</returns>
    public static Type TypeOf(QueryDatabase db, Symbol symbol)
    {
        if (symbol is Symbol.Variable)
        {
            // TODO: Have some sensible refactoring for this or utility in SymbolResolution?
            // Maybe even the ability to ask the declaring function from the variable symbol or something?
            // Walk up to the nearest scope that's either global or function
            var scope = symbol.EnclosingScope ?? throw new InvalidOperationException();
            while (scope.Kind != ScopeKind.Global && scope.Kind != ScopeKind.Function)
            {
                scope = scope.Parent ?? throw new InvalidOperationException();
            }
            // TODO: Not necessarily a variable
            // Infer the variables from the scope
            var inferenceResult = InferLocalTypes(db, scope);
            return inferenceResult.Symbols[symbol];
        }
        else if (symbol is Symbol.Parameter)
        {
            var definition = (ParseTree.FuncParam)symbol.Definition!;
            return Evaluate(db, definition.Type.Type);
        }
        else if (symbol is Symbol.Function)
        {
            var definition = (ParseTree.Decl.Func)symbol.Definition!;
            var paramTypes = definition.Params.Value.Elements
                .Select(p => Evaluate(db, p.Value.Type.Type))
                .ToImmutableArray();
            var returnType = definition.ReturnType is null ? Type.Unit : Evaluate(db, definition.ReturnType.Type);
            return new Type.Function(paramTypes, returnType);
        }
        else if (symbol is Symbol.Intrinsic intrinsic)
        {
            return intrinsic.Type;
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Retrieves the <see cref="Type"/> of a local <see cref="ParseTree.Expr"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ParseTree.Expr"/> to get the <see cref="Type"/> of.</param>
    /// <returns>The <see cref="Type"/> of <paramref name="expr"/>.</returns>
    private static Type GetTypeOfLocal(QueryDatabase db, ParseTree.Expr expr)
    {
        var scope = SymbolResolution.GetContainingScopeOrNull(db, expr) ?? throw new InvalidOperationException();
        // TODO: Duplicate
        // TODO: Have some sensible refactoring for this or utility in SymbolResolution?
        // Maybe even the ability to ask the declaring function from the variable symbol or something?
        // Walk up to the nearest scope that's either global or function
        while (scope.Kind != ScopeKind.Global && scope.Kind != ScopeKind.Function)
        {
            scope = scope.Parent ?? throw new InvalidOperationException();
        }
        // TODO: Not necessarily a variable
        // Infer the variables from the scope
        var inferenceResult = InferLocalTypes(db, scope);
        return inferenceResult.Expressions[expr];
    }

    /// <summary>
    /// Infers the <see cref="Type"/>s of entities in a given function <see cref="Scope"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="scope">The function <see cref="Scope"/> to infer types for.</param>
    /// <returns>The result of type inference.</returns>
    private static TypeInferenceResult InferLocalTypes(QueryDatabase db, Scope scope)
    {
        Debug.Assert(scope.Kind == ScopeKind.Function);
        Debug.Assert(scope.Definition is not null);

        return db.GetOrUpdate(
            args: scope,
            createContext: () =>
            {
                var definition = (ParseTree.Decl.Func)scope.Definition;
                var defType = definition.ReturnType is null ? Type.Unit : Evaluate(db, definition.ReturnType.Type);
                return new TypeInferenceVisitor(db, defType);
            },
            recompute: (visitor, scope) =>
            {
                visitor.Visit(scope.Definition!);
                return visitor.Result;
            },
            handleCycle: (visitor, scope) => visitor.PartialResult);
    }
}
