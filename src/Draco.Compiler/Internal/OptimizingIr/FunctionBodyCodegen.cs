using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Generates IR code on function-local level.
/// </summary>
internal sealed partial class FunctionBodyCodegen : BoundTreeVisitor<IOperand>
{
    private readonly Procedure procedure;
    private BasicBlock currentBasicBlock;

    public FunctionBodyCodegen(Procedure procedure)
    {
        this.procedure = procedure;
        this.currentBasicBlock = procedure.Entry;
    }

    private void Compile(BoundStatement stmt) => stmt.Accept(this);
    private IOperand Compile(BoundExpression expr) => expr.Accept(this);
    private void Write(IInstruction instr) => this.currentBasicBlock.InsertLast(instr);
    private BasicBlock DefineBasicBlock() => this.procedure.DefineBasicBlock();
    private Local DefineLocal(LocalSymbol local) => this.procedure.DefineLocal(local);
    private Local DefineLocal(Type type) => this.procedure.DefineLocal(type);
    private Register DefineRegister() => this.procedure.DefineRegister();

    public override IOperand VisitIfExpression(BoundIfExpression node)
    {
        // To support a return value, we allocate local storage
        // The two branches write their own results respectively
        // And finally we load the value into a register

        var condition = this.Compile(node.Condition);

        var thenBlock = this.DefineBasicBlock();
        var elseBlock = this.DefineBasicBlock();
        var finallyBlock = this.DefineBasicBlock();

        this.Write(Branch(condition, thenBlock, elseBlock));
        var storage = this.DefineLocal(node.TypeRequired);

        this.currentBasicBlock = thenBlock;
        var thenValue = this.Compile(node.Then);
        this.Write(Store(storage, thenValue));
        this.Write(Jump(finallyBlock));

        this.currentBasicBlock = elseBlock;
        var elseValue = this.Compile(node.Else);
        this.Write(Store(storage, elseValue));
        this.Write(Jump(finallyBlock));

        this.currentBasicBlock = finallyBlock;

        var result = this.DefineRegister();
        this.Write(Load(result, storage));

        return result;
    }

    public override IOperand VisitBlockExpression(BoundBlockExpression node)
    {
        // Compile all of the statements within
        foreach (var stmt in node.Statements) this.Compile(stmt);
        // Compile value
        return this.Compile(node.Value);
    }

    public override IOperand VisitBinaryExpression(BoundBinaryExpression node)
    {
        var left = this.Compile(node.Left);
        var right = this.Compile(node.Right);
        var target = this.DefineRegister();

        if (IsAdd(node.Operator))
        {
            this.Write(Add(target, left, right));
        }
        else if (IsGreater(node.Operator))
        {
            // a > b
            //  <=>
            // b < a
            this.Write(Less(target, right, left));
        }
        else
        {
            // TODO
            throw new System.NotImplementedException();
        }

        return target;
    }

    public override IOperand VisitReturnExpression(BoundReturnExpression node)
    {
        var operand = this.Compile(node.Value);
        this.Write(Ret(operand));
        return default!;
    }

    public override IOperand VisitLiteralExpression(BoundLiteralExpression node) => new Constant(node.Value);
    public override IOperand VisitUnitExpression(BoundUnitExpression node) => default(Void);

    // TODO: Do something with this block

    private static bool IsEqual(Symbol op) => op == IntrinsicSymbols.Int32_Equal
                                       || op == IntrinsicSymbols.Float64_Equal;
    private static bool IsNotEqual(Symbol op) => op == IntrinsicSymbols.Int32_NotEqual
                                              || op == IntrinsicSymbols.Float64_NotEqual;
    private static bool IsLess(Symbol op) => op == IntrinsicSymbols.Int32_LessThan
                                          || op == IntrinsicSymbols.Float64_LessThan;
    private static bool IsLessEqual(Symbol op) => op == IntrinsicSymbols.Int32_LessEqual
                                               || op == IntrinsicSymbols.Float64_LessEqual;
    private static bool IsGreater(Symbol op) => op == IntrinsicSymbols.Int32_GreaterThan
                                             || op == IntrinsicSymbols.Float64_GreaterThan;
    private static bool IsGreaterEqual(Symbol op) => op == IntrinsicSymbols.Int32_GreaterEqual
                                                  || op == IntrinsicSymbols.Float64_GreaterEqual;

    private static bool IsPlus(Symbol op) => op == IntrinsicSymbols.Int32_Plus
                                          || op == IntrinsicSymbols.Float64_Plus;
    private static bool IsMinus(Symbol op) => op == IntrinsicSymbols.Int32_Minus
                                           || op == IntrinsicSymbols.Float64_Minus;

    private static bool IsAdd(Symbol op) => op == IntrinsicSymbols.Int32_Add
                                         || op == IntrinsicSymbols.Float64_Add;
    private static bool IsSub(Symbol op) => op == IntrinsicSymbols.Int32_Sub
                                         || op == IntrinsicSymbols.Float64_Sub;
    private static bool IsMul(Symbol op) => op == IntrinsicSymbols.Int32_Mul
                                         || op == IntrinsicSymbols.Float64_Mul;
    private static bool IsDiv(Symbol op) => op == IntrinsicSymbols.Int32_Div
                                         || op == IntrinsicSymbols.Float64_Div;
    private static bool IsRem(Symbol op) => op == IntrinsicSymbols.Int32_Rem
                                         || op == IntrinsicSymbols.Float64_Rem;
    private static bool IsMod(Symbol op) => op == IntrinsicSymbols.Int32_Mod
                                         || op == IntrinsicSymbols.Float64_Mod;
}
