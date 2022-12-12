using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Types;

/// <summary>
/// The result of local type-inference.
/// </summary>
/// <param name="Symbols">The inferred type of symbols.</param>
/// <param name="Expressions">The inferred type of expressions.</param>
internal readonly record struct TypeInferenceResult(
    IReadOnlyDictionary<ISymbol, Type> Symbols,
    IReadOnlyDictionary<ParseTree.Expr, Type> Expressions,
    ImmutableArray<Diagnostic> Diagnostics);

/// <summary>
/// A visitor that does type-inference on the given subtree.
/// </summary>
internal sealed class TypeInferenceVisitor : ParseTreeVisitorBase<Unit>
{
    private readonly record struct ReturnContext(
        Type ReturnType,
        ParseTree.TypeSpecifier? Specifier);

    private TypeInferenceResult Result => new(
        Symbols: this.symbols.ToImmutableDictionary(kv => kv.Key, kv => this.RemoveSubstitutions(kv.Value)),
        Expressions: this.expressions.ToImmutableDictionary(kv => kv.Key, kv => this.RemoveSubstitutions(kv.Value)),
        Diagnostics: this.Diagnostics);

    public TypeInferenceResult PartialResult => new(
        Symbols: this.symbols,
        Expressions: this.expressions,
        Diagnostics: ImmutableArray<Diagnostic>.Empty);

    public ImmutableArray<Diagnostic> Diagnostics => this.solver.Diagnostics;

    private readonly ConstraintSolver solver = new();
    private readonly QueryDatabase db;

    private readonly Dictionary<ISymbol, Type> symbols = new();
    private readonly Dictionary<ParseTree.Expr, Type> expressions = new();

    private readonly Stack<ReturnContext> returnContextStack = new();

    public TypeInferenceVisitor(QueryDatabase db)
    {
        this.db = db;
    }

    public TypeInferenceResult Infer(ParseTree tree)
    {
        this.Visit(tree);
        this.solver.Solve();
        return this.Result;
    }

