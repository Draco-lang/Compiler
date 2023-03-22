using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Translates a bound tree to a <see cref="DataFlowGraph"/>.
/// </summary>
internal sealed class BoundTreeToDataFlowGraph
{
    public static DataFlowGraph ToDataFlowGraph(BoundNode node)
    {
        var converter = new BoundTreeToDataFlowGraph();
        converter.Translate(node);
        converter.FillNullNodes();
        return new(
            Entry: converter.First,
            Exit: converter.exits.ToImmutable());
    }

    private DataFlowOperation First => this.first;

    private readonly Dictionary<LabelSymbol, DataFlowOperation> labels = new();
    private readonly ImmutableArray<DataFlowOperation>.Builder exits = ImmutableArray.CreateBuilder<DataFlowOperation>();
    private readonly DataFlowOperation first = new(NoOp());
    private DataFlowOperation? prev;

    private BoundTreeToDataFlowGraph()
    {
        this.prev = this.first;
    }

    private static BoundNode NoOp() => new BoundNoOpStatement(syntax: null);

    private DataFlowOperation GetLabel(LabelSymbol label)
    {
        if (!this.labels.TryGetValue(label, out var op))
        {
            // NOTE: AST node filled out later
            // For error labels, we jump to the beginning for safety
            // This pervents most flow-errors to cascade
            // TODO: Unhandled
            // op = label.IsError ? this.first : new(null!);
            op = new(null!);
            this.labels.Add(label, op);
        }
        return op;
    }

    private void FillNullNodes()
    {
        // In case there are things like unreferenced labels, we have nulls
        // We promise no nulls, so we fill them
        foreach (var op in this.labels.Values) op.Node ??= NoOp();
    }

    [return: NotNullIfNotNull(nameof(next))]
    private DataFlowOperation? Append(DataFlowOperation? next)
    {
        if (next is null) return null;
        if (this.prev is not null) DataFlowOperation.Join(this.prev, next);
        this.prev = next;
        return next;
    }

    private DataFlowOperation Append(BoundNode node) => this.Append(new DataFlowOperation(node));

    private void Disjoin() => this.prev = null;

