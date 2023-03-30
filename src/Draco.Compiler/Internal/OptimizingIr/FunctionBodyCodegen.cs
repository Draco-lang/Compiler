using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Generates IR code on function-local level.
/// </summary>
internal sealed class FunctionBodyCodegen : BoundTreeVisitor<IOperand>
{
    private readonly Procedure procedure;
    private BasicBlock currentBasicBlock;

    public FunctionBodyCodegen(Procedure procedure)
    {
        this.procedure = procedure;
        this.currentBasicBlock = procedure.Entry;
    }

    public override IOperand VisitReturnExpression(BoundReturnExpression node)
    {
        var operand = node.Value.Accept(this);
        this.currentBasicBlock.InsertLast(new RetInstruction(operand));
        return default!;
    }
}
