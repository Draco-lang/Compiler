using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Builds a control flow graph from a bound tree.
/// </summary>
internal sealed class ControlFlowGraphBuilder : BoundTreeVisitor
{
    public static IControlFlowGraph Build(BoundNode root)
    {
        var builder = new ControlFlowGraphBuilder();
    }

    // All blocks associated to a label
    private readonly Dictionary<LabelSymbol, BasicBlock> labelsToBlocks = [];
    // The current basic block being built
    private BasicBlock? currentBasicBlock;

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
    /// Retrieves the basic block associated with a label.
    /// </summary>
    /// <param name="label">The label to retrieve the basic block for.</param>
    /// <returns>The basic block associated with the label.</returns>
    private BasicBlock GetBasicBlock(LabelSymbol label)
    {
        if (!this.labelsToBlocks.TryGetValue(label, out var block))
        {
            block = new();
            this.labelsToBlocks.Add(label, block);
        }
        return block;
    }

    // Alters flow /////////////////////////////////////////////////////////////

    // TODO

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

    public override void VisitUnaryExpression(BoundUnaryExpression node)
    {
        base.VisitUnaryExpression(node);
        this.Append(node);
    }

    // Passthrough /////////////////////////////////////////////////////////////
    // Elements that themselves do not alter the control flow, but their children might

    public override void VisitBlockExpression(BoundBlockExpression node) => base.VisitBlockExpression(node);
    public override void VisitExpressionStatement(BoundExpressionStatement node) => base.VisitExpressionStatement(node);
    public override void VisitStringExpression(BoundStringExpression node) => base.VisitStringExpression(node);
    public override void VisitStringInterpolation(BoundStringInterpolation node) => base.VisitStringInterpolation(node);

    // Inert ///////////////////////////////////////////////////////////////////
    // Elements that have no effect on the control flow

    public override void VisitFunctionGroupExpression(BoundFunctionGroupExpression node) { }
    public override void VisitGlobalExpression(BoundGlobalExpression node) { }
    public override void VisitGlobalLvalue(BoundGlobalLvalue node) { }
    public override void VisitIllegalLvalue(BoundIllegalLvalue node) { }
    public override void VisitLiteralExpression(BoundLiteralExpression node) { }
    public override void VisitLocalExpression(BoundLocalExpression node) { }
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
}
