using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Lowering;

/// <summary>
/// Performs local rewrites of the source code.
/// </summary>
internal partial class LocalRewriter : BoundTreeRewriter
{
    /// <summary>
    /// Represents a value that was temporarily stored.
    /// </summary>
    /// <param name="Symbol">The synthetized local symbol.</param>
    /// <param name="Reference">The expression referencing the stored temporary.</param>
    /// <param name="Assignment">The assignment that stores the temporary.</param>
    private readonly record struct TemporaryStorage(
        LocalSymbol? Symbol,
        BoundExpression Reference,
        BoundStatement Assignment);

    /// <summary>
    /// The singleton instance to use.
    /// </summary>
    public static LocalRewriter Instance { get; } = new();

    private LocalRewriter()
    {
    }

    public override BoundNode VisitWhileExpression(BoundWhileExpression node)
    {
        // while (condition)
        // {
        //     body...
        // }
        //
        // =>
        //
        // continue_label:
        //     if (!condition) goto break_label;
        //     body...
        //     goto continue_label;
        // break_label:

        var condition = (BoundExpression)node.Condition.Accept(this);
        var body = (BoundExpression)node.Then.Accept(this);

        return BlockExpression(
            locals: ImmutableArray<LocalSymbol>.Empty,
            statements: ImmutableArray.Create<BoundStatement>(
                LabelStatement(node.ContinueLabel),
                ConditionalGotoStatement(
                    condition: UnaryExpression(
                        @operator: Intrinsics.Bool_Not,
                        operand: condition,
                        type: Types.Intrinsics.Bool),
                    target: node.BreakLabel),
                ExpressionStatement(body),
                ExpressionStatement(GotoExpression(node.ContinueLabel)),
                LabelStatement(node.BreakLabel)),
            value: BoundUnitExpression.Default);
    }

    public override BoundNode VisitRelationalExpression(BoundRelationalExpression node)
    {
        // expr1 < expr2 == expr3 > expr4 != ...
        //
        // =>
        //
        // {
        //     val tmp1 = expr1;
        //     val tmp2 = expr2;
        //     val tmp3 = expr3;
        //     val tmp4 = expr4;
        //     ...
        //     tmp1 < tmp2 && tmp2 == tmp3 && tmp3 > tmp4 && tmp4 != ...
        // }

        // Store all expressions as temporary variables
        var tmpVariables = new List<TemporaryStorage>
        {
            this.StoreTemporary(node.First)
        };
        foreach (var item in node.Comparisons) tmpVariables.Add(this.StoreTemporary(item.Next));

        // Build pairs of comparisons from symbol references
        var comparisons = new List<BoundExpression>();
        for (var i = 0; i < node.Comparisons.Length; ++i)
        {
            var left = tmpVariables[i].Reference;
            var op = node.Comparisons[i].Operator;
            var right = tmpVariables[i + 1].Reference;
            comparisons.Add(BinaryExpression(
                left: left,
                @operator: op,
                right: right,
                type: Types.Intrinsics.Bool));
        }

        // Fold them into conjunctions
        var conjunction = comparisons.Aggregate(AndExpression);
        // Desugar them, conjunctions can be desugared too
        conjunction = (BoundExpression)conjunction.Accept(this);

        // Wrap up in block
        return BlockExpression(
            locals: tmpVariables
                .Select(tmp => tmp.Symbol)
                .OfType<LocalSymbol>()
                .ToImmutableArray(),
            statements: tmpVariables.Select(t => t.Assignment).ToImmutableArray(),
            value: conjunction);
    }

    public override BoundNode VisitAndExpression(BoundAndExpression node)
    {
        // expr1 and expr2
        //
        // =>
        //
        // if (expr1) expr2 else false

        var left = (BoundExpression)node.Left.Accept(this);
        var right = (BoundExpression)node.Right.Accept(this);

        return IfExpression(
            condition: left,
            then: right,
            @else: LiteralExpression(false),
            type: Types.Intrinsics.Bool);
    }

    public override BoundNode VisitOrExpression(BoundOrExpression node)
    {
        // expr1 or expr2
        //
        // =>
        //
        // if (expr1) true else expr2

        var left = (BoundExpression)node.Left.Accept(this);
        var right = (BoundExpression)node.Right.Accept(this);

        return IfExpression(
            condition: left,
            then: LiteralExpression(true),
            @else: right,
            type: Types.Intrinsics.Bool);
    }

    // Utility to store an expression to a temporary variable
    private TemporaryStorage StoreTemporary(BoundExpression expr)
    {
        // Optimization: if it's already a symbol reference, leave as-is
        // Optimization: if it's a literal, don't bother copying
        if (expr is BoundLocalExpression
                 or BoundParameterExpression
                 or BoundLiteralExpression)
        {
            return new(null, expr, BoundNoOpStatement.Default);
        }

        // Otherwise compute and store
        var symbol = new SynthetizedLocalSymbol(expr.TypeRequired);
        var symbolRef = LocalExpression(symbol);
        var assignment = LocalDeclaration(
            local: symbol,
            value: (BoundExpression)expr.Accept(this));
        return new(symbol, symbolRef, assignment);
    }
}
