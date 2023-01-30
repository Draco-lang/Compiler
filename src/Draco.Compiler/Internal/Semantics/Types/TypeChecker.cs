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
    /// <param name="tree">The <see cref="SyntaxNode"/> to check.</param>
    /// <returns>The <see cref="Diagnostic"/>s related to <paramref name="tree"/>.</returns>
    public static IEnumerable<Diagnostic> GetDiagnostics(QueryDatabase db, SyntaxNode tree)
    {
        if (tree is ExpressionSyntax expr)
        {
            // TODO: Any better way to handle this?
            // Maybe allow TypeOf to return a nullable?
            var referencedSymbol = SymbolResolution.GetReferencedSymbolOrNull(db, tree);
            if (referencedSymbol is not null && referencedSymbol is not ISymbol.ITyped)
            {
                // We don't consider it, asking its type is not valid
                return Enumerable.Empty<Diagnostic>();
            }
            var ty = TypeOf(db, expr);
            return ty.Diagnostics;
        }
        else if (tree is VariableDeclarationSyntax)
        {
            var symbol = SymbolResolution.GetDefinedSymbolOrNull(db, tree);
            if (symbol is null) return Enumerable.Empty<Diagnostic>();
            var type = TypeOf(db, symbol);
            return type.Diagnostics;
        }
        else if (tree is FunctionDeclarationSyntax)
        {
            var scope = SymbolResolution.GetDefinedScopeOrNull(db, tree) ?? throw new InvalidOperationException();
            var definition = scope.Definition;
            Debug.Assert(definition is not null);
            var result = InferLocalTypes(db, definition!);
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
    /// <param name="expr">The <see cref="TypeSyntax"/> to evaluate.</param>
    /// <returns>The <see cref="Type"/> representation of <paramref name="expr"/>.</returns>
    public static Type Evaluate(QueryDatabase db, TypeSyntax expr) => db.GetOrUpdate(
        expr,
        Type (expr) => expr switch
        {
            UnexpectedTypeSyntax => Type.Error.Empty,
            NameTypeSyntax namedType => SymbolResolution.GetReferencedSymbol(db, namedType) switch
            {
                ISymbol.ITypeDefinition typeDef => typeDef.DefinedType,
                var symbol when symbol.IsError => new Type.Error(symbol.Diagnostics),
                _ => throw new ArgumentOutOfRangeException(nameof(expr)),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        });

    /// <summary>
    /// Determines the type of an expression node.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ExpressionSyntax"/> to determine the type of.</param>
    /// <returns>The <see cref="Type"/> of <paramref name="expr"/>.</returns>
    public static Type TypeOf(QueryDatabase db, ExpressionSyntax expr) => expr switch
    {
        UnexpectedExpressionSyntax => Type.Error.Empty,
        GroupingExpressionSyntax g => TypeOf(db, g.Expression),
        LiteralExpressionSyntax lit => lit.Literal.Type switch
        {
            TokenType.LiteralInteger => Type.Int32,
            // NOTE: There is no agreement currently on float literal type
            TokenType.LiteralFloat => Type.Float64,
            TokenType.KeywordTrue or TokenType.KeywordFalse => Type.Bool,
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        },
        StringExpressionSyntax => Type.String,
        BlockExpressionSyntax block => block.Value is null
            ? Type.Unit
            : TypeOf(db, block.Value),
        NameExpressionSyntax name => TypeOf(db, SymbolResolution.GetReferencedSymbol(db, name)),
        IfExpressionSyntax @if => GetTypeOfLocal(db, @if),
        UnaryExpressionSyntax ury => GetTypeOfLocal(db, ury),
        BinaryExpressionSyntax bin => GetTypeOfLocal(db, bin),
        CallExpressionSyntax call => GetTypeOfLocal(db, call),
        RelationalExpressionSyntax => Type.Bool,
        WhileExpressionSyntax => Type.Unit,
        StatementExpressionSyntax => Type.Unit,
        ReturnExpressionSyntax => Type.Never_,
        GotoExpressionSyntax => Type.Never_,
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
                if (typed is ISymbol.IFunction func) return func.Type;
                if (typed is ISymbol.IParameter)
                {
                    var definition = (ParameterSyntax)typed.Definition;
                    return Evaluate(db, definition.Type);
                }
                if (typed.IsGlobal)
                {
                    var definition = typed.Definition;
                    Debug.Assert(definition is not null);
                    var inferenceResult = InferLocalTypes(db, definition!);
                    return inferenceResult.Symbols[typed];
                }
                else
                {
                    var definingFunc = typed.DefiningFunction;
                    Debug.Assert(definingFunc is not null);
                    Debug.Assert(definingFunc!.Definition is not null);
                    var inferenceResult = InferLocalTypes(db, definingFunc.Definition!);
                    return inferenceResult.Symbols[typed];
                }
            });
    }

    /// <summary>
    /// Retrieves the <see cref="Type"/> of a local <see cref="ExpressionSyntax"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ExpressionSyntax"/> to get the <see cref="Type"/> of.</param>
    /// <returns>The <see cref="Type"/> of <paramref name="expr"/>.</returns>
    private static Type GetTypeOfLocal(QueryDatabase db, ExpressionSyntax expr)
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
    private static TypeInferenceResult InferLocalTypes(QueryDatabase db, SyntaxNode tree) => db.GetOrUpdate(
        args: tree,
        createContext: () => new TypeInferenceVisitor(db),
        recompute: (visitor, tree) => visitor.Infer(tree),
        handleCycle: (visitor, tree) => visitor.PartialResult);

    /// <summary>
    /// Retrieves the <see cref="SyntaxNode"/> ancestor that can be used for type inference for <paramref name="tree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="tree">The <see cref="SyntaxNode"/> to get the ancestor of.</param>
    /// <returns>The inferrable ancestor of <paramref name="tree"/>.</returns>
    private static SyntaxNode GetInferrableAncestor(QueryDatabase db, SyntaxNode tree)
    {
        while (true)
        {
            var definedSymbol = SymbolResolution.GetDefinedSymbolOrNull(db, tree);
            if (definedSymbol is ISymbol.IFunction) break;
            if (definedSymbol is ISymbol.IVariable v && v.IsGlobal) break;
            Debug.Assert(tree.Parent is not null);
            tree = tree.Parent!;
        }
        return tree;
    }
}
