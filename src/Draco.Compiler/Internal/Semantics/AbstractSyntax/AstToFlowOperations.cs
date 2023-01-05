using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Semantics.Symbols;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Converts the <see cref="Ast"/> into <see cref="FlowOperation"/>s.
/// </summary>
internal sealed class AstToFlowOperations : AstVisitorBase<FlowOperation?>
{
    public static BasicBlock ToFlowOperations(Ast.Decl.Func func)
    {
        var visitor = new AstToFlowOperations();
        visitor.Visit(func.Body);
        return visitor.entry;
    }

    private readonly BasicBlock entry;
    private readonly Dictionary<ISymbol.ILabel, BasicBlock> labels = new();
    private BasicBlock currentBlock;

    private AstToFlowOperations()
    {
        this.entry = new();
        this.currentBlock = this.entry;
    }

    private BasicBlock GetBlock(ISymbol.ILabel label)
    {
        if (!this.labels.TryGetValue(label, out var block))
        {
            block = new();
            this.labels.Add(label, block);
        }
        return block;
    }

    private void AddOperation(FlowOperation op) => this.currentBlock.Operations.Add(op);

    public override FlowOperation? VisitVariableDecl(Ast.Decl.Variable node)
    {
        if (node.Value is not null)
        {
            var value = this.VisitExpr(node.Value);
            Debug.Assert(value is not null);
            var assignment = new FlowOperation.Assign(
                Ast: node,
                Target: node.DeclarationSymbol,
                Value: value!);
            this.AddOperation(assignment);
        }
        return this.Default;
    }

    public override FlowOperation? VisitLabelDecl(Ast.Decl.Label node)
    {
        // Allocate label
        var block = this.GetBlock(node.LabelSymbol);

        // Jump to new block
        this.currentBlock.Control = new FlowControlOperation.Goto(
            Target: block);
        this.currentBlock = block;

        return this.Default;
    }

    public override FlowOperation VisitAssignExpr(Ast.Expr.Assign node)
    {
        if (node.CompoundOperator is not null)
        {
            // TODO
            throw new NotImplementedException();
        }
        var target = this.GetLvalue(node);
        var value = this.VisitExpr(node.Value);
        var assignment = new FlowOperation.Assign(
            Ast: node,
            Target: target,
            Value: value!);
        this.AddOperation(assignment);
        return assignment;
    }

    public override FlowOperation VisitUnaryExpr(Ast.Expr.Unary node)
    {
        // Represent as a call
        var sub = this.VisitExpr(node.Operand);
        var call = new FlowOperation.Call(
            Ast: node,
            FunctionType: node.Operator.Type,
            Args: ImmutableArray.Create(sub!));
        this.AddOperation(call);
        return call;
    }

    public override FlowOperation VisitBinaryExpr(Ast.Expr.Binary node)
    {
        // Represent as a call
        var left = this.VisitExpr(node.Left);
        var right = this.VisitExpr(node.Left);
        var call = new FlowOperation.Call(
            Ast: node,
            FunctionType: node.Operator.Type,
            Args: ImmutableArray.Create(left!, right!));
        this.AddOperation(call);
        return call;
    }

    public override FlowOperation VisitCallExpr(Ast.Expr.Call node)
    {
        var called = this.VisitExpr(node.Called);
        var args = node.Args.Select(this.VisitExpr);
        var call = new FlowOperation.Call(
            Ast: node,
            FunctionType: (Type.Function)node.Called.EvaluationType,
            Args: args.ToImmutableArray()!);
        this.AddOperation(call);
        return call;
    }

    public override FlowOperation VisitBlockExpr(Ast.Expr.Block node)
    {
        foreach (var stmt in node.Statements) this.VisitStmt(stmt);
        return this.VisitExpr(node.Value)!;
    }

    public override FlowOperation VisitIfExpr(Ast.Expr.If node)
    {
        var thenBlock = new BasicBlock();
        var elseBlock = new BasicBlock();
        var finallyBlock = new BasicBlock();

        // Condition
        var condition = this.VisitExpr(node.Condition);
        this.currentBlock.Control = new FlowControlOperation.IfElse(
            Condition: condition!,
            Then: thenBlock,
            Else: elseBlock);

        // Then
        this.currentBlock = thenBlock;
        var thenValue = this.VisitExpr(node.Then);
        this.currentBlock.Control = new FlowControlOperation.Goto(
            Target: finallyBlock);

        // Else
        this.currentBlock = elseBlock;
        var elseValue = this.VisitExpr(node.Else);
        this.currentBlock.Control = new FlowControlOperation.Goto(
            Target: finallyBlock);

        // Finally
        this.currentBlock = finallyBlock;

        var result = new FlowOperation.Phi(
            Ast: node,
            ImmutableArray.Create(
                new KeyValuePair<BasicBlock, FlowOperation>(thenBlock, thenValue!),
                new KeyValuePair<BasicBlock, FlowOperation>(elseBlock, elseValue!)));
        return result;
    }

    public override FlowOperation? VisitWhileExpr(Ast.Expr.While node)
    {
        var startBlock = new BasicBlock();
        var thenBlock = new BasicBlock();
        var endBlock = new BasicBlock();

        // Jump to new block
        this.currentBlock.Control = new FlowControlOperation.Goto(
            Target: startBlock);
        this.currentBlock = startBlock;

        // Condition
        var condition = this.VisitExpr(node.Condition);
        this.currentBlock.Control = new FlowControlOperation.IfElse(
            Condition: condition!,
            Then: thenBlock,
            Else: endBlock);

        // Body
        this.currentBlock = thenBlock;
        this.VisitExpr(node.Expression);
        this.currentBlock.Control = new FlowControlOperation.Goto(
            Target: startBlock);

        // End
        this.currentBlock = endBlock;

        return this.Default;
    }

    public override FlowOperation? VisitGotoExpr(Ast.Expr.Goto node)
    {
        this.currentBlock.Control = new FlowControlOperation.Goto(
            Target: this.GetBlock(node.Target));
        this.currentBlock = new BasicBlock();
        return this.Default;
    }

    public override FlowOperation VisitLiteralExpr(Ast.Expr.Literal node) => new FlowOperation.Constant(Ast: node);

    // TODO: Implement
    public override FlowOperation VisitAndExpr(Ast.Expr.And node) => throw new NotImplementedException();

    // TODO: Implement
    public override FlowOperation VisitOrExpr(Ast.Expr.Or node) => throw new NotImplementedException();

    private ISymbol.IVariable GetLvalue(Ast.Expr value) => value switch
    {
        // TODO: Not necessarily true, but who will validate this?
        // Or should this hold true and we need to change AST.Assign to mean this?
        // Maybe have assignment for variables, and represent index and property setters completely separate?
        Ast.Expr.Reference r => (ISymbol.IVariable)r.Symbol,
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}
