using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Translates an <see cref="Ast"/> to a <see cref="AstToDataFlowGraph"/>.
/// </summary>
internal sealed class AstToDataFlowGraph
{
    public static DataFlowGraph ToDataFlowGraph(Ast ast)
    {
        var converter = new AstToDataFlowGraph();
        converter.Translate(ast);
        converter.FillNullNodes();
        return new(
            Entry: converter.First,
            Exit: converter.exits.ToImmutable());
    }

    private DataFlowOperation First => this.first ?? new(NoOp());

    private readonly Dictionary<ISymbol.ILabel, DataFlowOperation> labels = new();
    private readonly ImmutableArray<DataFlowOperation>.Builder exits = ImmutableArray.CreateBuilder<DataFlowOperation>();
    private DataFlowOperation? first = null;
    private DataFlowOperation? prev = null;

    private static Ast NoOp() => new Ast.Stmt.NoOp(ParseNode: null);

    private DataFlowOperation GetLabel(ISymbol.ILabel label)
    {
        if (!this.labels.TryGetValue(label, out var op))
        {
            // NOTE: Filled out later
            op = new(null!);
            this.labels.Add(label, op);
        }
        return op;
    }

    private void FillNullNodes()
    {
        // In case there are things like unreferenced labels, we have nulls
        // We promise no nulls, so we fill them
        foreach (var op in this.labels.Values)
        {
            if (op.Node is null) op.Node = NoOp();
        }
    }

    [return: NotNullIfNotNull(nameof(next))]
    private DataFlowOperation? Append(DataFlowOperation? next)
    {
        if (next is null) return null;
        this.first ??= next;
        if (this.prev is not null) DataFlowOperation.Join(this.prev, next);
        this.prev = next;
        return next;
    }

    private DataFlowOperation Append(Ast node) => this.Append(new DataFlowOperation(node));

    private void Disjoin() => this.prev = null;

    private DataFlowOperation? Translate(Ast node) => node switch
    {
        Ast.Stmt.Expr n => this.Translate(n.Expression),
        Ast.Stmt.Decl n => this.Translate(n.Declaration),
        Ast.Decl.Variable n => this.Translate(n),
        Ast.Decl.Label n => this.Translate(n),
        Ast.Expr.Return n => this.Translate(n),
        Ast.Expr.Goto n => this.Translate(n),
        Ast.Expr.Block n => this.Translate(n),
        Ast.Expr.If n => this.Translate(n),
        Ast.Expr.While n => this.Translate(n),
        Ast.Expr.Unary n => this.Translate(n),
        Ast.Expr.Binary n => this.Translate(n),
        Ast.Expr.Assign n => this.Translate(n),
        Ast.Expr.String n => this.Translate(n),
        Ast.Expr.And n => this.Translate(n),
        Ast.Expr.Or n => this.Translate(n),
        Ast.Expr.Relational n => this.Translate(n),
        Ast.Expr.Call n => this.Translate(n),
        Ast.Expr.Reference n => this.Append(n),
        Ast.LValue.Reference n => this.Append(n),
        Ast.StringPart.Interpolation i => this.Translate(i.Expression),
        // For a complete flow, even inert nodes are added
        Ast.Stmt.Unexpected or Ast.Expr.Unexpected or Ast.Expr.Literal => this.Append(node),
        Ast.LValue.Unexpected or Ast.LValue.Illegal => this.Append(node),
        // To avoid reference-equality problems
        Ast.Expr.Unit => this.Append(NoOp()),
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private DataFlowOperation? Translate(Ast.Decl.Variable node)
    {
        if (node.Value is not null) this.Translate(node.Value);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(Ast.Decl.Label node)
    {
        // Connect up
        var op = this.GetLabel(node.LabelSymbol);
        op.Node = node;
        // No new operation, already got one
        return this.Append(op);
    }

    private DataFlowOperation? Translate(Ast.Expr.Unary node)
    {
        this.Translate(node.Operand);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(Ast.Expr.Binary node)
    {
        this.Translate(node.Left);
        this.Translate(node.Right);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(Ast.Expr.Assign node)
    {
        this.Translate(node.Value);
        this.Translate(node.Target);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(Ast.Expr.String node)
    {
        foreach (var part in node.Parts.OfType<Ast.StringPart.Interpolation>()) this.Translate(part);
        return this.Append(node);
    }

    private DataFlowOperation? Translate(Ast.Expr.Return node)
    {
        this.Translate(node.Expression);
        var op = this.Append(node);
        this.exits.Add(op);

        // Disjoin flow
        this.Disjoin();
        return null;
    }

    private DataFlowOperation? Translate(Ast.Expr.Goto node)
    {
        // Join back to the referenced label
        var label = this.GetLabel(node.Target);
        var op = this.Append(node);
        DataFlowOperation.Join(op, label);

        // Disjoin flow
        this.Disjoin();
        return null;
    }

    private DataFlowOperation? Translate(Ast.Expr.Block node)
    {
        foreach (var stmt in node.Statements) this.Translate(stmt);
        this.Translate(node.Value);
        return null;
    }

    private DataFlowOperation? Translate(Ast.Expr.If node)
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

    private DataFlowOperation? Translate(Ast.Expr.While node)
    {
        if (this.prev is null) this.Append(NoOp());

        var beforeCondition = this.Append(node);

        // Condition always runs
        this.Translate(node.Condition);
        var afterCondition = this.prev;

        // The first alternative is that after the condition the body evaluates then jumps back
        this.Translate(node.Expression);
        this.Append(beforeCondition);

        // Allocate a no-op for after
        var noOp = this.Append(NoOp());

        // The other alternatice is that after the condition we exit
        this.prev = afterCondition;
        this.Append(noOp);

        return null;
    }

    private DataFlowOperation? Translate(Ast.Expr.And node)
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

    private DataFlowOperation? Translate(Ast.Expr.Or node)
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

    private DataFlowOperation? Translate(Ast.Expr.Relational node)
    {
        // The first 2 are guaranteed to evaluate, the rest are optional
        // First, chain the first 2
        this.Translate(node.Left);
        this.Translate(node.Comparisons[0].Right);
        var afterLast = this.prev;

        // Allocate a no-op for the end
        var noOp = this.Append(NoOp());

        // Now each one is optional
        foreach (var element in node.Comparisons.Skip(1).Select(c => c.Right))
        {
            this.prev = afterLast;
            this.Translate(element);
            afterLast = this.prev;
            this.Append(noOp);
        }

        return this.Append(node);
    }

    private DataFlowOperation? Translate(Ast.Expr.Call node)
    {
        this.Translate(node.Called);
        foreach (var arg in node.Args) this.Translate(arg);
        return this.Append(node);
    }
}
