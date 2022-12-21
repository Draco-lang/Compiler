using System;
using System.Collections.Generic;
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

    public override Value VisitFuncDecl(Ast.Decl.Func node)
    {
        var oldWriter = this.writer;
        var procedure = this.GetProcedure(node.DeclarationSymbol);
        this.writer = procedure.Writer();

        // TODO: Parameters
        procedure.ReturnType = this.TranslateType(node.ReturnType);

        this.VisitBlockExpr(node.Body);

        this.writer = oldWriter;
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

        var condition = this.VisitExpr(node.Condition);
        this.writer.JmpIf(condition, thenLabel, elseLabel);

        this.writer.PlaceLabel(thenLabel);
        // TODO: Store value?
        this.VisitExpr(node.Then);

        this.writer.PlaceLabel(elseLabel);
        // TODO: Store value?
        this.VisitExpr(node.Else);

        // TODO: Value?
        return this.Default;
    }

    public override Value VisitUnitExpr(Ast.Expr.Unit node) => Value.Unit.Instance;
    public override Value VisitLiteralExpr(Ast.Expr.Literal node) => new Value.Constant(node.Value);
}
