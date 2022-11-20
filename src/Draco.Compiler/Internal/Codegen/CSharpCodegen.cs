using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Semantics;
using static Draco.Compiler.Api.Syntax.ParseTree;
using System.IO;

namespace Draco.Compiler.Internal.Codegen;

// NOTE: Currently this is only here to have something hacky but runnable for the compiler
// Eventually we'll translate to typed C# when implemented type inference, and even to IL
/// <summary>
/// Generates low-level C# code from the Draco <see cref="ParseTree"/>.
/// </summary>
internal sealed class CSharpCodegen : ParseTreeVisitorBase<string>
{
    private readonly SemanticModel semanticModel;
    private readonly StreamWriter output;
    private readonly Dictionary<ISymbol, string> symbolNames = new();
    private int registerCount = 0;
    private int labelCount = 0;

    public CSharpCodegen(SemanticModel semanticModel, Stream output)
    {
        this.semanticModel = semanticModel;
        this.output = new(output);
    }

    private string AllocateRegister() => $"reg_{this.registerCount++}";
    private string AllocateLabel() => $"label_{this.labelCount++}";

    private string AllocateNameForSymbol(ISymbol symbol)
    {
        if (!this.symbolNames.TryGetValue(symbol, out var name))
        {
            // For now we reserve their proper names for globals
            // For the rest we allocate an enumerated ID
            var scopeKind = ((Symbol)symbol).InternalSymbol.EnclosingScope?.Kind ?? Semantics.ScopeKind.Global;
            name = scopeKind == Semantics.ScopeKind.Local
                ? $"sym_{this.symbolNames.Count}"
                : symbol.Name;
            this.symbolNames.Add(symbol, name);
        }
        return name;
    }

    private string DefinedSymbol(ParseTree parseTree)
    {
        // NOTE: Yeah this API is not async...
        var symbol = this.semanticModel.GetDefinedSymbolOrNull(parseTree);
        if (symbol is null) throw new NotImplementedException();
        return this.AllocateNameForSymbol(symbol);
    }

    private string ReferencedSymbol(ParseTree parseTree)
    {
        // NOTE: Yeah this API is not async...
        var symbol = this.semanticModel.GetReferencedSymbol(parseTree);
        return this.AllocateNameForSymbol(symbol);
    }

    public void Generate()
    {
        this.AppendPrelude();
        this.AppendMainInvocation();
        this.AppendProgramStart();
        this.Visit(this.semanticModel.Root);
        this.AppendProgramEnd();
        this.output.Flush();
    }

    private void AppendPrelude() => this.output.WriteLine("""
        using static Prelude;
        internal static class Prelude
        {
            public readonly record struct Unit;

            public record struct Char32(int Codepoint)
            {
                public override string ToString() =>
                    char.ConvertFromUtf32(this.Codepoint);
            }

            public static Unit print(dynamic value)
            {
                System.Console.Write(value);
                return default;
            }

            public static Unit println(dynamic value)
            {
                System.Console.WriteLine(value);
                return default;
            }

            public static dynamic fmt(dynamic format, params dynamic[] args) =>
                string.Format(format, args);
        }
        """);

    private void AppendMainInvocation() => this.output.WriteLine("""
        public sealed class Program
        {
            public static void Main(string[] args) =>
                DracoProgram.main();
        }
        """);

    private void AppendProgramStart() => this.output.WriteLine("""
        internal sealed class DracoProgram
        {
        """);

    private void AppendProgramEnd() => this.output.WriteLine("}");

    private void Indent1() => this.output.Write("    ");
    private void Indent2() => this.output.Write("        ");

    public override string VisitFuncDecl(Decl.Func node)
    {
        this.Indent1();
        this.output.Write($"internal static dynamic {node.Identifier.Text}");
        this.output.Write('(');
        this.output.Write(string.Join(
            ", ",
            node.Params.Value.Elements
                .Select(punct => punct.Value)
                .Select(param => $"dynamic {this.DefinedSymbol(param)}")));
        this.output.WriteLine(')');

        this.Indent1();
        this.output.WriteLine('{');

        this.VisitFuncBody(node.Body);

        this.Indent2();
        this.output.WriteLine("return default(Unit);");

        this.Indent1();
        this.output.WriteLine('}');

        return this.Default;
    }

    public override string VisitInlineBodyFuncBody(FuncBody.InlineBody node)
    {
        var value = this.VisitExpr(node.Expression);
        this.Indent2();
        this.output.WriteLine($"return {value};");
        return this.Default;
    }

    public override string VisitLabelDecl(Decl.Label node)
    {
        this.Indent1();
        this.output.WriteLine($"{this.DefinedSymbol(node)}:;");
        return this.Default;
    }

    public override string VisitVariableDecl(Decl.Variable decl)
    {
        var varReg = this.DefinedSymbol(decl);
        this.Indent2();
        this.output.WriteLine($"dynamic {varReg} = default(Unit);");
        if (decl.Initializer is not null)
        {
            var value = this.VisitExpr(decl.Initializer.Value);
            this.Indent2();
            this.output.WriteLine($"{varReg} = {value};");
        }
        return this.Default;
    }

    public override string VisitGotoExpr(Expr.Goto node)
    {
        var label = this.ReferencedSymbol(node.Target);
        this.Indent2();
        this.output.WriteLine($"goto {label};");
        return this.Default;
    }

