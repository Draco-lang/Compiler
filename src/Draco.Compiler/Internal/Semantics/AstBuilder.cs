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

namespace Draco.Compiler.Internal.Semantics;

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
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolOrNull(db, func) ?? throw new InvalidOperationException(),
                Params: func.Params.Value.Elements.Select(p =>
                    SymbolResolution.GetDefinedSymbolOrNull(db, p.Value) ?? throw new InvalidOperationException()).ToImmutableArray(),
                ReturnType: func.ReturnType is null ? Type.Unit : TypeChecker.Evaluate(db, func.ReturnType.Type),
                Body: ToAst(db, func.Body)),
            ParseTree.Decl.Label label => new Ast.Decl.Label(
                ParseTree: label,
                LabelSymbol: SymbolResolution.GetDefinedSymbolOrNull(db, label) ?? throw new InvalidOperationException()),
            ParseTree.Decl.Variable var => new Ast.Decl.Variable(
                ParseTree: var,
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolOrNull(db, var) ?? throw new InvalidOperationException(),
                Type: var.Type is null ? Type.Unit : TypeChecker.Evaluate(db, var.Type.Type),
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
}
