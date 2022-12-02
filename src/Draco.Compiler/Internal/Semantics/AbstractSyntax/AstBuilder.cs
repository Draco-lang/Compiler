using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Semantics.Types;
using System.Collections.Immutable;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Computations for building the AST.
/// </summary>
internal static class AstBuilder
{
    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="ast">The <see cref="ParseTree"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="ast"/>.</returns>
    public static Ast ToAst(QueryDatabase db, ParseTree ast) => ast switch
    {
        ParseTree.CompilationUnit cu => ToAst(db, cu),
        ParseTree.Decl decl => ToAst(db, decl),
        ParseTree.Stmt stmt => ToAst(db, stmt),
        ParseTree.Expr expr => ToAst(db, expr),
        _ => throw new ArgumentOutOfRangeException(nameof(ast)),
    };

    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="decl">The <see cref="ParseTree"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="decl"/>.</returns>
    public static Ast.Decl ToAst(QueryDatabase db, ParseTree.Decl decl) => db.GetOrUpdate(
        decl,
        Ast.Decl (decl) => decl switch
        {
            // TODO: Eliminate the ?? pattern everywhere by making the API use optional
            // TODO: Eliminate the null ? null : ... pattern everywhere by making the API use optional

            ParseTree.Decl.Func func => new Ast.Decl.Func(
                ParseTree: func,
                DeclarationSymbol: (Symbol.Function?)SymbolResolution.GetDefinedSymbolOrNull(db, func) ?? throw new InvalidOperationException(),
                Body: ToAst(db, func.Body)),
            ParseTree.Decl.Label label => new Ast.Decl.Label(
                ParseTree: label,
                LabelSymbol: SymbolResolution.GetDefinedSymbolOrNull(db, label) ?? throw new InvalidOperationException()),
            ParseTree.Decl.Variable var => new Ast.Decl.Variable(
                ParseTree: var,
                DeclarationSymbol: (Symbol.IVariable?)SymbolResolution.GetDefinedSymbolOrNull(db, var) ?? throw new InvalidOperationException(),
                Value: var.Initializer is null ? null : ToAst(db, var.Initializer.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(decl)),
        });

    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="stmt">The <see cref="ParseTree"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="stmt"/>.</returns>
    public static Ast.Stmt ToAst(QueryDatabase db, ParseTree.Stmt stmt) => db.GetOrUpdate(
        stmt,
        Ast.Stmt (stmt) => stmt switch
        {
            ParseTree.Stmt.Decl d => new Ast.Stmt.Decl(
                ParseTree: d,
                Declaration: ToAst(db, d.Declaration)),
            ParseTree.Stmt.Expr expr => new Ast.Stmt.Expr(
                ParseTree: expr,
                Expression: ToAst(db, expr.Expression)),
            _ => throw new ArgumentOutOfRangeException(nameof(stmt)),
        });

    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ParseTree"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="expr"/>.</returns>
    public static Ast.Expr ToAst(QueryDatabase db, ParseTree.Expr expr) => db.GetOrUpdate(
        expr,
        Ast.Expr (expr) => expr switch
        {
            ParseTree.Expr.Name name => new Ast.Expr.Reference(
                ParseTree: name,
                Symbol: SymbolResolution.GetReferencedSymbol(db, name)),
            ParseTree.Expr.If @if => new Ast.Expr.If(
                ParseTree: @if,
                Condition: ToAst(db, @if.Condition.Value),
                Then: ToAst(db, @if.Then),
                Else: @if.Else is null ? Ast.Expr.Unit.Default : ToAst(db, @if.Else.Expression)),
            ParseTree.Expr.While @while => new Ast.Expr.While(
                ParseTree: @while,
                Condition: ToAst(db, @while.Condition.Value),
                Expression: ToAst(db, @while.Expression)),
            ParseTree.Expr.Block block => new Ast.Expr.Block(
                ParseTree: block,
                Statements: block.Enclosed.Value.Statements.Select(s => ToAst(db, s)).ToImmutableArray(),
                Value: block.Enclosed.Value.Value is null ? Ast.Expr.Unit.Default : ToAst(db, block.Enclosed.Value.Value)),
            ParseTree.Expr.Call call => new Ast.Expr.Call(
                ParseTree: call,
                Called: ToAst(db, call.Called),
                Args: call.Args.Value.Elements.Select(a => ToAst(db, a.Value)).ToImmutableArray()),
            ParseTree.Expr.Relational rel => new Ast.Expr.Relational(
                ParseTree: rel,
                Left: ToAst(db, rel.Left),
                Comparisons: rel.Comparisons.Select(c => ToAst(db, c)).ToImmutableArray()),
            ParseTree.Expr.Unary ury => new Ast.Expr.Unary(
                ParseTree: ury,
                Operator: SymbolResolution.GetReferencedSymbolOrNull(db, ury) ?? throw new InvalidOperationException(),
                Operand: ToAst(db, ury.Operand)),
            ParseTree.Expr.Binary bin => new Ast.Expr.Binary(
                ParseTree: bin,
                Left: ToAst(db, bin.Left),
                Operator: (Symbol.IOperator?)SymbolResolution.GetReferencedSymbolOrNull(db, bin) ?? throw new InvalidOperationException(),
                Right: ToAst(db, bin.Right)),
            ParseTree.Expr.Literal lit => ToAst(lit),
            ParseTree.Expr.String str => new Ast.Expr.String(
                ParseTree: str,
                Parts: str.Parts.Select(p => ToAst(db, p)).ToImmutableArray()),
            // We desugar unit statements into { stmt; }
            ParseTree.Expr.UnitStmt stmt => new Ast.Expr.Block(
                ParseTree: stmt,
                Statements: ImmutableArray.Create(ToAst(db, stmt.Statement)),
                Value: Ast.Expr.Unit.Default),
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        });

    private static Ast.CompilationUnit ToAst(QueryDatabase db, ParseTree.CompilationUnit cu) => db.GetOrUpdate(
        cu,
        Ast.CompilationUnit (cu) => new(
            ParseTree: cu,
            Declarations: cu.Declarations.Select(d => ToAst(db, d)).ToImmutableArray()));

    private static Ast.Expr.Block ToAst(QueryDatabase db, ParseTree.FuncBody funcBody) => db.GetOrUpdate(
        funcBody,
        Ast.Expr.Block (funcBody) => funcBody switch
        {
            ParseTree.FuncBody.BlockBody blockBody => (Ast.Expr.Block)ToAst(db, blockBody.Block),
            // Desugar here into a simple return statement inside a block
            ParseTree.FuncBody.InlineBody inlineBody => new(
                ParseTree: inlineBody,
                Statements: ImmutableArray.Create<Ast.Stmt>(
                    new Ast.Stmt.Expr(
                        ParseTree: inlineBody,
                        Expression: new Ast.Expr.Return(
                            ParseTree: inlineBody,
                            Expression: ToAst(db, inlineBody.Expression)))),
                Value: Ast.Expr.Unit.Default),
            _ => throw new ArgumentOutOfRangeException(nameof(funcBody)),
        });

    private static Ast.ComparisonElement ToAst(QueryDatabase db, ParseTree.ComparisonElement ce) => db.GetOrUpdate(
        ce,
        Ast.ComparisonElement (ce) => new(
            ParseTree: ce,
            Operator: (Symbol.IOperator?)SymbolResolution.GetReferencedSymbolOrNull(db, ce) ?? throw new InvalidOperationException(),
            Right: ToAst(db, ce.Right)));

    private static Ast.StringPart ToAst(QueryDatabase db, ParseTree.StringPart part) => db.GetOrUpdate(
        part,
        Ast.StringPart (part) => part switch
        {
            ParseTree.StringPart.Content content => new Ast.StringPart.Content(
                ParseTree: content,
                Value: content.Value.ValueText ?? throw new InvalidOperationException()),
            ParseTree.StringPart.Interpolation interpolation => new Ast.StringPart.Interpolation(
                ParseTree: interpolation,
                Expression: ToAst(db, interpolation.Expression)),
            _ => throw new ArgumentOutOfRangeException(nameof(part)),
        });

    private static Ast.Expr ToAst(ParseTree.Expr.Literal lit) => lit.Value.Type switch
    {
        TokenType.LiteralInteger => new Ast.Expr.Literal(
            ParseTree: lit,
            Value: lit.Value.Value,
            Type: Type.Int32),
        _ => throw new NotImplementedException(),
    };
}