    /// <summary>
    /// Removes type variable substitutions.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to remove substitutions from.</param>
    /// <returns>The equivalent of <paramref name="type"/> without any variable substitutions.</returns>
    private Type RemoveSubstitutions(Type type) => type switch
    {
        Type.Builtin => type,
        Type.Variable v => this.UnwrapTypeVariable(v),
        Type.Error e => e,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private Type UnwrapTypeVariable(Type.Variable v)
    {
        var result = v.Substitution;
        if (result is Type.Variable)
        {
            // TODO: Not necessarily the defined symbol...
            var symbol = v.Defitition is null
                ? null
                : SymbolResolution.GetDefinedSymbolOrNull(this.db, v.Defitition);
            var diag = Diagnostic.Create(
                template: SemanticErrors.CouldNotInferType,
                location: v.Defitition is null ? Location.None : new Location.TreeReference(v.Defitition),
                formatArgs: symbol?.Name);
            return new Type.Error(ImmutableArray.Create(diag));
        }
        return result;
    }

    public override Unit VisitFuncDecl(ParseTree.Decl.Func node)
    {
        var context = new ReturnContext(
            ReturnType: node.ReturnType is null ? Type.Unit : TypeChecker.Evaluate(this.db, node.ReturnType.Type),
            Specifier: node.ReturnType);
        this.returnContextStack.Push(context);
        base.VisitFuncDecl(node);
        this.returnContextStack.Pop();

        return this.Default;
    }

    public override Unit VisitVariableDecl(ParseTree.Decl.Variable node)
    {
        // Inference in children
        base.VisitVariableDecl(node);

        // The symbol we are inferring the type for
        var symbol = SymbolResolution.GetDefinedSymbolOrNull(this.db, node);
        Debug.Assert(symbol is not null);

        // The declared type after the ':' and the value type after the '='
        var declaredType = node.Type is not null
            ? TypeChecker.Evaluate(this.db, node.Type.Type)
            : null;
        var valueType = node.Initializer is not null
            ? TypeChecker.TypeOf(this.db, node.Initializer.Value)
            : null;

        // Infer the type from the two potential sources
        var inferredType = null as Type;
        if (declaredType is null && valueType is null)
        {
            // var x;
            // Just a new type variable, will need to infer from context
            inferredType = new Type.Variable(node);
        }
        else if (declaredType is null || valueType is null)
        {
            // var x: T;
            // var x = v;
            // Whatever is non-null
            inferredType = declaredType ?? valueType;
        }
        else
        {
            // var x: T = v;
            // TODO: Need to put a constraint that valueType is subtype of declaredType
            inferredType = declaredType;
            // TODO: Not the right constraint, we need "Assignable"
            this.solver.Same(declaredType, valueType).ConfigureDiagnostic(diag => diag
                .WithLocation(new Location.TreeReference(node.Initializer!.Value))
                .AddRelatedInformation(
                    format: "the variable type vas declared to be {0} here",
                    formatArgs: declaredType,
                    location: new Location.TreeReference(node.Type!.Type)));
        }

        // Store the inferred type
        Debug.Assert(inferredType is not null);
        this.symbols[symbol] = inferredType;

        return this.Default;
    }

    public override Unit VisitInlineBodyFuncBody(ParseTree.FuncBody.InlineBody node)
    {
        // Inference in children
        base.VisitInlineBodyFuncBody(node);

        // TODO: Case where return type is unit but the expr is not?
        var exprType = TypeChecker.TypeOf(this.db, node.Expression);

        Debug.Assert(this.returnContextStack.Count > 0);
        var returnContext = this.returnContextStack.Peek();

        // TODO: Not the right constraint, we likely need something like "Assignable" here
        var promise = this.solver.Same(returnContext.ReturnType, exprType).ConfigureDiagnostic(diag => diag
            .WithLocation(new Location.TreeReference(node.Expression)));
        if (returnContext.Specifier is not null)
        {
            promise.ConfigureDiagnostic(diag => diag
                .AddRelatedInformation(
                    format: "the return type was specified to be {0} here",
                    formatArgs: returnContext.ReturnType,
                    location: new Location.TreeReference(returnContext.Specifier.Type)));
        }

        return this.Default;
    }

    public override Unit VisitReturnExpr(ParseTree.Expr.Return node)
    {
        // Inference in children
        base.VisitReturnExpr(node);

        var exprType = node.Expression is null ? Type.Unit : TypeChecker.TypeOf(this.db, node.Expression);

        Debug.Assert(this.returnContextStack.Count > 0);
        var returnContext = this.returnContextStack.Peek();

        // TODO: Not the right constraint, we likely need something like "Assignable" here
        var promise = this.solver.Same(returnContext.ReturnType, exprType).ConfigureDiagnostic(diag => diag
            .WithLocation(new Location.TreeReference(node)));
        if (returnContext.Specifier is not null)
        {
            promise.ConfigureDiagnostic(diag => diag
                .AddRelatedInformation(
                    format: "the return type was specified to be {0} here",
                    formatArgs: returnContext.ReturnType,
                    location: new Location.TreeReference(returnContext.Specifier.Type)));
        }

        return this.Default;
    }

    public override Unit VisitUnaryExpr(ParseTree.Expr.Unary node)
    {
        // Inference in children
        base.VisitUnaryExpr(node);

        var subexprType = TypeChecker.TypeOf(this.db, node.Operand);

        if (node.Operator.Type == TokenType.KeywordNot)
        {
            // Must be boolean
            this.solver.Same(subexprType, Type.Bool).ConfigureDiagnostic(diag => diag
                .WithLocation(new Location.TreeReference(node.Operand)));
            this.expressions[node] = Type.Bool;
        }
        else
        {
            // TODO: Validate operand
            this.expressions[node] = subexprType;
        }

        return this.Default;
    }

    public override Unit VisitBinaryExpr(ParseTree.Expr.Binary node)
    {
        // Inference in children
        base.VisitBinaryExpr(node);

        var leftType = TypeChecker.TypeOf(this.db, node.Left);
        var rightType = TypeChecker.TypeOf(this.db, node.Right);

        if (node.Operator.Type == TokenType.Assign)
        {
            // Right has to be assignable to left
            // TODO: Wrong constraint, we need "Assignable"
            var promise = this.solver.Same(leftType, rightType).ConfigureDiagnostic(diag => diag
                .WithLocation(new Location.TreeReference(node.Right)));

            // Optionally append where the variable was declared
            var leftSymbol = SymbolResolution.GetReferencedSymbolOrNull(this.db, node.Left);
            if (leftSymbol is ISymbol.IVariable varSymbol && varSymbol.Definition is not null)
            {
                promise.ConfigureDiagnostic(diag => diag
                    .AddRelatedInformation(
                        format: "the variable was declared to be {0} here",
                        formatArgs: leftType,
                        location: new Location.TreeReference(varSymbol.Definition)));
            }

            this.expressions[node] = promise.Result;
        }
        else if (node.Operator.Type is TokenType.KeywordAnd or TokenType.KeywordOr)
        {
            // We require the sides to be of boolean type
            this.solver.Same(leftType, Type.Bool).ConfigureDiagnostic(diag => diag
                .WithLocation(new Location.TreeReference(node.Left)));
            this.solver.Same(rightType, Type.Bool).ConfigureDiagnostic(diag => diag
                .WithLocation(new Location.TreeReference(node.Right)));
        }
        else
        {
            // TODO: Temporary
            var resultType = this.solver.Same(leftType, rightType).Result;
            this.expressions[node] = resultType;
        }

        return this.Default;
    }

    public override Unit VisitIfExpr(ParseTree.Expr.If node)
    {
        // Inference in children
        base.VisitIfExpr(node);

        // Check if condition is bool
        var conditionType = TypeChecker.TypeOf(this.db, node.Condition.Value);
        this.solver.Same(conditionType, Type.Bool).ConfigureDiagnostic(diag => diag
            .WithLocation(new Location.TreeReference(node.Condition.Value)));

        var resultType = null as Type;
        var leftType = TypeChecker.TypeOf(this.db, node.Then);
        if (node.Else is null)
        {
            // Then part has to be of unit
            this.solver.Same(leftType, Type.Unit)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(ExtractReturnLocation(node.Then))
                    .AddRelatedInformation("an if-expression without an else branch must result in unit"));
            resultType = leftType;
        }
        else
        {
            var rightType = TypeChecker.TypeOf(this.db, node.Else.Expression);
            // TODO: Wrong constraint, we need "Common ancestor"
            resultType = this.solver.Same(leftType, rightType)
                .ConfigureDiagnostic(diags => diags
                    .WithLocation(ExtractReturnLocation(node.Else.Expression))
                    .AddRelatedInformation(
                        format: "the other branch was inferred to be {0} here",
                        formatArgs: new[] { leftType },
                        location: ExtractReturnLocation(node.Then)))
                .Result;
        }
        this.expressions[node] = resultType;

        return this.Default;
    }

