using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.DracoIr;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;
using Type = Draco.Compiler.Internal.DracoIr.Type;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates Draco IR from the <see cref="Ast"/>.
/// </summary>
internal sealed class DracoIrCodegen : AstVisitorBase<Value>
{
    /// <summary>
    /// Generates IR code in the given <see cref="Assembly"/> for the given <see cref="Ast"/>.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to generate the IR into.</param>
    /// <param name="ast">The <see cref="Ast"/> to generate IR code for.</param>
    public static void Generate(Assembly assembly, Ast ast)
    {
        var codegen = new DracoIrCodegen(assembly);
        codegen.Visit(ast);
    }

    private readonly Assembly assembly;
    private InstructionWriter writer = null!;

    private readonly Dictionary<ISymbol.IFunction, Procedure> procedures = new();
    private readonly Dictionary<ISymbol.ILabel, Label> labels = new();
    private readonly Dictionary<ISymbol, Value> values = new();

    private DracoIrCodegen(Assembly assembly)
    {
        this.assembly = assembly;
    }

    private Type TranslateType(Semantics.Types.Type type)
    {
        if (type == Semantics.Types.Type.Unit) return Type.Unit;
        if (type == Semantics.Types.Type.Bool) return Type.Bool;
        if (type == Semantics.Types.Type.Int32) return Type.Int32;

        throw new NotImplementedException();
    }

    private Procedure GetProcedure(ISymbol.IFunction function)
    {
        if (!this.procedures.TryGetValue(function, out var proc))
        {
            proc = this.assembly.DefineProcedure(function.Name);
            this.procedures.Add(function, proc);
        }
        return proc;
    }

    private Label GetLabel(ISymbol.ILabel label)
    {
        if (!this.labels.TryGetValue(label, out var lbl))
        {
            lbl = this.writer.DeclareLabel();
            this.labels.Add(label, lbl);
        }
        return lbl;
    }

    private Value.Register CompileLvalue(Ast.Expr expr) => expr switch
    {
        // TODO: Cast might fail
        Ast.Expr.Reference r => (Value.Register)this.values[r.Symbol],
        _ => throw new ArgumentOutOfRangeException(nameof(expr)),
    };

    public override Value VisitFuncDecl(Ast.Decl.Func node)
    {
        var oldWriter = this.writer;
        var procedure = this.GetProcedure(node.DeclarationSymbol);
        this.writer = procedure.Writer();

        foreach (var param in node.Params)
        {
            var paramValue = procedure.DefineParameter(param.Name, this.TranslateType(param.Type));
            this.values[param] = paramValue;
        }
        procedure.ReturnType = this.TranslateType(node.ReturnType);

        this.VisitBlockExpr(node.Body);
        if (!this.writer.EndsInBranch) this.writer.Ret(Value.Unit.Instance);

        this.writer = oldWriter;
        return this.Default;
    }

    public override Value VisitVariableDecl(Ast.Decl.Variable node)
    {
        var stackSpace = this.writer.Alloc(this.TranslateType(node.Type));
        this.values[node.DeclarationSymbol] = stackSpace;
        if (node.Value is not null)
        {
            var value = this.VisitExpr(node.Value);
            this.writer.Store(stackSpace, value);
        }
        return this.Default;
    }

    public override Value VisitLabelDecl(Ast.Decl.Label node)
    {
        var label = this.GetLabel(node.LabelSymbol);
        this.writer.PlaceLabel(label);
        return this.Default;
    }

    public override Value VisitBlockExpr(Ast.Expr.Block node)
    {
        foreach (var stmt in node.Statements) this.VisitStmt(stmt);
        return this.VisitExpr(node.Value);
    }

    public override Value VisitIfExpr(Ast.Expr.If node)
    {
        var thenLabel = this.writer.DeclareLabel();
        var elseLabel = this.writer.DeclareLabel();
        var endLabel = this.writer.DeclareLabel();

        // Allcoate value for result
        var result = this.writer.Alloc(this.TranslateType(node.EvaluationType));

        var condition = this.VisitExpr(node.Condition);
        this.writer.JmpIf(condition, thenLabel, elseLabel);

        this.writer.PlaceLabel(thenLabel);
        var thenValue = this.VisitExpr(node.Then);
        this.writer.Store(result, thenValue);
        this.writer.Jmp(endLabel);

        this.writer.PlaceLabel(elseLabel);
        var elseValue = this.VisitExpr(node.Else);
        this.writer.Store(result, elseValue);

        this.writer.PlaceLabel(endLabel);

        return this.writer.Load(result);
    }

