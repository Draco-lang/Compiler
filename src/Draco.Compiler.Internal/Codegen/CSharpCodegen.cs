using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Syntax;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.Syntax.ParseTree;

namespace Draco.Compiler.Internal.Codegen;

// NOTE: Currently this is only here to have something hacky but runnable for the compiler
// Eventually we'll translate to typed C# when implemented type inference, and even to IL
/// <summary>
/// Generates low-level C# code from the Draco <see cref="ParseTree"/>.
/// </summary>
internal sealed class CSharpCodegen : ParseTreeVisitorBase<string>
{
    public static string Transpile(ParseTree parseTree)
    {
        var codegen = new CSharpCodegen();
        codegen.AppendPrelude();
        codegen.AppendMainInvocation();
        codegen.AppendProgramStart();
        codegen.Visit(parseTree);
        codegen.AppendProgramEnd();
        return codegen.Code;
    }

    public string Code => this.output.ToString();

    private readonly StringBuilder output = new();
    private int registerCount = 0;
    private int labelCount = 0;

    private void AppendPrelude() => this.output.AppendLine("""
        using System;
        using static Prelude;
        internal static class Prelude
        {
            public record struct Unit;

            public static Unit print(dynamic value)
            {
                Console.Write(value);
                return default;
            }

            public static Unit println(dynamic value)
            {
                Console.WriteLine(value);
                return default;
            }
        }
        """);

    private void AppendMainInvocation() => this.output.AppendLine("""
        internal sealed class Program
        {
            internal static void Main(string[] args) =>
                DracoProgram.main();
        }
        """);

    private void AppendProgramStart() => this.output.AppendLine("""
        internal sealed class DracoProgram
        {
        """);

    private void AppendProgramEnd() => this.output.AppendLine("}");

    private StringBuilder Indent1() => this.output.Append("    ");
    private StringBuilder Indent2() => this.output.Append("        ");

    private string AllocateRegister() => $"reg_{this.registerCount++}";
    private string AllocateLabel() => $"label_{this.labelCount++}";
    // NOTE: This will have to take a symbol to be fully functional
    // For now we just make a leap of faith here
    private string AllocateVariable(string name) => name;
    // NOTE: See previous
    private string AllocateLabel(string name) => name;

    public override string VisitFuncDecl(Decl.Func node)
    {
        this
            .Indent1()
            .Append($"internal static dynamic {node.Identifier.Text}")
            .Append('(')
            .AppendJoin(
                ", ",
                node.Params.Value.Elements
                    .Select(e => e.Value.Identifier.Text)
                    .Select(n => $"dynamic {this.AllocateVariable(n)}"))
            .AppendLine(")");
        this
            .Indent1()
            .AppendLine("{");
        this.VisitFuncBody(node.Body);
        this
            .Indent2()
            .AppendLine("return default(Unit);");
        this
            .Indent1()
            .AppendLine("}");
        return this.Default;
    }

    public override string VisitInlineBodyFuncBody(FuncBody.InlineBody node)
    {
        var value = this.VisitExpr(node.Expression);
        this
            .Indent2()
            .AppendLine($"return {value};");
        return this.Default;
    }

    public override string VisitLabelDecl(Decl.Label node)
    {
        this
            .Indent1()
            .AppendLine($"{this.AllocateLabel(node.Identifier.Text)}:;");
        return this.Default;
    }

    public override string VisitVariableDecl(Decl.Variable decl)
    {
        var varReg = this.AllocateVariable(decl.Identifier.Text);
        this
            .Indent2()
            .AppendLine($"dynamic {varReg} = default(Unit);");
        if (decl.Initializer is not null)
        {
            var value = this.VisitExpr(decl.Initializer.Value);
            this.Indent2().AppendLine($"{varReg} = {value};");
        }
        return this.Default;
    }

    public override string VisitGotoExpr(Expr.Goto node)
    {
        var label = this.AllocateLabel(node.Identifier.Text);
        this
            .Indent2()
            .AppendLine($"goto {label};");
        return this.Default;
    }

    public override string VisitReturnExpr(Expr.Return node)
    {
        var result = "default(Unit)";
        if (node.Expression is not null) result = this.VisitExpr(node.Expression);
        this
            .Indent2()
            .AppendLine($"return {result};");
        return this.Default;
    }

    public override string VisitCallExpr(Expr.Call node)
    {
        var func = this.VisitExpr(node.Called);
        var args = node.Args.Value.Elements.Select(a => this.VisitExpr(a.Value)).ToList();
        var resultReg = this.AllocateRegister();
        this
            .Indent2()
            .AppendLine($"dynamic {resultReg} = {func}({string.Join(", ", args)});");
        return resultReg;
    }

