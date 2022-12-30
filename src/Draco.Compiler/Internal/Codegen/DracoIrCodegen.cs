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
    private Procedure currentProcedure = null!;
    private InstructionWriter writer = null!;

    private readonly Dictionary<ISymbol.IFunction, Procedure> procedures = new();
    private readonly Dictionary<ISymbol.ILabel, Label> labels = new();
    private readonly Dictionary<ISymbol.IParameter, Parameter> parameters = new();
    private readonly Dictionary<ISymbol.IVariable, Local> locals = new();

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

    private Local CompileLvalue(Ast.Expr expr) => expr switch
    {
        Ast.Expr.Reference r => r.Symbol switch
        {
            // TODO: Globals?
            ISymbol.IVariable v => this.locals[v],
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(expr)),
    };

    public override Value VisitFuncDecl(Ast.Decl.Func node)
    {
        // TODO: Maybe introduce context instead of this juggling?
        var oldWriter = this.writer;
        var oldProcedure = this.currentProcedure;

        var procedure = this.GetProcedure(node.DeclarationSymbol);
        this.currentProcedure = procedure;
        this.writer = procedure.Writer();

        foreach (var param in node.Params)
        {
            var paramValue = procedure.DefineParameter(param.Name, this.TranslateType(param.Type));
            this.parameters.Add(param, paramValue);
        }
        procedure.ReturnType = this.TranslateType(node.ReturnType);

        this.VisitBlockExpr(node.Body);
        if (!this.writer.EndsInBranch) this.writer.Ret(Value.Unit.Instance);

        // TODO: Maybe introduce context instead of this juggling?
        this.writer = oldWriter;
        this.currentProcedure = oldProcedure;
        return this.Default;
    }

    public override Value VisitVariableDecl(Ast.Decl.Variable node)
    {
        // TODO: Globals
        var stackSpace = this.currentProcedure.DefineLocal(node.DeclarationSymbol.Name, this.TranslateType(node.Type));
        this.locals.Add(node.DeclarationSymbol, stackSpace);
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
        var result = this.currentProcedure.DefineLocal(null, this.TranslateType(node.EvaluationType));

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
        if (node.Operator == Intrinsics.Operators.Not_Bool) return this.writer.Equal(sub, new Value.Const(false));
        if (node.Operator == Intrinsics.Operators.Pos_Int32) return sub;
        if (node.Operator == Intrinsics.Operators.Neg_Int32) return this.writer.Neg(sub);
        // TODO
        throw new NotImplementedException();
    }

    public override Value VisitBinaryExpr(Ast.Expr.Binary node)
    {
        var left = this.VisitExpr(node.Left);
        var right = this.VisitExpr(node.Right);
        if (node.Operator == Intrinsics.Operators.Add_Int32) return this.writer.Add(left, right);
        if (node.Operator == Intrinsics.Operators.Sub_Int32) return this.writer.Sub(left, right);
        if (node.Operator == Intrinsics.Operators.Mul_Int32) return this.writer.Mul(left, right);
        if (node.Operator == Intrinsics.Operators.Div_Int32) return this.writer.Div(left, right);
        if (node.Operator == Intrinsics.Operators.Rem_Int32) return this.writer.Rem(left, right);
        if (node.Operator == Intrinsics.Operators.Mod_Int32)
        {
            // a mod b
            // <=>
            // (a rem b + b) rem b
            var tmp1 = this.writer.Rem(left, right);
            var tmp2 = this.writer.Add(tmp1, right);
            return this.writer.Rem(tmp2, right);
        }
        if (node.Operator == Intrinsics.Operators.Less_Int32) return this.writer.Less(left, right);
        if (node.Operator == Intrinsics.Operators.Greater_Int32) return this.writer.Less(right, left);
        if (node.Operator == Intrinsics.Operators.LessEqual_Int32)
        {
            var tmp = this.writer.Less(right, left);
            return this.writer.Equal(tmp, new Value.Const(false));
        }
        if (node.Operator == Intrinsics.Operators.GreaterEqual_Int32)
        {
            var tmp = this.writer.Less(left, right);
            return this.writer.Equal(tmp, new Value.Const(false));
        }
        if (node.Operator == Intrinsics.Operators.Equal_Int32) return this.writer.Equal(left, right);
        if (node.Operator == Intrinsics.Operators.NotEqual_Int32)
        {
            // a != b
            // <=>
            // (a == b) == false
            var tmp = this.writer.Equal(left, right);
            return this.writer.Equal(tmp, new Value.Const(false));
        }
        // TODO
        throw new NotImplementedException();
    }

    public override Value VisitCallExpr(Ast.Expr.Call node)
    {
        var called = this.VisitExpr(node.Called);
        var args = node.Args.Select(this.VisitExpr).ToImmutableArray();
        return this.writer.Call(called, args);
    }

    public override Value VisitAssignExpr(Ast.Expr.Assign node)
    {
        var right = this.VisitExpr(node.Value);
        var toStore = right;
        if (node.CompoundOperator is not null)
        {
            var left = this.VisitExpr(node.Target);
            if (node.CompoundOperator == Intrinsics.Operators.Add_Int32) toStore = this.writer.Add(left, right);
            // TODO
            else throw new NotImplementedException();
        }
        var target = this.CompileLvalue(node.Target);
        this.writer.Store(target, toStore);
        return right;
    }

    public override Value VisitReferenceExpr(Ast.Expr.Reference node) => node.Symbol switch
    {
        ISymbol.IParameter p => new Value.Param(this.parameters[p]),
        // TODO: Globals?
        ISymbol.IVariable v => this.writer.Load(this.locals[v]),
        ISymbol.IFunction f => new Value.Proc(this.procedures[f]),
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
    public override Value VisitUnitExpr(Ast.Expr.Unit node) => Value.Unit.Instance;
    public override Value VisitLiteralExpr(Ast.Expr.Literal node) => new Value.Const(node.Value);
}
