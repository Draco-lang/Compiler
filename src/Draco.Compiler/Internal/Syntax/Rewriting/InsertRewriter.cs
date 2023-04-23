using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class InsertRewriter : SyntaxRewriter
{
    private readonly SyntaxNode ToInsert;
    private readonly SyntaxNode InsertInto;
    private int Position { get; }

    public InsertRewriter(SyntaxNode toInsert, SyntaxNode insertInto, int position)
    {
        this.ToInsert = toInsert;
        this.InsertInto = insertInto;
        this.Position = position;
    }

    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
    {
        if (this.InsertInto == node && this.ToInsert is DeclarationSyntax)
        {
            return new CompilationUnitSyntax(this.InsertIntoSyntaxList(node.Declarations, (DeclarationSyntax)this.ToInsert), node.End);
        }
        return base.VisitCompilationUnit(node);
    }

    public override SyntaxNode VisitBlockFunctionBody(BlockFunctionBodySyntax node)
    {
        if (this.InsertInto == node && this.ToInsert is StatementSyntax)
        {
            return new BlockFunctionBodySyntax(node.OpenBrace, this.InsertIntoSyntaxList(node.Statements, (StatementSyntax)this.ToInsert), node.CloseBrace);
        }
        return base.VisitBlockFunctionBody(node);
    }

    public override SyntaxNode VisitBlockExpression(BlockExpressionSyntax node)
    {
        if (this.InsertInto == node && this.ToInsert is StatementSyntax)
        {
            return new BlockExpressionSyntax(node.OpenBrace, this.InsertIntoSyntaxList(node.Statements, (StatementSyntax)this.ToInsert), node.Value, node.CloseBrace);
        }
        return base.VisitBlockExpression(node);
    }

    private SyntaxList<T> InsertIntoSyntaxList<T>(SyntaxList<T> original, T insertion) where T : SyntaxNode
    {
        var list = original.ToList();
        list.Insert(this.Position, insertion);
        var builder = SyntaxList.CreateBuilder<T>();
        builder.AddRange(list);
        return builder.ToSyntaxList();
    }

    public override SyntaxNode VisitSyntaxToken(SyntaxToken node) => node;
}
