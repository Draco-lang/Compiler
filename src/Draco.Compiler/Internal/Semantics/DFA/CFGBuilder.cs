using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Semantics.DFA;

internal sealed class CFGBuilder
{
    private CFG<Ast.Stmt, Ast.Expr>.Builder? cfg;
    public CFG<Ast.Stmt, Ast.Expr>.Block BuildCFG(Ast node)
    {
        if (node is Ast.Expr.Block)
        {
            this.cfg = new CFG<Ast.Stmt, Ast.Expr>.Builder(new CFG<Ast.Stmt, Ast.Expr>.Block.Builder());
            this.ToCFG((Ast.Expr.Block)node);
            return this.cfg!.Build();
        }
        throw new NotImplementedException();
    }

    private void ToCFG(Ast.Expr.Block block)
    {
        List<Ast.Stmt> statements = new();
        foreach (var stmt in block.Statements)
        {
            if (stmt is Ast.Stmt.Expr expr) switch (expr.Expression)
                {
                case Ast.Expr.If ifExpr: this.ToCFG(ifExpr); break;
                case Ast.Expr.While: throw new NotImplementedException();
                case Ast.Expr.Goto: throw new NotImplementedException();
                case Ast.Expr.Return: throw new NotImplementedException();
                }
            else if (stmt is Ast.Stmt.Decl decl && decl.Declaration is Ast.Decl.Label) throw new NotImplementedException();
            this.cfg!.PushStatement(stmt);
        }
    }

    private void ToCFG(Ast.Expr expr) => throw new NotImplementedException();

    private void ToCFG(Ast.Expr.If expr)
    {
        this.cfg!.PushBranch(new CFG<Ast.Stmt, Ast.Expr>.Branch.Builder(expr.Condition));
        this.cfg.PushBlock(new CFG<Ast.Stmt, Ast.Expr>.Block.Builder());
        this.ToCFG(expr.Then);
        this.cfg.PopBranch();
        if (expr.Else != Ast.Expr.Unit.Default)
        {
            throw new NotImplementedException();
        }
    }
}