    public override Unit VisitWhileExpr(ParseTree.Expr.While node)
    {
        // Inference in children
        base.VisitWhileExpr(node);

        var conditionType = TypeChecker.TypeOf(this.db, node.Condition.Value);
        this.solver.Same(conditionType, Type.Bool).ConfigureDiagnostic(diag => diag
            .WithLocation(new Location.TreeReference(node.Condition.Value)));

        return this.Default;
    }

    public override Unit VisitCallExpr(ParseTree.Expr.Call node)
    {
        // Inference in children
        base.VisitCallExpr(node);

        var calledType = TypeChecker.TypeOf(this.db, node.Called);
        var argsType = node.Args.Value.Elements
            .Select(a => TypeChecker.TypeOf(this.db, a.Value))
            .ToImmutableArray();
        // TODO: We want a "callable" constraint here, but for now we'll go around a bit
        var clientType = new Type.Function(argsType, new Type.Variable(null));
        var returnType = ((Type.Function)this.solver.Same(calledType, clientType).Result).Return;
        this.expressions[node] = returnType;

        return this.Default;
    }

    // TODO:
    //  - relational
    //  - index
    //  - member access

    private static Location ExtractReturnLocation(ParseTree.Expr expr) =>
        new Location.TreeReference(ExtractReturnExpression(expr));

    private static ParseTree.Expr ExtractReturnExpression(ParseTree.Expr expr) => expr switch
    {
        ParseTree.Expr.If @if => ExtractReturnExpression(@if.Then),
        ParseTree.Expr.While @while => ExtractReturnExpression(@while.Expression),
        ParseTree.Expr.Block block => block.Enclosed.Value.Value ?? block,
        _ => expr,
    };
}
