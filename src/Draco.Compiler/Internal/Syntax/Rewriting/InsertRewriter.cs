using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class InsertRewriter : SyntaxRewriter
{
    private readonly SyntaxNode toInsert;
    private readonly SyntaxNode insertInto;
    private readonly int position;

    public InsertRewriter(SyntaxNode toInsert, SyntaxNode insertInto, int position)
    {
        this.toInsert = toInsert;
        this.insertInto = insertInto;
        this.position = position;
    }

    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
    {
        if (this.insertInto == node && this.toInsert is DeclarationSyntax decl)
        {
            return new CompilationUnitSyntax(this.InsertIntoSyntaxList(node.Declarations, decl), node.End);
        }
        return base.VisitCompilationUnit(node);
    }

    public override SyntaxNode VisitBlockFunctionBody(BlockFunctionBodySyntax node)
    {
        if (this.insertInto == node && this.toInsert is StatementSyntax stmt)
        {
            return new BlockFunctionBodySyntax(node.OpenBrace, this.InsertIntoSyntaxList(node.Statements, stmt), node.CloseBrace);
        }
        return base.VisitBlockFunctionBody(node);
    }

    public override SyntaxNode VisitBlockExpression(BlockExpressionSyntax node)
    {
        if (this.insertInto == node && this.toInsert is StatementSyntax stmt)
        {
            return new BlockExpressionSyntax(node.OpenBrace, this.InsertIntoSyntaxList(node.Statements, stmt), node.Value, node.CloseBrace);
        }
        return base.VisitBlockExpression(node);
    }

    private SyntaxList<T> InsertIntoSyntaxList<T>(SyntaxList<T> original, T insertion)
        where T : SyntaxNode
    {
        var list = original.ToList();
        list.Insert(this.position, insertion);
        var builder = SyntaxList.CreateBuilder<T>();
        builder.AddRange(list);
        return builder.ToSyntaxList();
    }

    public override SyntaxNode VisitSyntaxToken(SyntaxToken node) => node;
}
