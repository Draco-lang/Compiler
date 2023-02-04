namespace Draco.Compiler.Internal.Syntax;

internal abstract partial class SyntaxVisitor
{
    public virtual void VisitSyntaxList<TNode>(SyntaxList<TNode> node)
        where TNode : SyntaxNode
    {
        foreach (var child in node) child.Accept(this);
    }
    public virtual void VisitSeparatedSyntaxList<TNode>(SeparatedSyntaxList<TNode> node)
        where TNode : SyntaxNode
    {
        foreach (var child in node) child.Accept(this);
    }
    public virtual void VisitSyntaxToken(SyntaxToken node) { }
    public virtual void VisitSyntaxTrivia(SyntaxTrivia node) { }
}

internal abstract partial class SyntaxVisitor<TResult>
{
    public virtual TResult VisitSyntaxList<TNode>(SyntaxList<TNode> node)
        where TNode : SyntaxNode
    {
        foreach (var child in node) child.Accept(this);
        return default!;
    }
    public virtual TResult VisitSeparatedSyntaxList<TNode>(SeparatedSyntaxList<TNode> node)
        where TNode : SyntaxNode
    {
        foreach (var child in node) child.Accept(this);
        return default!;
    }
    public virtual TResult VisitSyntaxToken(SyntaxToken node) => default!;
    public virtual TResult VisitSyntaxTrivia(SyntaxTrivia node) => default!;
}
