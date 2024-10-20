using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Builds a control flow graph from a bound tree.
/// </summary>
internal sealed class ControlFlowGraphBuilder : BoundTreeVisitor
{
    /// <summary>
    /// Builds a control flow graph from a bound tree.
    /// </summary>
    /// <param name="root">The root of the bound tree.</param>
    /// <returns>The control flow graph.</returns>
    public static IControlFlowGraph Build(BoundNode root)
    {
        var builder = new ControlFlowGraphBuilder();
        builder.EnsureBlock();
        var start = builder.currentBasicBlock!;
        root.Accept(builder);
        var exit = builder.MergeExitPoints(start);
        return new ControlFlowGraph(start, exit);
    }

    // All blocks associated to a label
    private readonly Dictionary<LabelSymbol, BasicBlock> labelsToBlocks = [];
    // All exit points
    private readonly HashSet<BasicBlock> exitPoints = [];
    // The current basic block being built
    private BasicBlock? currentBasicBlock = null;

    private ControlFlowGraphBuilder()
    {
    }

    /// <summary>
    /// Appends a node to the current basic block.
    /// If there is no current basic block, a new one is created.
    /// </summary>
    /// <param name="node">The node to append.</param>
    private void Append(BoundNode node)
    {
        this.currentBasicBlock ??= new();
        this.currentBasicBlock.Nodes.Add(node);
    }

    /// <summary>
    /// Detaches the current basic block, meaning the builder will be in an "unreachable" state.
    /// This could be right after a goto for example.
    /// </summary>
    private void Detach()
    {
        this.currentBasicBlock = null!;
    }

    /// <summary>
    /// Ensures that there is a current basic block.
    /// </summary>
    private void EnsureBlock()
    {
        this.currentBasicBlock ??= new();
    }

    /// <summary>
    /// Retrieves the basic block associated with a label.
    /// </summary>
    /// <param name="label">The label to retrieve the basic block for.</param>
    /// <returns>The basic block associated with the label.</returns>
    private BasicBlock GetBlock(LabelSymbol label)
    {
        if (!this.labelsToBlocks.TryGetValue(label, out var block))
        {
            block = new();
            this.labelsToBlocks.Add(label, block);
        }
        return block;
    }

    /// <summary>
    /// Connects the current basic block to another basic block.
    /// Does not change the current basic block.
    /// </summary>
    /// <param name="to">The basic block to connect to (successor).</param>
    private void ConnectTo(BasicBlock to, FlowCondition condition)
    {
        if (this.currentBasicBlock is null) return;

        var predecessorEdge = new PredecessorEdge(condition, this.currentBasicBlock);
        var successorEdge = new SuccessorEdge(condition, to);

        this.currentBasicBlock.Successors.Add(successorEdge);
        to.Predecessors.Add(predecessorEdge);
    }

    /// <summary>
    /// Merge all exit points into a single block.
    /// </summary>
    /// <param name="start">The start block.</param>
    /// <returns>The exit block. Can return null, if there are no exit points.</returns>
    private BasicBlock? MergeExitPoints(IBasicBlock start)
    {
        // Add all basic blocks as exit points that have no successors
        var blocksWithNoSuccessors = GraphTraversal
            .DepthFirst(start, bb => bb.Successors.Select(s => s.Successor))
            .Where(bb => !bb.Successors.Any());
        foreach (var block in blocksWithNoSuccessors) this.exitPoints.Add((BasicBlock)block);

        // If there is 1 or none, no need to create an extra node
        if (this.exitPoints.Count <= 1) return this.exitPoints.FirstOrDefault();

        // We need to merge all exit points into a single block
        var finalBlock = new BasicBlock();
        foreach (var exitPoint in this.exitPoints)
        {
            this.currentBasicBlock = exitPoint;
            this.ConnectTo(finalBlock, FlowCondition.Always);
        }
        return finalBlock;
    }

    // Alters flow /////////////////////////////////////////////////////////////

    public override void VisitReturnExpression(BoundReturnExpression node)
    {
        node.Value?.Accept(this);
        this.Append(node);
        Debug.Assert(this.currentBasicBlock is not null);
        this.exitPoints.Add(this.currentBasicBlock);
        this.Detach();
    }

