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

        this.Visit(node.Body);

        this
            .Indent2()
            .AppendLine("return default(Unit);");

        this
            .Indent1()
            .AppendLine("}");

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

    public override string VisitCallExpr(Expr.Call node)
    {
        var func = this.Visit(node.Called);
        var args = node.Args.Value.Elements.Select(a => this.Visit(a.Value)).ToList();
        var resultReg = this.AllocateRegister();
        this
            .Indent2()
            .AppendLine($"dynamic {resultReg} = {func}({string.Join(", ", args)});");
        return resultReg;
    }

    public override string VisitNameExpr(Expr.Name node) => this.AllocateVariable(node.Identifier.Text);
    public override string VisitNameTypeExpr(TypeExpr.Name node) => this.AllocateVariable(node.Identifier.Text);
}