    public override string VisitMemberAccessExpr(Expr.MemberAccess node)
    {
        var left = this.VisitExpr(node.Object);
        return $"{left}.{node.MemberName.Text}";
    }

    public override string VisitUnaryExpr(Expr.Unary node)
    {
        var result = this.AllocateRegister();
        var op = node.Operator.Text;
        var subexpr = this.VisitExpr(node.Operand);
        this
            .Indent2()
            .AppendLine($"dynamic {result} = {op} {subexpr};");
        return subexpr;
    }

    public override string VisitBinaryExpr(Expr.Binary node)
    {
        // NOTE: Incomplete, mod and rem don't work
        var result = this.AllocateRegister();
        var left = this.VisitExpr(node.Left);
        var right = this.VisitExpr(node.Right);
        var op = node.Operator.Text;
        this
            .Indent2()
            .AppendLine($"dynamic {result} = {left} {op} {right};");
        return result;
    }

    public override string VisitRelationalExpr(Expr.Relational node)
    {
        var result = this.AllocateRegister();
        this
            .Indent2()
            .AppendLine($"dynamic {result} = false;");
        var last = this.VisitExpr(node.Left);
        foreach (var cmp in node.Comparisons)
        {
            var op = cmp.Operator.Text;
            var right = this.VisitExpr(cmp.Right);
            this
                .Indent2()
                .AppendLine($"if ({last} {op} {right}) {{");
            last = right;
        }
        this
            .Indent2()
            .AppendLine($"{result} = true;");
        foreach (var _ in node.Comparisons)
        {
            this
                .Indent2()
                .AppendLine("}");
        }
        return result;
    }

    public override string VisitBlockExpr(Expr.Block node)
    {
        var result = "default(Unit)";
        var content = node.Enclosed.Value;
        foreach (var stmt in content.Statements) this.VisitStmt(stmt);
        if (content.Value is not null) result = this.VisitExpr(content.Value);
        return result;
    }

    public override string VisitIfExpr(Expr.If node)
    {
        var result = this.AllocateRegister();
        this
            .Indent2()
            .AppendLine($"dynamic {result} = default(Unit);");
        var elseLabel = this.AllocateLabel();
        var endLabel = this.AllocateLabel();
        var condition = this.VisitExpr(node.Condition.Value);
        this
            .Indent2()
            .AppendLine($"if (!{condition}) goto {elseLabel};");
        var thenValue = this.VisitExpr(node.Then);
        this.Indent2().AppendLine($"{result} = {thenValue ?? "default(Unit)"};");
        this.Indent2().AppendLine($"goto {endLabel};");
        this
            .Indent1()
            .AppendLine($"{elseLabel}:;");
        if (node.Else is not null)
        {
            var elseValue = this.VisitExpr(node.Else.Expression);
            this.Indent2().AppendLine($"{result} = {elseValue ?? "default(Unit)"};");
        }
        this
            .Indent1()
            .AppendLine($"{endLabel}:;");
        return result;
    }

    public override string VisitWhileExpr(Expr.While node)
    {
        var startLabel = this.AllocateLabel();
        var endLabel = this.AllocateLabel();
        this.Indent1().AppendLine($"{startLabel}:;");
        var cond = this.VisitExpr(node.Condition.Value);
        this
            .Indent2()
            .AppendLine($"if (!{cond}) goto {endLabel};");
        this.VisitExpr(node.Expression);
        this.Indent2().AppendLine($"goto {startLabel};");
        this.Indent1().AppendLine($"{endLabel}:;");
        return "default(Unit)";
    }

    public override string VisitStringExpr(Expr.String node)
    {
        var result = this.AllocateRegister();
        this
            .Indent2()
            .AppendLine($"dynamic {result} = new System.Text.StringBuilder();");
        foreach (var part in node.Parts)
        {
            if (part is StringPart.Interpolation i)
            {
                var subexpr = this.VisitExpr(i.Expression);
                this
                    .Indent2()
                    .AppendLine($"{result}.Append({subexpr}.ToString());");
            }
            else
            {
                var c = (StringPart.Content)part;
                this
                    .Indent2()
                    .AppendLine($"{result}.Append(\"{Unescape(c.Value.ValueText!)}\");");
            }
        }
        return $"{result}.ToString()";
    }

    // TODO: Not 100% correct, some escapes are actually illegal in C# that Draco has
    public override string VisitLiteralExpr(Expr.Literal node) => node.Value.Text;

    public override string VisitNameExpr(Expr.Name node) => this.AllocateVariable(node.Identifier.Text);

    private static string Unescape(string text) => text
        .Replace("\n", @"\n")
        .Replace("\r", @"\r");
}