    private DataFlowOperation? Translate(BoundNode node) => node switch
    {
        BoundExpressionStatement n => this.Translate(n.Expression),
        BoundStringInterpolation n => this.Translate(n.Value),
        BoundLocalDeclaration n => this.Translate(n),
        BoundLabelStatement n => this.Translate(n),
        BoundReturnExpression n => this.Translate(n),
        BoundGotoExpression n => this.Translate(n),
        BoundBlockExpression n => this.Translate(n),
        BoundIfExpression n => this.Translate(n),
        BoundWhileExpression n => this.Translate(n),
        BoundUnaryExpression n => this.Translate(n),
        BoundBinaryExpression n => this.Translate(n),
        BoundAssignmentExpression n => this.Translate(n),
        BoundStringExpression n => this.Translate(n),
        BoundAndExpression n => this.Translate(n),
        BoundOrExpression n => this.Translate(n),
        BoundRelationalExpression n => this.Translate(n),
        BoundCallExpression n => this.Translate(n),
        BoundLocalExpression n => this.Append(n),
        BoundGlobalExpression n => this.Append(n),
        BoundParameterExpression n => this.Append(n),
        BoundFunctionExpression n => this.Append(n),
        BoundLiteralExpression n => this.Append(n),
        BoundLocalLvalue n => this.Append(n),
        BoundGlobalLvalue n => this.Append(n),
        // For a complete flow, even inert nodes are added
        BoundReferenceErrorExpression n => this.Append(n),
        // TODO: What do we do here?
        // Ast.Stmt.Unexpected or Ast.Expr.Unexpected or Ast.Expr.Literal => this.Append(node),
        // Ast.LValue.Unexpected or Ast.LValue.Illegal => this.Append(node),
        // ==============
        // To avoid reference-equality problems
        BoundUnitExpression => this.Append(NoOp()),
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private DataFlowOperation? Translate(BoundLocalDeclaration node)
    {
        if (node.Value is not null) this.Translate(node.Value);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundLabelStatement node)
    {
        // Connect up
        var op = this.GetLabel(node.Label);
        op.Node = node;
        // No new operation, already got one
        return this.Append(op);
    }

    private DataFlowOperation? Translate(BoundUnaryExpression node)
    {
        this.Translate(node.Operand);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundBinaryExpression node)
    {
        this.Translate(node.Left);
        this.Translate(node.Right);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundAssignmentExpression node)
    {
        // NOTE: Reverse order, it's right-associative
        this.Translate(node.Right);
        this.Translate(node.Left);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundStringExpression node)
    {
        foreach (var part in node.Parts.OfType<BoundStringInterpolation>()) this.Translate(part);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundReturnExpression node)
    {
        this.Translate(node.Value);
        var op = this.Append(node);
        this.exits.Add(op);

        // Disjoin flow
        this.Disjoin();
        return null;
    }

    private DataFlowOperation? Translate(BoundGotoExpression node)
    {
        // Join back to the referenced label
        var label = this.GetLabel(node.Target);
        var op = this.Append(node);
        DataFlowOperation.Join(op, label);

        // Disjoin flow
        this.Disjoin();
        return null;
    }

    private DataFlowOperation? Translate(BoundBlockExpression node)
    {
        foreach (var stmt in node.Statements) this.Translate(stmt);
        this.Translate(node.Value);
        return null;
    }

    private DataFlowOperation? Translate(BoundIfExpression node)
    {
        this.Translate(node.Condition);

        // Both alternatives start from the same place
        var afterCondition = this.prev;

        // Then
        this.prev = afterCondition;
        this.Translate(node.Then);
        var afterThen = this.prev;

        // Else
        this.prev = afterCondition;
        this.Translate(node.Else);
        var afterElse = this.prev;

        // Connect both to a no-op for simplicity
        this.prev = afterThen;
        var noOp = this.Append(NoOp());
        this.prev = afterElse;
        this.Append(noOp);

        return null;
    }

    private DataFlowOperation? Translate(BoundWhileExpression node)
    {
        // Connect up continue label
        var continueOp = this.GetLabel(node.ContinueLabel);
        this.Append(continueOp);

        // Condition always runs
        this.Translate(node.Condition);
        var afterCondition = this.prev;

        // The first alternative is that after the condition the body evaluates then jumps back
        this.Translate(node.Then);
        this.Append(continueOp);

        // Break label
        var breakOp = this.GetLabel(node.BreakLabel);

        // The other alternative is that after the condition we exit
        this.prev = afterCondition;
        this.Append(breakOp);

        return null;
    }

    private DataFlowOperation? Translate(BoundAndExpression node)
    {
        // First one always translates
        this.Translate(node.Left);
        var afterLeft = this.prev;

        // Optionally the second one evaluates
        this.Translate(node.Right);

        // Allocate a no-op after
        var noOp = this.Append(NoOp());

        // Alternatively, the right side does not evaluate
        this.prev = afterLeft;
        this.Append(noOp);

        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundOrExpression node)
    {
        // First one always translates
        this.Translate(node.Left);
        var afterLeft = this.prev;

        // Optionally the second one evaluates
        this.Translate(node.Right);

        // Allocate a no-op after
        var noOp = this.Append(NoOp());

        // Alternatively, the right side does not evaluate
        this.prev = afterLeft;
        this.Append(noOp);

        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundRelationalExpression node)
    {
        // The first 2 are guaranteed to evaluate, the rest are optional
        // First, chain the first 2
        this.Translate(node.First);
        this.Translate(node.Comparisons[0].Next);
        var afterLast = this.prev;

        // Allocate a no-op for the end
        var noOp = this.Append(NoOp());

        // Now each one is optional
        foreach (var element in node.Comparisons.Skip(1).Select(c => c.Next))
        {
            this.prev = afterLast;
            this.Translate(element);
            afterLast = this.prev;
            this.Append(noOp);
        }

        return this.Append(node);
    }

    private DataFlowOperation? Translate(BoundCallExpression node)
    {
        this.Translate(node.Method);
        foreach (var arg in node.Arguments) this.Translate(arg);
        return this.Append(node);
    }
}
