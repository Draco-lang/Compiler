using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Api;
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

    private WellKnownTypes WellKnownTypes => this.compilation.WellKnownTypes;

    private readonly Compilation compilation;

    public LocalRewriter(Compilation compilation)
    {
        this.compilation = compilation;
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

    public override BoundNode VisitIfExpression(BoundIfExpression node)
    {
        // if (condition) then_expr else else_expr
        //
        // =>
        //
        // {
        //     var result;
        //     if (condition) goto then_label;
        //     goto else_label;
        //     then_label:
        //         result = then_expr;
        //         goto finally_label;
        //     else_label:
        //         result = else_expr;
        //     finally_label:
        //     @sequence point
        //     result
        // }
        //
        // NOTE: We are putting a sequence point after the finally label to erase
        // the previous one, so after evaluating the 'then' block and jumping to
        // 'finally', the debugger won't highlight the 'else' block by accident,
        // because the last sequence point was there

        var condition = (BoundExpression)node.Condition.Accept(this);
        var then = (BoundExpression)node.Then.Accept(this);
        var @else = (BoundExpression)node.Else.Accept(this);

        var result = new SynthetizedLocalSymbol(node.TypeRequired, true);
        var thenLabel = new SynthetizedLabelSymbol("then");
        var elseLabel = new SynthetizedLabelSymbol("else");
        var finallyLabel = new SynthetizedLabelSymbol("finally");

        return BlockExpression(
            locals: ImmutableArray.Create<LocalSymbol>(result),
            statements: ImmutableArray.Create<BoundStatement>(
                LocalDeclaration(result, null),
                ConditionalGotoStatement(condition, thenLabel),
                ExpressionStatement(GotoExpression(elseLabel)),
                LabelStatement(thenLabel),
                ExpressionStatement(AssignmentExpression(null, LocalLvalue(result), then)),
                ExpressionStatement(GotoExpression(finallyLabel)),
                LabelStatement(elseLabel),
                ExpressionStatement(AssignmentExpression(null, LocalLvalue(result), @else)),
                LabelStatement(finallyLabel),
                SequencePointStatement(
                    statement: null,
                    range: null,
                    emitNop: true)),
            value: LocalExpression(result));
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
                        type: IntrinsicSymbols.Bool),
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
                type: IntrinsicSymbols.Bool);
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
                type: IntrinsicSymbols.Bool));
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

        var result = IfExpression(
            condition: left,
            then: right,
            @else: LiteralExpression(false),
            type: IntrinsicSymbols.Bool);
        // If-expressions can be lowered too
        return result.Accept(this);
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

        var result = IfExpression(
            condition: left,
            then: LiteralExpression(true),
            @else: right,
            type: IntrinsicSymbols.Bool);
        // If-expressions can be lowered too
        return result.Accept(this);
    }

    public override BoundNode VisitStringExpression(BoundStringExpression node)
    {
        // Empty string
        if (node.Parts.Length == 0) return LiteralExpression(string.Empty);
        // A single string
        if (node.Parts.Length == 1 && node.Parts[0] is BoundStringText singleText) return LiteralExpression(singleText.Text);
        // A single interpolated part
        if (node.Parts.Length == 1 && node.Parts[0] is BoundStringInterpolation singleInterpolation)
        {
            // Lower the expression
            var arg = (BoundExpression)singleInterpolation.Value.Accept(this);
            // TODO: Just call ToString on it
            throw new System.NotImplementedException();
        }
        // We need to desugar into string.Format("format string", array of args)
        // Build up interpolation string and lower interpolated expressions
        var formatString = new StringBuilder();
        var args = new List<BoundExpression>();
        foreach (var part in node.Parts)
        {
            if (part is BoundStringText text)
            {
                formatString.Append(text.Text);
            }
            else if (part is BoundStringInterpolation interpolation)
            {
                formatString
                    .Append('{')
                    .Append(args.Count)
                    .Append('}');
                // Lower the expression
                var arg = (BoundExpression)interpolation.Value.Accept(this);
                args.Add(arg);
            }
        }

        var arrayType = new ArrayTypeSymbol(IntrinsicSymbols.Object, 1);
        var arrayLocal = new SynthetizedLocalSymbol(arrayType, true);

        var arrayAssignmentBuilder = ImmutableArray.CreateBuilder<BoundStatement>(1 + args.Count);

        // var args = new object[number of interpolated expressions];
        arrayAssignmentBuilder.Add(LocalDeclaration(
            local: arrayLocal,
            value: ArrayCreationExpression(
                elementType: IntrinsicSymbols.Object,
                sizes: ImmutableArray.Create<BoundExpression>(LiteralExpression(args.Count)))));

        for (var i = 0; i < args.Count; i++)
        {
            // args[i] = interpolatedExpr;
            arrayAssignmentBuilder.Add(ExpressionStatement(AssignmentExpression(
                compoundOperator: null,
                left: ArrayAccessLvalue(
                    array: LocalExpression(arrayLocal),
                    indices: ImmutableArray.Create<BoundExpression>(LiteralExpression(i))),
                right: args[i])));
        }

        // {
        //     var args = new object[...];
        //     args[0] = ...;
        //     args[1] = ...;
        //     string.Format("...", args);
        // }
        var result = BlockExpression(
            locals: ImmutableArray.Create<LocalSymbol>(arrayLocal),
            statements: arrayAssignmentBuilder.ToImmutable(),
            value: CallExpression(
                method: this.WellKnownTypes.SystemString_Format,
                receiver: null,
                arguments: ImmutableArray.Create<BoundExpression>(
                    LiteralExpression(formatString.ToString()),
                    LocalExpression(arrayLocal))));

        return result.Accept(this);
    }

    public override BoundNode VisitPropertySetExpression(BoundPropertySetExpression node)
    {
        // property = x
        //
        // =>
        //
        // property_set(x)

        var receiver = node.Receiver;
        var setter = node.Setter;
        var value = node.Value;

        var result = CallExpression(
            receiver: receiver,
            method: setter,
            arguments: ImmutableArray.Create(value));

        return result.Accept(this);
    }

    public override BoundNode VisitPropertyGetExpression(BoundPropertyGetExpression node)
    {
        // property
        //
        // =>
        //
        // property_get()

        var receiver = node.Receiver;
        var getter = node.Getter;

        var result = CallExpression(
            receiver: receiver,
            method: getter,
            arguments: ImmutableArray<BoundExpression>.Empty);

        return result.Accept(this);
    }

    public override BoundNode VisitIndexSetExpression(BoundIndexSetExpression node)
    {
        // indexed[x] = foo
        //
        // =>
        //
        // indexed.Item_set(x, foo)

        var receiver = node.Receiver;
        var setter = node.Setter;
        var args = node.Indices.Append(node.Value).ToImmutableArray();

        var result = CallExpression(
            receiver: receiver,
            method: setter,
            arguments: args);

        return result.Accept(this);
    }

    public override BoundNode VisitIndexGetExpression(BoundIndexGetExpression node)
    {
        // indexed[x]
        //
        // =>
        //
        // indexed.Item_get()

        var receiver = node.Receiver;
        var getter = node.Getter;
        var args = node.Indices;

        var result = CallExpression(
            receiver: receiver,
            method: getter,
            arguments: args);

        return result.Accept(this);
    }

    // Utility to store an expression to a temporary variable
    private TemporaryStorage StoreTemporary(BoundExpression expr)
    {
        // Optimization: if it's already a symbol reference, leave as-is
        // Optimization: if it's a literal, don't bother copying
        if (expr is BoundLocalExpression
                 or BoundGlobalExpression
                 or BoundParameterExpression
                 or BoundFunctionGroupExpression
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
