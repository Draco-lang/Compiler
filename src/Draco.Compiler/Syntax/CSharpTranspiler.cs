using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Draco.Compiler.Utilities;
using static Draco.Compiler.Syntax.ParseTree;

namespace Draco.Compiler.Syntax;
internal sealed class CSharpTranspiler : ParseTreeVisitorBase<Unit>
{
    public override Unit Default => new Unit();
    public StringBuilder output = new StringBuilder();

    public override Unit VisitStmt(Stmt stmt)
    {
        base.VisitStmt(stmt);
        this.output.AppendLine();
        return this.Default;
    }
    public override Unit VisitFuncDecl(Decl.Func decl)
    {
        this.output.Append($"dynamic {decl.Identifier.Text}(");
        base.VisitPunctuatedList(decl.Params.Value);
        if (decl.Params.Value.Elements.Count > 0)
            this.output.Remove(this.output.Length - 1, 1);
        this.output.Append(')');
        base.VisitFuncBody(decl.Body);
        return this.Default;
    }
    public override Unit VisitLabelDecl(Decl.Label decl)
    {
        this.output.Append($"{decl.Identifier.Text}:;");
        return this.Default;
    }
    public override Unit VisitVariableDecl(Decl.Variable decl)
    {
        this.output.Append($"dynamic {decl.Identifier.Text}");
        if (decl.Initializer != null)
        {
            this.output.Append(" = ");
            base.VisitNullable(decl.Initializer?.Expression);
        }
        this.output.Append(';');
        return this.Default;
    }
    public override Unit VisitFuncParam(FuncParam param)
    {
        this.output.Append($"{param.Identifier.Text},");
        return this.Default;
    }
    public override Unit VisitInlineFuncBody(FuncBody.InlineBody body)
    {
        this.output.Append("=>");
        base.VisitInlineFuncBody(body);
        this.output.Append(";");
        return this.Default;
    }
    public override Unit VisitCallExpr(Expr.Call expr)
    {
        base.VisitExpr(expr.Called);
        this.output.Append('(');
        this.VisitPunctuatedList(expr.Args.Value);
        this.output.Append(')');
        return this.Default;
    }
    public override Unit VisitBlockExpr(Expr.Block expr)
    {
        this.output.AppendLine("{");
        base.VisitBlockExpr(expr);
        this.output.AppendLine();
        this.output.Append('}');
        return this.Default;
    }
    public override Unit VisitGotoExpr(Expr.Goto expr)
    {
        this.output.Append($"goto {expr.Identifier.Text};");
        return this.Default;
    }
    public override Unit VisitReturnExpr(Expr.Return expr)
    {
        this.output.Append($"return ");
        base.VisitNullable(expr.Expression);
        return this.Default;
    }
    public override Unit VisitLiteralExpr(Expr.Literal expr)
    {
        this.output.Append(expr.Value.Text);
        return this.Default;
    }
    public override Unit VisitNameExpr(Expr.Name expr)
    {
        this.output.Append(expr.Identifier.Text);
        return this.Default;
    }
    public override Unit VisitRelationalExpr(Expr.Relational expr)
    {
        base.VisitExpr(expr.Left);
        foreach (var item in expr.Comparisons)
        {
            this.output.Append(item.Operator.Text);
            base.VisitNode(item.Right);
        }
        return this.Default;
    }
    public override Unit VisitIfExpr(Expr.If expr)
    {
        this.output.Append("if (");
        base.VisitExpr(expr.Condition.Value);
        this.output.Append(')');
        this.VisitExpr(expr.Then);
        if (expr.Else != null)
        {
            this.output.Append("else ");
            this.VisitNullable(expr.Else?.Expression);
        }
        
        return this.Default;
    }
    public override Unit VisitWhileExpr(Expr.While expr)
    {
        this.output.Append("while (");
        base.VisitExpr(expr.Condition.Value);
        this.output.Append(')');
        this.VisitExpr(expr.Expression);
        return this.Default;
    }
    public override Unit VisitBinaryExpr(Expr.Binary expr)
    {
        base.VisitExpr(expr.Left);
        this.output.Append(expr.Operator.Text);
        base.VisitExpr(expr.Right);
        return this.Default;
    }
    public override Unit VisitExprStmt(Stmt.Expr stmt)
    {
        base.VisitExprStmt(stmt);
        this.output.Append(';');
        return this.Default;
    }
}
