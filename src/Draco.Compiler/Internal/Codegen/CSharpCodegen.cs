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
        this.builderStack.Push(newBuilder);
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
            // For now we just reserve for globals only
            var canKeepOriginalName = symbol.IsGlobal;
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

    private void WriteInstruction(string instr) => this.Indent2().Append(instr);
    private void DefineLabel(string labelName) => this.Indent1().Append($"{labelName}:;");
    private string? DeclareRegister(Type type)
    {
        if (IsUnit(type)) return null;

        var name = this.AllocateRegister();
        this.Indent2().AppendLine($"{TranslateType(type)} {name};");
        return name;
    }
    private string? DefineRegister(Type type, string expr)
    {
        if (IsUnit(type))
        {
            this.Indent2().AppendLine($"{expr};");
            return null;
        }

        var name = this.AllocateRegister();
        this.Indent2().AppendLine($"{TranslateType(type)} {name} = {expr};");
        return name;
    }

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

    public override string? VisitBlockExpr(Ast.Expr.Block node)
    {
        foreach (var stmt in node.Statements) this.VisitStmt(stmt);
        return this.VisitExpr(node.Value);
    }

    public override string? VisitIfExpr(Ast.Expr.If node)
    {
        // Allocate result storage
        var result = this.DeclareRegister(node.EvaluationType);

        // Allocate else and end labels
        var elseLabel = this.AllocateLabel();
        var endLabel = this.AllocateLabel();

        // Compile condition
        var condition = this.Visit(node.Condition);

        // If false, jump to else
        this.WriteInstruction($"if (!({condition})) goto {elseLabel};");
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
}