    public override void VisitLabelStatement(BoundLabelStatement node)
    {
        // Connect the current block to the label
        this.ConnectTo(this.GetBlock(node.Label), FlowCondition.Always);
        this.currentBasicBlock = this.GetBlock(node.Label);
    }

    public override void VisitGotoExpression(BoundGotoExpression node)
    {
        var target = this.GetBlock(node.Target);
        this.ConnectTo(target, FlowCondition.Always);
        this.Detach();
    }

    public override void VisitIfExpression(BoundIfExpression node)
    {
        // Condition runs unconditionally
        node.Condition.Accept(this);
        // We have 2 alternative branches
        var thenBlock = new BasicBlock();
        var elseBlock = new BasicBlock();
        var finalBlock = new BasicBlock();
        // Connect the current block to the branches
        this.ConnectTo(thenBlock, WhenTrue(node.Condition));
        this.ConnectTo(elseBlock, WhenFalse(node.Condition));
        // Translate then
        this.currentBasicBlock = thenBlock;
        node.Then.Accept(this);
        this.ConnectTo(finalBlock, FlowCondition.Always);
        // Translate else
        this.currentBasicBlock = elseBlock;
        node.Else.Accept(this);
        this.ConnectTo(finalBlock, FlowCondition.Always);
        // Continue with the final block
        this.currentBasicBlock = finalBlock;
    }

    public override void VisitWhileExpression(BoundWhileExpression node)
    {
        var continueBlock = this.GetBlock(node.ContinueLabel);
        var breakBlock = this.GetBlock(node.BreakLabel);
        // Sequence the current block to the continue block
        this.ConnectTo(continueBlock, FlowCondition.Always);
        // Translate the condition
        this.currentBasicBlock = continueBlock;
        node.Condition.Accept(this);
        // We can either run the body, or break out
        var bodyBlock = new BasicBlock();
        this.ConnectTo(bodyBlock, WhenTrue(node.Condition));
        this.ConnectTo(breakBlock, WhenFalse(node.Condition));
        // Translate the body
        this.currentBasicBlock = bodyBlock;
        node.Then.Accept(this);
        // Go back to the continue block
        this.ConnectTo(continueBlock, FlowCondition.Always);
        // Go on with the break block
        this.currentBasicBlock = breakBlock;
    }

    public override void VisitForExpression(BoundForExpression node)
    {
        var continueBlock = this.GetBlock(node.ContinueLabel);
        var breakBlock = this.GetBlock(node.BreakLabel);
        // We need to create an extra block right after the sequence that makes the conditional jump
        var conditionBlock = new BasicBlock();
        // The sequence gets evaluated at the start
        node.Sequence.Accept(this);
        // Then we jump to the condition block
        this.ConnectTo(conditionBlock, FlowCondition.Always);
        this.currentBasicBlock = conditionBlock;
        // Then we jump to the continue block or the break block
        this.ConnectTo(continueBlock, SequenceItem(node.Sequence));
        this.ConnectTo(breakBlock, SequenceEnd(node.Sequence));
        // Translate the body
        this.currentBasicBlock = continueBlock;
        node.Then.Accept(this);
        // Go back to the condition block
        this.ConnectTo(conditionBlock, FlowCondition.Always);
        // Go on with the break block
        this.currentBasicBlock = breakBlock;
    }

    public override void VisitAndExpression(BoundAndExpression node)
    {
        // Right side might not be evaluated
        var finallyBlock = new BasicBlock();
        var rightRuns = new BasicBlock();
        node.Left.Accept(this);
        this.ConnectTo(finallyBlock, WhenFalse(node.Left));
        this.ConnectTo(rightRuns, WhenTrue(node.Left));
        this.currentBasicBlock = rightRuns;
        node.Right.Accept(this);
        this.ConnectTo(finallyBlock, FlowCondition.Always);
        this.currentBasicBlock = finallyBlock;
    }

