using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Semantics.DFA;

internal sealed class CFGBuilder
{
    public static CFG.Block ToCFG(Ast node)
    {
        if (node is Ast.Expr.Block) return ToCFG((Ast.Expr.Block)node);
        throw new NotImplementedException();
    }

    public static CFG.Block ToCFG(Ast.Expr.Block block)
    {
        List<Ast.Stmt> statements = new();
        foreach (var stmt in block.Statements)
        {
            if (stmt is Ast.Stmt.Expr expr) switch (expr.Expression)
                {
                case Ast.Expr.If: throw new NotImplementedException();
                case Ast.Expr.While: throw new NotImplementedException();
                case Ast.Expr.Goto: throw new NotImplementedException();
                case Ast.Expr.Return: throw new NotImplementedException();
                }
            else if (stmt is Ast.Stmt.Decl decl && decl.Declaration is Ast.Decl.Label) throw new NotImplementedException();
            statements.Add(stmt);
        }
        return new CFG.Block(statements, new List<CFG.Branch>());
    }
}
