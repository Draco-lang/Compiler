using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Draco.Compiler.Internal.Semantics.Symbols;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Converts the <see cref="Ast"/> into <see cref="FlowOperation"/>s.
/// </summary>
internal sealed class AstToFlowOperations : AstVisitorBase<FlowOperation?>
{
    public static BasicBlock ToFlowOperation(Ast.Decl.Func func)
    {
        var visitor = new AstToFlowOperations();
        visitor.Visit(func.Body);
        return visitor.entry;
    }

    private readonly BasicBlock entry;
    private BasicBlock currentBlock;

    private AstToFlowOperations()
    {
        this.entry = new();
        this.currentBlock = this.entry;
    }

    private void AddOperation(FlowOperation op) => this.currentBlock.Operations.Add(op);

    public override FlowOperation? VisitVariableDecl(Ast.Decl.Variable node)
    {
        if (node.Value is not null)
        {
            var value = this.VisitExpr(node.Value);
            Debug.Assert(value is not null);
            var assignment = new FlowOperation.Assign(node, node.DeclarationSymbol, value!);
            this.AddOperation(assignment);
        }
        return this.Default;
    }

    public override FlowOperation VisitAssignExpr(Ast.Expr.Assign node)
    {
        var target = this.GetLvalue(node);
        var value = this.VisitExpr(node.Value);
        Debug.Assert(value is not null);
        var assignment = new FlowOperation.Assign(node, target, value!);
        this.AddOperation(assignment);
        return assignment;
    }

    private ISymbol.IVariable GetLvalue(Ast.Expr value) => value switch
    {
        // TODO: Not necessarily true, but who will validate this?
        // Or should this hold true and we need to change AST.Assign to mean this?
        Ast.Expr.Reference r => (ISymbol.IVariable)r.Symbol,
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}