    public override void VisitOrExpression(BoundOrExpression node)
    {
        // Right side might not be evaluated
        var finallyBlock = new BasicBlock();
        var rightRuns = new BasicBlock();
        node.Left.Accept(this);
        this.ConnectTo(finallyBlock, WhenTrue(node.Left));
        this.ConnectTo(rightRuns, WhenFalse(node.Left));
        this.currentBasicBlock = rightRuns;
        node.Right.Accept(this);
        this.ConnectTo(finallyBlock, FlowCondition.Always);
        this.currentBasicBlock = finallyBlock;
    }

    public override void VisitRelationalExpression(BoundRelationalExpression node)
    {
        if (node.Comparisons.Length <= 1)
        {
            base.VisitRelationalExpression(node);
            this.Append(node);
            return;
        }

        // The first 2 operands are always evaluated
        // After that, each operand might not be evaluated
        var finallyBlock = new BasicBlock();
        node.First.Accept(this);
        node.Comparisons[0].Accept(this);
        for (var i = 1; i < node.Comparisons.Length; ++i)
        {
            var rightRuns = new BasicBlock();
            var previousValue = node.Comparisons[i - 1].Next;
            var currentComparison = node.Comparisons[i];
            this.ConnectTo(finallyBlock, ComparisonFalse(previousValue, currentComparison));
            this.ConnectTo(rightRuns, ComparisonTrue(previousValue, currentComparison));
            this.currentBasicBlock = rightRuns;
            currentComparison.Accept(this);
        }
        this.ConnectTo(finallyBlock, FlowCondition.Always);
        this.currentBasicBlock = finallyBlock;
        this.Append(node);
    }

