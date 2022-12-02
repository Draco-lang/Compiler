using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.ParseTree;
using System.IO;
using Draco.Compiler.Internal.Semantics.Symbols;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Types;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Codegen;

// NOTE: Currently this is only here to have something hacky but runnable for the compiler
// Eventually we'll translate to our own IR and then compile that to IL
/// <summary>
/// Generates low-level C# code from the Draco <see cref="ParseTree"/>.
/// </summary>
internal sealed class CSharpCodegen : AstVisitorBase<string?>
{
    // Final output
    private readonly StreamWriter output;

    // Code builders
    private StringBuilder Builder => this.builderStack.Peek();
    private readonly StringBuilder topLevelBuilder = new();
    private readonly Stack<StringBuilder> builderStack = new();

    // Name allocation state
    private readonly Dictionary<Symbol, string> symbolNames = new();
    private int registerCount = 0;
    private int labelCount = 0;

    public CSharpCodegen(Stream output)
    {
        this.output = new(output);
        this.builderStack.Push(this.topLevelBuilder);
    }

    public void Generate(Ast root)
    {
        this.Visit(root);
        this.output.Write(this.topLevelBuilder);
        this.output.Flush();
    }

    // Builder stack

    private void PushBuilder(StringBuilder builder) => this.builderStack.Push(builder);
    private StringBuilder PushNewBuilder()
    {
        var newBuilder = new StringBuilder();
        this.PushBuilder(newBuilder);
        return newBuilder;
    }
    private StringBuilder PopBuilder() => this.builderStack.Pop();

    // Name allocation

    private string AllocateRegister() => $"reg_{this.registerCount++}";
    private string AllocateLabel() => $"label_{this.labelCount++}";
    private string AllocateName(Symbol symbol)
    {
        if (!this.symbolNames.TryGetValue(symbol, out var name))
        {
            // Check if we need to generate a name for it
            var canKeepOriginalName = symbol.IsExternallyVisible;
            name = canKeepOriginalName ? symbol.Name : $"sym_{this.symbolNames.Count}";
            this.symbolNames.Add(symbol, name);
        }
        return name;
    }

    // Types

    private static string TranslateBuiltinType(System.Type type)
    {
        if (type == typeof(void)) return "void";
        return type.FullName ?? type.Name;
    }