    public override string VisitReturnExpr(Expr.Return node)
    {
        var result = "default(Unit)";
        if (node.Expression is not null) result = this.VisitExpr(node.Expression);
        this.Indent2();
        this.output.WriteLine($"return {result};");
        return this.Default;
    }

    public override string VisitCallExpr(Expr.Call node)
    {
        var func = this.VisitExpr(node.Called);
        var args = node.Args.Value.Elements.Select(a => this.VisitExpr(a.Value)).ToList();
        var resultReg = this.AllocateRegister();
        this.Indent2();
        this.output.WriteLine($"dynamic {resultReg} = {func}({string.Join(", ", args)});");
        return resultReg;
    }

    public override string VisitIndexExpr(Expr.Index node)
    {
        var indexed = this.VisitExpr(node.Called);
        var args = node.Args.Value.Elements.Select(a => this.VisitExpr(a.Value)).ToList();
        return $"{indexed}[{string.Join(", ", args)}]";
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
        this.Indent2();
        this.output.WriteLine($"dynamic {result} = {op} {subexpr};");
        return subexpr;
    }

    public override string VisitBinaryExpr(Expr.Binary node)
    {
        // NOTE: Incomplete, mod and rem don't work
        var result = this.AllocateRegister();
        var left = this.VisitExpr(node.Left);
        var right = this.VisitExpr(node.Right);
        var op = node.Operator.Text;
        this.Indent2();
        this.output.WriteLine($"dynamic {result} = {left} {op} {right};");
        return result;
    }

    public override string VisitRelationalExpr(Expr.Relational node)
    {
        var result = this.AllocateRegister();
        this.Indent2();
        this.output.WriteLine($"dynamic {result} = false;");
        var last = this.VisitExpr(node.Left);
        foreach (var cmp in node.Comparisons)
        {
            var op = cmp.Operator.Text;
            var right = this.VisitExpr(cmp.Right);
            this.Indent2();
            this.output.WriteLine($"if ({last} {op} {right}) {{");
            last = right;
        }
        this.Indent2();
        this.output.WriteLine($"{result} = true;");
        foreach (var _ in node.Comparisons)
        {
            this.Indent2();
            this.output.WriteLine("}");
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
        this.Indent2();
        this.output.WriteLine($"dynamic {result} = default(Unit);");
        var elseLabel = this.AllocateLabel();
        var endLabel = this.AllocateLabel();
        var condition = this.VisitExpr(node.Condition.Value);
        this.Indent2();
        this.output.WriteLine($"if (!{condition}) goto {elseLabel};");
        var thenValue = this.VisitExpr(node.Then);
        this.Indent2();
        this.output.WriteLine($"{result} = {thenValue ?? "default(Unit)"};");
        this.Indent2();
        this.output.WriteLine($"goto {endLabel};");
        this.Indent1();
        this.output.WriteLine($"{elseLabel}:;");
        if (node.Else is not null)
        {
            var elseValue = this.VisitExpr(node.Else.Expression);
            this.Indent2();
            this.output.WriteLine($"{result} = {elseValue ?? "default(Unit)"};");
        }
        this.Indent1();
        this.output.WriteLine($"{endLabel}:;");
        return result;
    }

    public override string VisitWhileExpr(Expr.While node)
    {
        var startLabel = this.AllocateLabel();
        var endLabel = this.AllocateLabel();
        this.Indent1();
        this.output.WriteLine($"{startLabel}:;");
        var cond = this.VisitExpr(node.Condition.Value);
        this.Indent2();
        this.output.WriteLine($"if (!{cond}) goto {endLabel};");
        this.VisitExpr(node.Expression);
        this.Indent2();
        this.output.WriteLine($"goto {startLabel};");
        this.Indent1();
        this.output.WriteLine($"{endLabel}:;");
        return "default(Unit)";
    }

    public override string VisitStringExpr(Expr.String node)
    {
        var result = this.AllocateRegister();
        this.Indent2();
        this.output.WriteLine($"dynamic {result} = new System.Text.StringBuilder();");
        var firstInLine = true;
        foreach (var part in node.Parts)
        {
            if (part is StringPart.Interpolation i)
            {
                var subexpr = this.VisitExpr(i.Expression);
                this.Indent2();
                this.output.WriteLine($"{result}.Append({subexpr}.ToString());");
                firstInLine = false;
            }
            else
            {
                var c = (StringPart.Content)part;
                var text = c.Value.ValueText!;
                if (firstInLine) text = text.Substring(c.Cutoff);
                this.Indent2();
                this.output.WriteLine($"{result}.Append(\"{StringUtils.Unescape(text)}\");");
                firstInLine = c.Value.Type == TokenType.StringNewline;
            }
        }
        return $"{result}.ToString()";
    }

    // TODO: Not 100% correct, some escapes are actually illegal in C# that Draco has
    public override string VisitLiteralExpr(Expr.Literal node)
    {
        var token = node.Value;
        if (token.Type == TokenType.LiteralCharacter)
        {
            var codepoint = char.ConvertToUtf32(token.ValueText!, 0);
            return $"new Char32({codepoint})";
        }
        else
        {
            return token.Text;
        }
    }

    public override string VisitNameExpr(Expr.Name node) => this.ReferencedSymbol(node);
}
