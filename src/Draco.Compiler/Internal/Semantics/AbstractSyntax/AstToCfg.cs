using System;
using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Internal.Semantics.FlowAnalysis;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Conversion utility to turn an <see cref="Ast"/> into a <see cref="IControlFlowGraph{TStatement}"/>.
/// </summary>
internal sealed class AstToCfg : AstVisitorBase<Unit>
{
    public static IControlFlowGraph<Ast> ToCfg(Ast ast)
    {
        var translator = new AstToCfg();
        translator.Visit(ast);
        translator.builder.Exit();
        return translator.builder.Build();
    }

    private readonly CfgBuilder<Ast> builder = new();
    private readonly Dictionary<ISymbol.ILabel, CfgBuilder<Ast>.Label> labels = new();

    private AstToCfg()
    {
    }

    private CfgBuilder<Ast>.Label GetLabel(ISymbol.ILabel symbol)
    {
        if (!this.labels.TryGetValue(symbol, out var label))
        {
            label = this.builder.DeclareLabel();
            this.labels.Add(symbol, label);
        }
        return label;
    }

    public override Unit VisitLabelDecl(Ast.Decl.Label node)
    {
        this.builder.PlaceLabel(this.GetLabel(node.LabelSymbol));
        return this.Default;
    }

    public override Unit VisitExprStmt(Ast.Stmt.Expr node)
    {
        base.VisitExprStmt(node);
        if (!IsCompoundExpr(node.Expression)) this.builder.AddStatement(node);
        return this.Default;
    }

    public override Unit VisitNoOpStmt(Ast.Stmt.NoOp node)
    {
        this.builder.AddStatement(node);
        return this.Default;
    }

    public override Unit VisitGotoExpr(Ast.Expr.Goto node)
    {
        var label = this.GetLabel(node.Target);
        this.builder.Connect(label);
        this.builder.Disjoin();
        return this.Default;
    }

    public override Unit VisitIfExpr(Ast.Expr.If node)
    {
        this.VisitExpr(node.Condition);
        var start = this.builder.CurrentLabel;
        var thenLabel = this.builder.DeclareLabel();
        var elseLabel = this.builder.DeclareLabel();
        var endLabel = this.builder.DeclareLabel();

        this.builder.Connect(elseLabel);

        this.builder.PlaceLabel(thenLabel);
        this.VisitExpr(node.Then);
        this.builder.Connect(endLabel);

        this.builder.Jump(elseLabel);
        this.VisitExpr(node.Else);

        this.builder.PlaceLabel(endLabel);

        return this.Default;
    }

    private static bool IsCompoundExpr(Ast.Expr expr) => expr
        is Ast.Expr.Block
        or Ast.Expr.If;
}