    private static string TranslateType(Type type) => type switch
    {
        Type.Builtin builtin => TranslateBuiltinType(builtin.Type),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static bool IsUnit(Type type) => type.Equals(Type.Unit);

    // Code writer utils

    private StringBuilder Indent1() => this.Builder.Append("    ");
    private StringBuilder Indent2() => this.Builder.Append("        ");

    private void WriteInstruction(string instr) => this.Indent2().AppendLine(instr);
    private void DefineLabel(string labelName) => this.Indent1().AppendLine($"{labelName}:;");
    private string? DefineRegister(Type type, string? name = null, string? expr = null)
    {
        if (IsUnit(type))
        {
            if (expr is not null) this.Indent2().AppendLine($"{expr};");
            return null;
        }

        name ??= this.AllocateRegister();
        this.Indent2().Append($"{TranslateType(type)} {name}");
        if (expr is not null) this.Builder.Append($" = {expr}");
        this.Builder.AppendLine(";");
        return name;
    }

    // Illegal visitors

    public override string VisitRelationalExpr(Ast.Expr.Relational node) => throw new InvalidOperationException();
    public override string VisitWhileExpr(Ast.Expr.While node) => throw new InvalidOperationException();

    // Visitors

    public override string? VisitFuncDecl(Ast.Decl.Func node)
    {
        // We write to a local builder
        var builder = this.PushNewBuilder();

        var symbol = node.DeclarationSymbol;
        var returnType = TranslateType(node.ReturnType);
        var funcName = this.AllocateName(symbol);

        // Function header
        this.Indent1()
            .Append($"internal static {returnType} {funcName}(")
            .AppendJoin(", ", node.Params.Select(p => $"{TranslateType(p.Type)} {this.AllocateName(p)}"))
            .AppendLine(")");
        this.Indent1().AppendLine("{");

        // Codegen body
        this.VisitBlockExpr(node.Body);

        this.Indent1().AppendLine("}");

        this.PopBuilder();

        // Write it to top-level
        this.topLevelBuilder.Append(builder);

        return this.Default;
    }

    public override string? VisitVariableDecl(Ast.Decl.Variable node)
    {
        var name = this.AllocateName((Symbol)node.DeclarationSymbol);
        var value = node.Value is null ? null : this.VisitExpr(node.Value);
        this.DefineRegister(node.DeclarationSymbol.Type, name, value);
        return this.Default;
    }

    public override string? VisitLabelDecl(Ast.Decl.Label node)
    {
        this.DefineLabel(this.AllocateName(node.LabelSymbol));
        return this.Default;
    }

    public override string? VisitBlockExpr(Ast.Expr.Block node)
    {
        foreach (var stmt in node.Statements) this.VisitStmt(stmt);
        return this.VisitExpr(node.Value);
    }

    public override string? VisitReturnExpr(Ast.Expr.Return node)
    {
        var result = this.VisitExpr(node.Expression);
        this.WriteInstruction($"return {result};");
        return this.Default;
    }

    public override string? VisitGotoExpr(Ast.Expr.Goto node)
    {
        var label = this.AllocateName(node.Target);
        this.WriteInstruction($"goto {label};");
        return this.Default;
    }

    public override string? VisitCallExpr(Ast.Expr.Call node)
    {
        var func = this.VisitExpr(node.Called);
        var args = node.Args.Select(this.VisitExpr);
        var expr = $"{func}({string.Join(", ", args)})";
        var result = this.DefineRegister(node.EvaluationType, expr: expr);
        return result;
    }

    public override string? VisitIndexExpr(Ast.Expr.Index node)
    {
        // TODO
        throw new NotImplementedException();
    }

    public override string? VisitMemberAccessExpr(Ast.Expr.MemberAccess node)
    {
        // TODO
        throw new NotImplementedException();
    }

    public override string? VisitIfExpr(Ast.Expr.If node)
    {
        // Allocate result storage
        var result = this.DefineRegister(node.EvaluationType);

        // Allocate else and end labels
        var elseLabel = this.AllocateLabel();
        var endLabel = this.AllocateLabel();

        // Compile condition
        var condition = this.Visit(node.Condition);

        // If false, jump to else
        this.WriteInstruction($"if (!{condition}) goto {elseLabel};");
        // Otherwise just compile then and store result, then jump to the end
        var thenResult = this.VisitExpr(node.Then);
        if (thenResult is not null) this.WriteInstruction($"{result} = {thenResult};");
        this.WriteInstruction($"goto {endLabel};");

        // Else branch
        this.DefineLabel(elseLabel);
        // Similarly to then, compile, store result
        // No need to jump to the end here, we are already there
        var elseResult = this.VisitExpr(node.Else);
        if (elseResult is not null) this.WriteInstruction($"{result} = {elseResult};");

        // End label
        this.DefineLabel(endLabel);

        return result;
    }

    public override string? VisitUnaryExpr(Ast.Expr.Unary node)
    {
        var subexpr = this.VisitExpr(node.Operand);
        var expr = MapUnaryOperator(node.Operator, subexpr);
        var result = this.DefineRegister(node.EvaluationType, expr: expr);
        return result;
    }

    public override string? VisitBinaryExpr(Ast.Expr.Binary node)
    {
        var left = this.VisitExpr(node.Left);
        var right = this.VisitExpr(node.Right);
        var expr = MapBinaryOperator(node.Operator, left, right);
        var result = this.DefineRegister(node.EvaluationType, expr: expr);
        return result;
    }

    public override string? VisitAssignExpr(Ast.Expr.Assign node)
    {
        var left = this.VisitExpr(node.Target);
        var right = this.VisitExpr(node.Value);
        var expr = MapAssignmentOperator(node.CompoundOperator, left, right);
        this.WriteInstruction($"{expr};");
        return left;
    }

    public override string VisitStringExpr(Ast.Expr.String node)
    {
        var builder = this.AllocateRegister();
        this.WriteInstruction($"System.Text.StringBuilder {builder} = new System.Text.StringBuilder();");
        foreach (var part in node.Parts)
        {
            if (part is Ast.StringPart.Interpolation i)
            {
                var subexpr = this.VisitExpr(i.Expression);
                this.WriteInstruction($"{builder}.Append({subexpr}.ToString());");
            }
            else
            {
                var c = (Ast.StringPart.Content)part;
                var text = c.Value;
                text = text.Substring(c.Cutoff);
                this.WriteInstruction($"{builder}.Append(\"{StringUtils.Unescape(text)}\");");
            }
        }
        var result = this.DefineRegister(Type.String, expr: $"{builder}.ToString()");
        return result!;
    }

    public override string? VisitLiteralExpr(Ast.Expr.Literal node) =>
        node.Value?.ToString()?.ToLower();

    public override string VisitReferenceExpr(Ast.Expr.Reference node) =>
        this.AllocateName(node.Symbol);

    private static string? MapUnaryOperator(Symbol.IOperator op, string? sub)
    {
        if (op is not Symbol.IntrinsicOperator intrinsicOp) throw new NotImplementedException();
        if (sub is null) return null;
        return intrinsicOp.Operator switch
        {
            TokenType.KeywordNot => $"!{sub}",

            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }

    private static string? MapBinaryOperator(Symbol.IOperator op, string? left, string? right)
    {
        if (op is not Symbol.IntrinsicOperator intrinsicOp) throw new NotImplementedException();
        if (left is null || right is null) return null;
        return intrinsicOp.Operator switch
        {
            // NOTE: TokenTypes shouldn't leak in like this, see note in IntrinsicOperator

            TokenType.Plus => $"{left} + {right}",
            TokenType.Minus => $"{left} - {right}",
            TokenType.Star => $"{left} * {right}",
            TokenType.Slash => $"{left} / {right}",
            TokenType.KeywordRem => $"{left} % {right}",
            TokenType.KeywordMod => $"({left} % {right} + {right}) % {right}",

            TokenType.LessThan => $"{left} < {right}",
            TokenType.GreaterThan => $"{left} > {right}",
            TokenType.LessEqual => $"{left} <= {right}",
            TokenType.GreaterEqual => $"{left} >= {right}",
            TokenType.Equal => $"{left} == {right}",
            TokenType.NotEqual => $"{left} != {right}",

            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }

    private static string? MapAssignmentOperator(Symbol.IOperator? op, string? left, string? right)
    {
        if (op is null) return $"{left} = {right}";
        if (op is not Symbol.IntrinsicOperator intrinsicOp) throw new NotImplementedException();
        if (left is null || right is null) return null;
        return intrinsicOp.Operator switch
        {
            // NOTE: TokenTypes shouldn't leak in like this, see note in IntrinsicOperator

            TokenType.Plus => $"{left} += {right}",
            TokenType.Minus => $"{left} -= {right}",
            TokenType.Star => $"{left} *= {right}",
            TokenType.Slash => $"{left} /= {right}",
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }
}
