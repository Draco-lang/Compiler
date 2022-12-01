using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Semantics.Symbols;
using static Draco.Compiler.Internal.Semantics.AbstractSyntax.AstFactory;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Implements lowering (desugaring) to the <see cref="Ast"/> to simplify codegen.
/// </summary>
internal sealed class AstLowering : AstTransformerBase
{
    /// <summary>
    /// Lowers the <see cref="Ast"/> into simpler elements.
    /// </summary>
    /// <param name="ast">The <see cref="Ast"/> to lower.</param>
    /// <returns>The lowered equivalent of <paramref name="ast"/>.</returns>
    public static Ast Lower(Ast ast) =>
        new AstLowering().Transform(ast, out _);

    private AstLowering()
    {
    }

    public override Ast.Expr TransformWhileExpr(Ast.Expr.While node, out bool changed)
    {
        // while (condition)
        // {
        //     body...
        // }
        //
        // =>
        //
        // continue_label:
        //     if (!condition) goto break_label;
        //     body...
        //     goto continue_label;
        // break_label:

        changed = true;

        var continueLabel = new Symbol.SynthetizedLabel();
        var breakLabel = new Symbol.SynthetizedLabel();
        var condition = this.TransformExpr(node.Condition, out _);
        var body = this.TransformExpr(node.Expression, out _);

        return Block(
            // continue_label:
            Stmt(Label(continueLabel)),
            // if (!condition) goto break_label;
            Stmt(If(
                condition: Unary(
                    op: null!, // TODO: Unary boolean negation
                    subexpr: condition),
                then: Goto(breakLabel))),
            // body...
            Stmt(body),
            // goto continue_label;
            Stmt(Goto(continueLabel)),
            // break_label:
            Stmt(Label(breakLabel)));
    }

    public override Ast.Expr TransformRelationalExpr(Ast.Expr.Relational node, out bool changed)
    {
        // expr1 < expr2 == expr3 > expr4 != ...
        //
        // =>
        //
        // {
        //     val tmp1 = expr1;
        //     val tmp2 = expr2;
        //     val tmp3 = expr3;
        //     val tmp4 = expr4;
        //     ...
        //     tmp1 < tmp2 && tmp2 == tmp3 && tmp3 > tmp4 && tmp4 != ...
        // }

        // Utility to store an expression as a temporary
        (Symbol Symbol, Ast.Decl Assignment) StoreTemporary(Ast.Expr expr)
        {
            // TODO: Get type of synthetized var
            var symbol = new Symbol.SynthetizedVariable(false, null!);
            var assignment = Var(
                varSymbol: symbol,
                value: this.TransformExpr(expr, out _));
            return (symbol, assignment);
        }

        changed = true;

        // Store all expressions as temporary variables
        var tmpVariables = new List<(Symbol Symbol, Ast.Decl Assignment)>();
        tmpVariables.Add(StoreTemporary(node.Left));
        foreach (var item in node.Comparisons) tmpVariables.Add(StoreTemporary(item.Right));

        // Build pairs of comparisons from symbol references
        var comparisons = new List<Ast.Expr>();
        for (var i = 0; i < node.Comparisons.Length; ++i)
        {
            var left = tmpVariables[i].Symbol;
            var op = node.Comparisons[i].Operator;
            var right = tmpVariables[i + 1].Symbol;
            comparisons.Add(Binary(
                left: Reference(left),
                op: op,
                right: Reference(right)));
        }

        // Fold them into conjunctions
        var conjunction = comparisons
            .Aggregate((x, y) => Binary(
                left: x,
                op: null!, // TODO: && operator
                right: y));

        // Wrap up in block
        return Block(
            stmts: tmpVariables.Select(v => Stmt(v.Assignment)),
            value: conjunction);
    }
}
