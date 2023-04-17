namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class ReplaceRewriter : SyntaxRewriter
{
    private SyntaxNode Original { get; }
    private SyntaxNode Replacement { get; }
    public ReplaceRewriter(SyntaxNode original, SyntaxNode replacement)
    {
        this.Original = original;
        this.Replacement = replacement;
    }

    public override SyntaxNode VisitStatement(StatementSyntax node)
    {
        if (node == this.Original) return this.Replacement;
        return base.VisitStatement(node);
    }

    public override SyntaxNode VisitExpression(ExpressionSyntax node)
    {
        if (node == this.Original) return this.Replacement;
        return base.VisitExpression(node);
    }

    public override SyntaxNode VisitDeclaration(DeclarationSyntax node)
    {
        if (node == this.Original) return this.Replacement;
        return base.VisitDeclaration(node);
    }
}
