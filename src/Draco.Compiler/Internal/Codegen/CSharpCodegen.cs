using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;
using Draco.Compiler.Api.Semantics;
using static Draco.Compiler.Api.Syntax.ParseTree;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Codegen;

// NOTE: Currently this is only here to have something hacky but runnable for the compiler
// Eventually we'll translate to typed C# when implemented type inference, and even to IL
/// <summary>
/// Generates low-level C# code from the Draco <see cref="ParseTree"/>.
/// </summary>
internal sealed class CSharpCodegen : ParseTreeVisitorBase<string>
{
    public static string Transpile(SemanticModel semanticModel)
    {
        var codegen = new CSharpCodegen(semanticModel);
        codegen.Generate();
        return codegen.Code;
    }

    public string Code => this.output.ToString();

    private readonly SemanticModel semanticModel;
    private readonly StringBuilder output = new();
    private readonly Dictionary<ISymbol, string> symbolNames = new();
    private int registerCount = 0;
    private int labelCount = 0;

    private CSharpCodegen(SemanticModel semanticModel)
    {
        this.semanticModel = semanticModel;
    }

    private string AllocateRegister() => $"reg_{this.registerCount++}";
    private string AllocateLabel() => $"label_{this.labelCount++}";

    private string AllocateNameForSymbol(ISymbol symbol)
    {
        if (!this.symbolNames.TryGetValue(symbol, out var name))
        {
            // For now we reserve their proper names for globals
            // For the rest we allocate an enumerated ID
            var scopeKind = ((Semantics.Symbol)symbol).EnclosingScope?.Kind ?? Semantics.ScopeKind.Global;
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
        var symbol = this.semanticModel.GetDefinedSymbol(parseTree).Result;
        if (symbol is null) throw new NotImplementedException();
        return this.AllocateNameForSymbol(symbol);
    }

    private string ReferencedSymbol(ParseTree parseTree)
    {
        // NOTE: Yeah this API is not async...
        var symbol = this.semanticModel.GetReferencedSymbol(parseTree).Result;
        if (symbol is null) throw new NotImplementedException();
        return this.AllocateNameForSymbol(symbol);
    }

    private void Generate()
    {
        this.AppendPrelude();
        this.AppendMainInvocation();
        this.AppendProgramStart();
        this.Visit(this.semanticModel.Root);
        this.AppendProgramEnd();
    }

    private void AppendPrelude() => this.output.AppendLine("""
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

    public override string VisitFuncDecl(Decl.Func node)
    {
        this
            .Indent1()
            .Append($"internal static dynamic {node.Identifier.Text}")
            .Append('(')
            .AppendJoin(
                ", ",
                node.Params.Value.Elements
                    .Select(punct => punct.Value)
                    .Select(param => $"dynamic {this.DefinedSymbol(param)}"))
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
            .AppendLine($"{this.DefinedSymbol(node)}:;");
        return this.Default;
    }

    public override string VisitVariableDecl(Decl.Variable decl)
    {
        var varReg = this.DefinedSymbol(decl);
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
        var label = this.ReferencedSymbol(node.Target);
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

    public override string VisitIndexExpr(Expr.Index node)
    {
        var indexed = this.VisitExpr(node.Called);
        var args = node.Args.Value.Elements.Select(a => this.VisitExpr(a.Value)).ToList();
        return $"{indexed}[{string.Join(", ", args)}];";
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
                var text = c.Value.ValueText!.Substring(c.Cutoff); //Infer# says ValueText could be null.
                this
                    .Indent2()
                    .AppendLine($"{result}.Append(\"{StringUtils.Unescape(text)}\");");
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
