using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.OptimizingIr.Model;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

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

    private void Write(IInstruction instr) => this.currentBasicBlock.InsertLast(instr);

    public override IOperand VisitReturnExpression(BoundReturnExpression node)
    {
        var operand = node.Value.Accept(this);
        this.Write(Ret(operand));
        return default!;
    }

    public override IOperand VisitUnitExpression(BoundUnitExpression node) => default(Void);
}