    // Special cases ///////////////////////////////////////////////////////////

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        // The order of operations is important here
        node.Right.Accept(this);
        node.Left.Accept(this);
        this.Append(node);
    }

    // Trivial topological appending, no control flow //////////////////////////

    public override void VisitArrayAccessExpression(BoundArrayAccessExpression node)
    {
        base.VisitArrayAccessExpression(node);
        this.Append(node);
    }

    public override void VisitArrayAccessLvalue(BoundArrayAccessLvalue node)
    {
        base.VisitArrayAccessLvalue(node);
        this.Append(node);
    }

    public override void VisitArrayCreationExpression(BoundArrayCreationExpression node)
    {
        base.VisitArrayCreationExpression(node);
        this.Append(node);
    }

    public override void VisitBinaryExpression(BoundBinaryExpression node)
    {
        base.VisitBinaryExpression(node);
        this.Append(node);
    }

    public override void VisitCallExpression(BoundCallExpression node)
    {
        base.VisitCallExpression(node);
        this.Append(node);
    }

    public override void VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
    {
        base.VisitDelegateCreationExpression(node);
        this.Append(node);
    }

    public override void VisitFieldExpression(BoundFieldExpression node)
    {
        base.VisitFieldExpression(node);
        this.Append(node);
    }

    public override void VisitFieldLvalue(BoundFieldLvalue node)
    {
        base.VisitFieldLvalue(node);
        this.Append(node);
    }

    public override void VisitIndexGetExpression(BoundIndexGetExpression node)
    {
        base.VisitIndexGetExpression(node);
        this.Append(node);
    }

    public override void VisitIndexSetExpression(BoundIndexSetExpression node)
    {
        base.VisitIndexSetExpression(node);
        this.Append(node);
    }

    public override void VisitIndexSetLvalue(BoundIndexSetLvalue node)
    {
        base.VisitIndexSetLvalue(node);
        this.Append(node);
    }

    public override void VisitIndirectCallExpression(BoundIndirectCallExpression node)
    {
        base.VisitIndirectCallExpression(node);
        this.Append(node);
    }

    public override void VisitObjectCreationExpression(BoundObjectCreationExpression node)
    {
        base.VisitObjectCreationExpression(node);
        this.Append(node);
    }

    public override void VisitPropertyGetExpression(BoundPropertyGetExpression node)
    {
        base.VisitPropertyGetExpression(node);
        this.Append(node);
    }

    public override void VisitPropertySetExpression(BoundPropertySetExpression node)
    {
        base.VisitPropertySetExpression(node);
        this.Append(node);
    }

    public override void VisitPropertySetLvalue(BoundPropertySetLvalue node)
    {
        base.VisitPropertySetLvalue(node);
        this.Append(node);
    }

    public override void VisitStringExpression(BoundStringExpression node)
    {
        base.VisitStringExpression(node);
        // Trivial strings get inlined for the graph, no append
        if (node.Parts.Length == 1 && node.Parts[0] is BoundStringText) return;
        this.Append(node);
    }

    public override void VisitUnaryExpression(BoundUnaryExpression node)
    {
        base.VisitUnaryExpression(node);
        this.Append(node);
    }

    // Passthrough /////////////////////////////////////////////////////////////
    // Elements that themselves do not alter the control flow, but their children might

    public override void VisitBlockExpression(BoundBlockExpression node) => base.VisitBlockExpression(node);
    public override void VisitComparison(BoundComparison node) => base.VisitComparison(node);
    public override void VisitExpressionStatement(BoundExpressionStatement node) => base.VisitExpressionStatement(node);
    public override void VisitStringInterpolation(BoundStringInterpolation node) => base.VisitStringInterpolation(node);

    // Inert ///////////////////////////////////////////////////////////////////
    // Elements that have no effect on the control flow

    public override void VisitFunctionGroupExpression(BoundFunctionGroupExpression node) { }
    public override void VisitGlobalExpression(BoundGlobalExpression node) { }
    public override void VisitGlobalLvalue(BoundGlobalLvalue node) { }
    public override void VisitIllegalLvalue(BoundIllegalLvalue node) { }
    public override void VisitLiteralExpression(BoundLiteralExpression node) { }
    public override void VisitLocalExpression(BoundLocalExpression node) { }
    public override void VisitLocalFunction(BoundLocalFunction node) { }
    public override void VisitLocalLvalue(BoundLocalLvalue node) { }
    public override void VisitModuleExpression(BoundModuleExpression node) { }
    public override void VisitNoOpStatement(BoundNoOpStatement node) { }
    public override void VisitParameterExpression(BoundParameterExpression node) { }
    public override void VisitReferenceErrorExpression(BoundReferenceErrorExpression node) { }
    public override void VisitStringText(BoundStringText node) { }
    public override void VisitTypeExpression(BoundTypeExpression node) { }
    public override void VisitUnexpectedExpression(BoundUnexpectedExpression node) { }
    public override void VisitUnexpectedLvalue(BoundUnexpectedLvalue node) { }
    public override void VisitUnexpectedStatement(BoundUnexpectedStatement node) { }
    public override void VisitUnexpectedStringPart(BoundUnexpectedStringPart node) { }
    public override void VisitUnitExpression(BoundUnitExpression node) { }

    // Lowered /////////////////////////////////////////////////////////////////

    public override void VisitConditionalGotoStatement(BoundConditionalGotoStatement node) => ThrowOnLoweredNode(node);
    public override void VisitSequencePointExpression(BoundSequencePointExpression node) => ThrowOnLoweredNode(node);
    public override void VisitSequencePointStatement(BoundSequencePointStatement node) => ThrowOnLoweredNode(node);

    private static void ThrowOnLoweredNode(BoundNode node) =>
        throw new InvalidOperationException($"the lowered node {node.GetType().Name} should not be rewritten, a CFG should be constructed from the non-lowered tree");

    // Utility /////////////////////////////////////////////////////////////////

    private static FlowCondition WhenTrue(BoundExpression condition) =>
        FlowCondition.WhenTrue(Unwrap(condition));

    private static FlowCondition WhenFalse(BoundExpression condition) =>
        FlowCondition.WhenFalse(Unwrap(condition));

    private static FlowCondition SequenceItem(BoundExpression condition) =>
        FlowCondition.SequenceItem(Unwrap(condition));

    private static FlowCondition SequenceEnd(BoundExpression condition) =>
        FlowCondition.SequenceEnd(Unwrap(condition));

    private static FlowCondition ComparisonTrue(BoundExpression left, BoundComparison right) =>
        FlowCondition.ComparisonTrue(Unwrap(left), right);

    private static FlowCondition ComparisonFalse(BoundExpression left, BoundComparison right) =>
        FlowCondition.ComparisonFalse(Unwrap(left), right);

    private static BoundExpression Unwrap(BoundExpression node) => node switch
    {
        BoundBlockExpression block => Unwrap(block.Value),
        _ => node,
    };
}
