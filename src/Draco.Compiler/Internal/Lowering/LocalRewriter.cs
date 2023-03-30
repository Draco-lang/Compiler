using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Types;
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

    public override BoundNode VisitBlockExpression(BoundBlockExpression node)
    {
        // We only keep useful statements
        var statements = node.Statements
            .Select(s => s.Accept(this))
            .Cast<BoundStatement>()
            .Where(s => !IsUseless(s))
            .ToImmutableArray();
        var value = (BoundExpression)node.Value.Accept(this);

        // If the node became empty, we can erase it
        if (node.Locals.Length == 0 && statements.Length == 0 && IsUseless(value))
        {
            // Useless block, erase it
            return BoundUnitExpression.Default;
        }
        else
        {
            // Just update it
            return node.Update(node.Locals, statements, value);
        }
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

        var result = BlockExpression(
            locals: ImmutableArray<LocalSymbol>.Empty,
            statements: ImmutableArray.Create<BoundStatement>(
                LabelStatement(node.ContinueLabel),
                ConditionalGotoStatement(
                    condition: UnaryExpression(
                        @operator: IntrinsicSymbols.Bool_Not,
                        operand: condition,
                        type: IntrinsicTypes.Bool),
                    target: node.BreakLabel),
                ExpressionStatement(body),
                ExpressionStatement(GotoExpression(node.ContinueLabel)),
                LabelStatement(node.BreakLabel)),
            value: BoundUnitExpression.Default);
        // Blocks can be desugared too, pass through
        return result.Accept(this);
    }

    public override BoundNode VisitRelationalExpression(BoundRelationalExpression node)
    {
        // In case there are only two operands, don't do any of the optimizations below
        if (node.Comparisons.Length == 1)
        {
            var left = (BoundExpression)node.First.Accept(this);
            var right = (BoundExpression)node.Comparisons[0].Next.Accept(this);
            return BinaryExpression(
                left: left,
                @operator: node.Comparisons[0].Operator,
                right: right,
                type: IntrinsicTypes.Bool);
        }

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
                type: IntrinsicTypes.Bool));
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
            type: IntrinsicTypes.Bool);
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
            type: IntrinsicTypes.Bool);
    }

    // Utility to store an expression to a temporary variable
    private TemporaryStorage StoreTemporary(BoundExpression expr)
    {
        // Optimization: if it's already a symbol reference, leave as-is
        // Optimization: if it's a literal, don't bother copying
        if (expr is BoundLocalExpression
                 or BoundGlobalExpression
                 or BoundParameterExpression
                 or BoundFunctionExpression
                 or BoundLiteralExpression)
        {
            return new(null, expr, BoundNoOpStatement.Default);
        }

        // Otherwise compute and store
        var symbol = new SynthetizedLocalSymbol(expr.TypeRequired, false);
        var symbolRef = LocalExpression(symbol);
        var assignment = LocalDeclaration(
            local: symbol,
            value: (BoundExpression)expr.Accept(this));
        return new(symbol, symbolRef, assignment);
    }

    private static bool IsUseless(BoundStatement stmt) => stmt switch
    {
        BoundExpressionStatement s => IsUseless(s.Expression),
        BoundNoOpStatement => true,
        _ => false,
    };

    private static bool IsUseless(BoundExpression expr) => expr switch
    {
        BoundBlockExpression block => block.Locals.Length == 0
                                   && block.Statements.All(IsUseless)
                                   && IsUseless(block.Value),
        BoundUnitExpression => true,
        _ => false,
    };
}