    public override Value VisitReturnExpr(Ast.Expr.Return node)
    {
        var value = this.VisitExpr(node.Expression);
        this.writer.Ret(value);
        return Value.Unit.Instance;
    }

    public override Value VisitGotoExpr(Ast.Expr.Goto node)
    {
        var label = this.GetLabel(node.Target);
        this.writer.Jmp(label);
        return Value.Unit.Instance;
    }

    public override Value VisitUnaryExpr(Ast.Expr.Unary node)
    {
        var sub = this.VisitExpr(node.Operand);
        if (node.Operator == Intrinsics.Operators.Not_Bool) return this.writer.NotBool(sub);
        if (node.Operator == Intrinsics.Operators.Pos_Int32) return sub;
        if (node.Operator == Intrinsics.Operators.Neg_Int32) return this.writer.NegInt(sub);
        // TODO
        throw new NotImplementedException();
    }

    public override Value VisitBinaryExpr(Ast.Expr.Binary node)
    {
        var left = this.VisitExpr(node.Left);
        var right = this.VisitExpr(node.Right);
        if (node.Operator == Intrinsics.Operators.Add_Int32) return this.writer.AddInt(left, right);
        if (node.Operator == Intrinsics.Operators.Sub_Int32) return this.writer.SubInt(left, right);
        if (node.Operator == Intrinsics.Operators.Mul_Int32) return this.writer.MulInt(left, right);
        if (node.Operator == Intrinsics.Operators.Div_Int32) return this.writer.DivInt(left, right);
        if (node.Operator == Intrinsics.Operators.Rem_Int32) return this.writer.RemInt(left, right);
        if (node.Operator == Intrinsics.Operators.Mod_Int32)
        {
            // a mod b
            // <=>
            // (a rem b + b) rem b
            var tmp1 = this.writer.RemInt(left, right);
            var tmp2 = this.writer.AddInt(tmp1, right);
            return this.writer.RemInt(tmp2, right);
        }
        if (node.Operator == Intrinsics.Operators.Less_Int32) return this.writer.LessInt(left, right);
        if (node.Operator == Intrinsics.Operators.Greater_Int32) return this.writer.LessInt(right, left);
        if (node.Operator == Intrinsics.Operators.LessEqual_Int32) return this.writer.LessEqualInt(left, right);
        if (node.Operator == Intrinsics.Operators.GreaterEqual_Int32) return this.writer.LessEqualInt(right, left);
        if (node.Operator == Intrinsics.Operators.Equal_Int32) return this.writer.EqualInt(left, right);
        if (node.Operator == Intrinsics.Operators.NotEqual_Int32)
        {
            // a != b
            // <=>
            // !(a == b)
            var tmp = this.writer.EqualInt(left, right);
            return this.writer.NotBool(tmp);
        }
        // TODO
        throw new NotImplementedException();
    }

    public override Value VisitCallExpr(Ast.Expr.Call node)
    {
        var called = this.VisitExpr(node.Called);
        var args = node.Args.Select(this.VisitExpr).ToList();
        return this.writer.Call(called, args);
    }

    public override Value VisitAssignExpr(Ast.Expr.Assign node)
    {
        var right = this.VisitExpr(node.Value);
        var toStore = right;
        if (node.CompoundOperator is not null)
        {
            var left = this.VisitExpr(node.Target);
            if (node.CompoundOperator == Intrinsics.Operators.Add_Int32) toStore = this.writer.AddInt(left, right);
            // TODO
            else throw new NotImplementedException();
        }
        var target = this.CompileLvalue(node.Target);
        this.writer.Store(target, toStore);
        return right;
    }

    public override Value VisitReferenceExpr(Ast.Expr.Reference node) => node.Symbol switch
    {
        ISymbol.IParameter => this.values[node.Symbol],
        ISymbol.IVariable => this.writer.Load(this.values[node.Symbol]),
        ISymbol.IFunction f => this.procedures[f],
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
    public override Value VisitUnitExpr(Ast.Expr.Unit node) => Value.Unit.Instance;
    public override Value VisitLiteralExpr(Ast.Expr.Literal node) => new Value.Constant(node.Value);
}
