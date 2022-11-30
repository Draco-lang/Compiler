using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Semantics.Symbols;

namespace Draco.Compiler.Internal.Semantics;

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

    public override Ast.Expr.Relational TransformRelationalExpr(Ast.Expr.Relational node, out bool changed)
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

        var tmpVariables = ImmutableArray.CreateBuilder<(Symbol Symbol, Ast.Decl Assignment)>();

        void StoreTemporary(Ast.Expr expr)
        {
            expr = this.TransformExpr(expr, out _);
            var symbol = new Symbol.SynthetizedVariable();
            var assignment = new Ast.Decl.Variable(
                ParseTree: null,
                DeclarationSymbol: symbol,
                Type: null!, // TODO
                Value: expr);
            tmpVariables.Add((symbol, assignment));
        }

        // Store temporary variables
        StoreTemporary(node.Left);
        foreach (var item in node.Comparisons) StoreTemporary(item.Right);

        Debug.Assert(tmpVariables.Count >= 2);

        // TODO
        throw new NotImplementedException();
    }
}
