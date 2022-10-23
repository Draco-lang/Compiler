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
    private readonly StringBuilder output = new StringBuilder();
    public string GeneratedCode => this.output.ToString();

    public CSharpTranspiler()
    {
        this.AppendPrelude();
    }

    private void AppendPrelude()
    {
        this.output.AppendLine("record struct Unit;");
        this.output.AppendLine("Unit Valueify(Action action) { action(); return new Unit(); }");
        this.output.AppendLine("dynamic Valueify(Func<dynamic> func) => func();");
    }

    public override Unit VisitStmt(Stmt stmt)
    {
        base.VisitStmt(stmt);
        this.output.AppendLine();
        return this.Default;
    }

    public override Unit VisitFuncDecl(Decl.Func decl)
    {
        this.output.Append($"dynamic {(decl.Identifier.Text == "main" ? "Main" : decl.Identifier.Text)}(");
        for (int i = 0; i < decl.Params.Value.Elements.Count; i++)
        {
            this.VisitFuncParam(decl.Params.Value.Elements[i].Value);
            if (i != decl.Params.Value.Elements.Count - 1) this.output.Append(", ");
        }
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
        if (decl.Initializer is not null)
        {
            this.output.Append(" = ");
            base.VisitNullable(decl.Initializer?.Expression);
        }
        this.output.Append(';');
        return this.Default;
    }

    public override Unit VisitFuncParam(FuncParam param)
    {
        this.output.Append($"dynamic {param.Identifier.Text}");
        return this.Default;
    }

    public override Unit VisitInlineFuncBody(FuncBody.InlineBody body)
    {
        this.output.Append("=>");
        base.VisitInlineFuncBody(body);
        this.output.Append(";");
        return this.Default;
    }

    public override Unit VisitBlockFuncBody(FuncBody.BlockBody body)
    {
        this.output.AppendLine("{");
        var (stmts, value) = body.Block.Enclosed.Value;
        this.VisitEnumerable(stmts);
        if (body.Block.Enclosed.Value.Value is not null)
        {
            this.output.Append("return Valueify(() => {");
            base.VisitExpr(body.Block.Enclosed.Value.Value);
            this.output.Append("});");
        }
        this.output.AppendLine("}");
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
        this.output.AppendLine("(() =>\n{");
        var (stmts, value) = expr.Enclosed.Value;
        this.VisitEnumerable(stmts);
        if (value != null)
        {
            this.output.Append("return ");
            this.VisitNullable(value);
            this.output.Append(';');
        }
        this.output.AppendLine();
        this.output.Append("})()");
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
        this.output.Append("Valueify(()=>(");
        base.VisitExpr(expr.Condition.Value);
        this.output.Append(") ? Valueify(()=>");
        base.VisitExpr(expr.Then);
        this.output.Append(") : ");
        if (expr.Else is null) this.output.Append("new Unit()");
        else
        {
            this.output.Append("Valueify(()=>");
            base.VisitExpr(expr.Else.Value.Expression);
            this.output.Append(')');
        }
        this.output.Append(')');
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
        this.output.Append($" {expr.Operator.Text} ");
        base.VisitExpr(expr.Right);
        return this.Default;
    }

    public override Unit VisitExprStmt(Stmt.Expr stmt)
    {
        base.VisitExprStmt(stmt);
        this.output.Append(';');
        return this.Default;
    }

    public override Unit VisitStringExpr(Expr.String expr)
    {
        for (int i = 0; i < expr.Parts.Count; i++)
        {
            base.VisitNode(expr.Parts[i]);
            if (i != expr.Parts.Count - 1) this.output.Append(" + ");
        }
        return this.Default;
    }

    public override Unit VisitContentStringPart(StringPart.Content stringPart)
    {
        this.output.Append('\"');
        // Escaping chars
        foreach (char c in stringPart.Token.ValueText!)
        {
            this.output.Append(c switch
            {
                '\"' => @"\\\""",
                '\'' => @"\\\'",
                '\\' => @"\\",
                '\a' => @"\a",
                '\b' => @"\b",
                '\f' => @"\f",
                '\n' => @"\n",
                '\r' => @"\r",
                '\t' => @"\t",
                '\v' => @"\v",
                '\0' => @"\0",
                _ => c
            });
        }
        this.output.Append('\"');
        return this.Default;
    }

    public override Unit VisitMemberAccessExpr(Expr.MemberAccess expr)
    {
        base.VisitExpr(expr.Object);
        this.output.Append('.');
        this.output.Append(expr.MemberName.Text);
        return this.Default;
    }

}
