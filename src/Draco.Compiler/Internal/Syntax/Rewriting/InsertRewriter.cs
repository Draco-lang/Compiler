using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class InsertRewriter(
    SyntaxNode toInsert, SyntaxNode insertInto, int position) : SyntaxRewriter
{
    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
    {
        if (insertInto == node && toInsert is DeclarationSyntax decl)
        {
            return new CompilationUnitSyntax(this.InsertIntoSyntaxList(node.Declarations, decl), node.End);
        }
        return base.VisitCompilationUnit(node);
    }

    public override SyntaxNode VisitBlockFunctionBody(BlockFunctionBodySyntax node)
    {
        if (insertInto == node && toInsert is StatementSyntax stmt)
        {
            return new BlockFunctionBodySyntax(node.OpenBrace, this.InsertIntoSyntaxList(node.Statements, stmt), node.CloseBrace);
        }
        return base.VisitBlockFunctionBody(node);
    }

    public override SyntaxNode VisitBlockExpression(BlockExpressionSyntax node)
    {
        if (insertInto == node && toInsert is StatementSyntax stmt)
        {
            return new BlockExpressionSyntax(node.OpenBrace, this.InsertIntoSyntaxList(node.Statements, stmt), node.Value, node.CloseBrace);
        }
        return base.VisitBlockExpression(node);
    }

    private SyntaxList<T> InsertIntoSyntaxList<T>(SyntaxList<T> original, T insertion)
        where T : SyntaxNode
    {
        var list = original.ToList();
        list.Insert(position, insertion);
        var builder = SyntaxList.CreateBuilder<T>();
        builder.AddRange(list);
        return builder.ToSyntaxList();
    }

    public override SyntaxNode VisitSyntaxToken(SyntaxToken node) => node;
}
