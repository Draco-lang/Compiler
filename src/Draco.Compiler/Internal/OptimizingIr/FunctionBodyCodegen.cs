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
    private BasicBlock? currentBasicBlock;

    public FunctionBodyCodegen(Procedure procedure)
    {
        this.procedure = procedure;
        this.currentBasicBlock = procedure.Entry;
    }

    private void Compile(BoundStatement stmt) => stmt.Accept(this);
    private IOperand Compile(BoundLvalue lvalue) => lvalue.Accept(this);
    private IOperand Compile(BoundExpression expr) => expr.Accept(this);

    private void Write(IInstruction instr)
    {
        // Happens, when the basic block got detached and there's code left over to compile
        // Example:
        //     goto foo;
        //     y = x;    // This is inaccessible, current BB is null here!
        //     foo:
        if (this.currentBasicBlock is null) return;
        this.currentBasicBlock.InsertLast(instr);
    }

    private BasicBlock DefineBasicBlock(LabelSymbol label) => this.procedure.DefineBasicBlock(label);
    private Local DefineLocal(LocalSymbol local) => this.procedure.DefineLocal(local);
    private Register DefineRegister() => this.procedure.DefineRegister();

    // Statements //////////////////////////////////////////////////////////////

    public override IOperand VisitLocalDeclaration(BoundLocalDeclaration node)
    {
        if (node.Value is null) return default!;

        var right = this.Compile(node.Value);
        var left = this.DefineLocal(node.Local);
        this.Write(Store(left, right));

        return default!;
    }

    public override IOperand VisitLabelStatement(BoundLabelStatement node)
    {
        // Define a new basic block
        var newBasicBlock = this.DefineBasicBlock(node.Label);

        // Here we thread the previous basic block to this one
        // Basically an implicit goto
        this.Write(Jump(newBasicBlock));
        this.currentBasicBlock = newBasicBlock;

        return default!;
    }

    public override IOperand VisitConditionalGotoStatement(BoundConditionalGotoStatement node)
    {
        var condition = this.Compile(node.Condition);
        var thenBlock = this.DefineBasicBlock(node.Target);
        var elseBlock = this.DefineBasicBlock(new SynthetizedLabelSymbol("else"));
        this.Write(Branch(condition, thenBlock, elseBlock));
        // We fall-through to the else block implicitly
        this.currentBasicBlock = elseBlock;

        return default!;
    }

    // Lvalues /////////////////////////////////////////////////////////////////

    public override IOperand VisitLocalLvalue(BoundLocalLvalue node) => this.DefineLocal(node.Local);

    // Expressions /////////////////////////////////////////////////////////////

    public override IOperand VisitGotoExpression(BoundGotoExpression node)
    {
        var target = this.DefineBasicBlock(node.Target);
        this.Write(Jump(target));
        // Detach current block
        this.currentBasicBlock = null;
        return default(Void);
    }

    public override IOperand VisitBlockExpression(BoundBlockExpression node)
    {
        // Compile all of the statements within
        foreach (var stmt in node.Statements) this.Compile(stmt);
        // Compile value
        return this.Compile(node.Value);
    }

    public override IOperand VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        var right = this.Compile(node.Right);
        var left = this.Compile(node.Left);

        if (node.CompoundOperator is not null)
        {
            // TODO
            throw new System.NotImplementedException();
        }

        this.Write(Store(left, right));
        return right;
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
        else if (IsSub(node.Operator))
        {
            this.Write(Sub(target, left, right));
        }
        else if (IsMul(node.Operator))
        {
            this.Write(Mul(target, left, right));
        }
        else if (IsDiv(node.Operator))
        {
            this.Write(Div(target, left, right));
        }
        else if (IsRem(node.Operator))
        {
            this.Write(Rem(target, left, right));
        }
        else if (IsMod(node.Operator))
        {
            // a mod b
            //  <=>
            // (a rem b + b) rem b
            var tmp1 = this.DefineRegister();
            var tmp2 = this.DefineRegister();
            this.Write(Rem(tmp1, left, right));
            this.Write(Add(tmp2, tmp1, right));
            this.Write(Add(target, tmp1, right));
        }
        else if (IsLess(node.Operator))
        {
            this.Write(Less(target, left, right));
        }
        else if (IsGreater(node.Operator))
        {
            // a > b
            //  <=>
            // b < a
            this.Write(Less(target, right, left));
        }
        else if (IsLessEqual(node.Operator))
        {
            // a <= b
            //  <=>
            // (b < a) == false
            var tmp = this.DefineRegister();
            this.Write(Less(tmp, right, left));
            this.Write(Equal(target, tmp, new Constant(false)));
        }
        else if (IsGreaterEqual(node.Operator))
        {
            // a >= b
            //  <=>
            // (a < b) == false
            var tmp = this.DefineRegister();
            this.Write(Less(tmp, left, right));
            this.Write(Equal(target, tmp, new Constant(false)));
        }
        else if (IsEqual(node.Operator))
        {
            this.Write(Equal(target, left, right));
        }
        else if (IsNotEqual(node.Operator))
        {
            // a != b
            //  <=>
            // (a == b) == false
            var tmp = this.DefineRegister();
            this.Write(Equal(tmp, left, right));
            this.Write(Equal(target, tmp, new Constant(false)));
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

    public override IOperand VisitLocalExpression(BoundLocalExpression node)
    {
        var result = this.DefineRegister();
        var local = this.DefineLocal(node.Local);
        this.Write(Load(result, local));
        return result;
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
