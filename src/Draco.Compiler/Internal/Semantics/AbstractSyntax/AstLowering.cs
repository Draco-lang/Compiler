using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Semantics.Types;
using static Draco.Compiler.Internal.Semantics.AbstractSyntax.AstFactory;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Implements lowering (desugaring) to the <see cref="Ast"/> to simplify codegen.
/// </summary>
internal sealed class AstLowering : AstTransformerBase
{
    /// <summary>
    /// Lowers the <see cref="Ast"/> into simpler elements.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="ast">The <see cref="Ast"/> to lower.</param>
    /// <returns>The lowered equivalent of <paramref name="ast"/>.</returns>
    public static Ast Lower(QueryDatabase db, Ast ast) =>
        new AstLowering(db).Transform(ast, out _);

    private readonly QueryDatabase db;

    private AstLowering(QueryDatabase db)
    {
        this.db = db;
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

        var continueLabel = ISymbol.SynthetizeLabel();
        var breakLabel = ISymbol.SynthetizeLabel();
        var condition = this.TransformExpr(node.Condition, out _);
        var body = this.TransformExpr(node.Expression, out _);

        return Block(
            // continue_label:
            Stmt(Label(continueLabel)),
            // if (!condition) goto break_label;
            If(
                condition: Not(condition),
                then: Goto(breakLabel)),
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

        changed = true;

        // Store all expressions as temporary variables
        var tmpVariables = new List<(Ast.Expr Reference, Ast.Stmt Assignment)>();
        tmpVariables.Add(this.StoreTemporary(node.Left));
        foreach (var item in node.Comparisons) tmpVariables.Add(this.StoreTemporary(item.Right));

        // Build pairs of comparisons from symbol references
        var comparisons = new List<Ast.Expr>();
        for (var i = 0; i < node.Comparisons.Length; ++i)
        {
            var left = tmpVariables[i].Reference;
            var op = node.Comparisons[i].Operator;
            var right = tmpVariables[i + 1].Reference;
            comparisons.Add(Binary(
                left: left,
                op: op,
                right: right));
        }

        // Fold them into conjunctions
        var conjunction = comparisons.Aggregate(And);
        // Desugar them, conjunctions can be desugared too
        conjunction = this.TransformExpr(conjunction, out _);

        // Wrap up in block
        return Block(
            stmts: tmpVariables.Select(t => t.Assignment),
            value: conjunction);
    }

    public override Ast.Expr TransformAndExpr(Ast.Expr.And node, out bool changed)
    {
        // expr1 and expr2
        //
        // =>
        //
        // {
        //     var result = false;
        //     if (expr1) result = expr2;
        //     result
        // }

        changed = true;

        var left = this.TransformExpr(node.Left, out _);
        var right = this.TransformExpr(node.Right, out _);

        var varSymbol = ISymbol.SynthetizeVariable(type: Type.Bool, isMutable: true);
        return Block(
            stmts: new[]
            {
                // var result = false;
                Stmt(Var(
                    varSymbol: varSymbol,
                    value: Bool(false))),
                // if (expr1) result = expr2;
                If(
                    condition: left,
                    then: Assign(Reference(varSymbol), right)),
            },
            value: Reference(varSymbol));
    }

    public override Ast.Expr TransformOrExpr(Ast.Expr.Or node, out bool changed)
    {
        // expr1 or expr2
        //
        // =>
        //
        // {
        //     var result = true;
        //     if (!expr1) result = expr2;
        //     result
        // }

        changed = true;

        var left = this.TransformExpr(node.Left, out _);
        var right = this.TransformExpr(node.Right, out _);

        var varSymbol = ISymbol.SynthetizeVariable(type: Type.Bool, isMutable: true);
        return Block(
            stmts: new[]
            {
                // var result = false;
                Stmt(Var(
                    varSymbol: varSymbol,
                    value: Bool(true))),
                // if (!expr1) result = expr2;
                If(
                    condition: Not(left),
                    then: Assign(Reference(varSymbol), right)),
            },
            value: Reference(varSymbol));
    }

    public override Ast.Expr TransformStringExpr(Ast.Expr.String node, out bool changed)
    {
        if (node.Parts.All(p => p is Ast.StringPart.Content))
        {
            // It's a single, or multi-line string without interpolation, we make a literal out of it
            changed = true;
            var literal = string.Join(string.Empty, node.Parts.Cast<Ast.StringPart.Content>().Select(p => p.Value));
            return new Ast.Expr.Literal(
                ParseTree: node.ParseTree,
                Value: literal,
                Type: Type.String);
        }
        else
        {
            // TODO: Desugar into interpolation handler
            changed = false;
            return node;
        }
    }

    // Utility to store an expression to a temporary variable
    private (Ast.Expr Reference, Ast.Stmt Assignment) StoreTemporary(Ast.Expr expr)
    {
        // Optimization: if it's already a symbol reference, leave as-is
        // Optimization: if it's a literal, don't bother copying
        if (expr is Ast.Expr.Reference or Ast.Expr.Literal)
        {
            return (expr, Ast.Stmt.NoOp.Default);
        }

        // Otherwise compute and store
        Debug.Assert(expr.ParseTree is ParseTree.Expr);
        var type = TypeChecker.TypeOf(this.db, (ParseTree.Expr)expr.ParseTree);
        var symbol = ISymbol.SynthetizeVariable(type: type, isMutable: false);
        var symbolRef = Reference(symbol);
        var assignment = Stmt(Var(
            varSymbol: symbol,
            value: this.TransformExpr(expr, out _)));
        return (symbolRef, assignment);
    }
}
