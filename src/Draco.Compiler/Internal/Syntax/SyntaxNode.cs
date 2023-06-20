using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A single node in the Draco syntax tree.
/// </summary>
internal abstract class SyntaxNode
{
    /// <summary>
    /// The width of this node in characters, including the leading and trailing trivia.
    /// </summary>
    public virtual int FullWidth => this.Children.Select(c => c.FullWidth).Sum();

    /// <summary>
    /// The width of this node in characters, not including the surrounding trivia.
    /// </summary>
    public int Width
    {
        get
        {
            var width = this.FullWidth;
            var leadingTrivia = this.FirstToken?.LeadingTrivia;
            var trailingTrivia = this.LastToken?.TrailingTrivia;
            if (leadingTrivia is not null) width -= leadingTrivia.FullWidth;
            if (trailingTrivia is not null) width -= trailingTrivia.FullWidth;
            return width;
        }
    }

    /// <summary>
    /// The immediate descendant nodes of this one.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> Children { get; }

    /// <summary>
    /// All <see cref="SyntaxToken"/>s this node consists of.
    /// </summary>
    public IEnumerable<SyntaxToken> Tokens => this.PreOrderTraverse().OfType<SyntaxToken>();

    /// <summary>
    /// Retrieves the first token within this node.
    /// </summary>
    public SyntaxToken? FirstToken
    {
        get
        {
            var node = this;
            while (true)
            {
                if (node is SyntaxTrivia) return null;
                if (node is SyntaxToken token) return token;
                node = node.Children.FirstOrDefault();
                if (node is null) return null;
            }
        }
    }

    /// <summary>
    /// Retrieves the last token within this node.
    /// </summary>
    public SyntaxToken? LastToken
    {
        get
        {
            var node = this;
            while (true)
            {
                if (node is SyntaxTrivia) return null;
                if (node is SyntaxToken token) return token;
                node = node.Children.LastOrDefault();
                if (node is null) return null;
            }
        }
    }

    /// <summary>
    /// The documentation attached before this node.
    /// </summary>
    public string Documentation => this.ExtractDocumentation();

    /// <summary>
    /// Preorder traverses the subtree with this node being the root.
    /// </summary>
    /// <returns>The enumerator that performs a preorder traversal.</returns>
    public IEnumerable<SyntaxNode> PreOrderTraverse()
    {
        yield return this;
        foreach (var child in this.Children)
        {
            foreach (var e in child.PreOrderTraverse()) yield return e;
        }
    }

    /// <summary>
    /// Searches for a child node of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">The type of child to search for.</typeparam>
    /// <param name="index">The index of the child to search for.</param>
    /// <returns>The <paramref name="index"/>th child of type <typeparamref name="TNode"/>.</returns>
    public TNode FindInChildren<TNode>(int index = 0)
        where TNode : SyntaxNode => this
        .PreOrderTraverse()
        .OfType<TNode>()
        .ElementAt(index);

    /// <summary>
    /// Prints this syntax node as the text it was parsed from.
    /// </summary>
    /// <returns>This syntax node printed to text, identical to the text it was parsed from.</returns>
    public string ToCode()
    {
        var result = new StringBuilder();
        foreach (var token in this.Tokens)
        {
            foreach (var t in token.LeadingTrivia) result.Append(t.Text);
            result.Append(token.Text);
            foreach (var t in token.TrailingTrivia) result.Append(t.Text);
        }
        return result.ToString();
    }

    /// <summary>
    /// Prints this syntax node as the text it was parsed from, discarding the very first leading trivia
    /// and the very last trailing trivia, "trimming" the code.
    /// </summary>
    /// <returns>This syntax node printed to text, without the surrounding trivia.</returns>
    public string ToCodeWithoutSurroundingTrivia()
    {
        var result = new StringBuilder();
        // We simply print the text of all tokens except the first and last ones
        // For the first, we ignore leading trivia, for the last we ignore trailing trivia
        var lastTrailingTrivia = SyntaxList<SyntaxTrivia>.Empty;
        using var tokenEnumerator = this.Tokens.GetEnumerator();
        // The first token just gets it's content printed
        // That ignores the leading trivia, trailing will only be printed if there are following tokens
        var hasFirstToken = tokenEnumerator.MoveNext();
        if (!hasFirstToken) return string.Empty;
        var firstToken = tokenEnumerator.Current;
        result.Append(firstToken.Text);
        lastTrailingTrivia = firstToken.TrailingTrivia;
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            // Last trailing trivia
            foreach (var t in lastTrailingTrivia) result.Append(t.Text);
            // Leading trivia
            foreach (var t in token.LeadingTrivia) result.Append(t.Text);
            // Content
            result.Append(token.Text);
            // Trailing trivia
            lastTrailingTrivia = token.TrailingTrivia;
        }
        return result.ToString();
    }

    /// <summary>
    /// Converts this syntax-tree to a DOT graph.
    /// </summary>
    /// <returns>The DOT-graph of this tree.</returns>
    public string ToDot()
    {
        var graph = new DotGraphBuilder<SyntaxNode>(isDirected: false, vertexComparer: ReferenceEqualityComparer.Instance);
        graph.WithName("SyntaxTree");

        void Recurse(SyntaxNode node)
        {
            graph!
                .AddVertex(node)
                .WithLabel(node is SyntaxToken t ? t.Text : node.GetType().Name);
            // Children
            foreach (var child in node.Children)
            {
                graph.AddEdge(node, child);
                Recurse(child);
            }
        }

        Recurse(this);

        return graph.ToDot();
    }

    private string ExtractDocumentation()
    {
        // The documentation is in the leading trivia of the first token
        var firstToken = this.Tokens.FirstOrDefault();
        if (firstToken is null) return string.Empty;

        var trivia = firstToken.LeadingTrivia;
        var i = 0;

        // We skip trivia that isn't documentation
        while (i < trivia.Count && trivia[i].Kind != Api.Syntax.TriviaKind.DocumentationComment) ++i;
        if (i == trivia.Count) return string.Empty;

        // We have at least one line of trivia
        var result = new StringBuilder();
        result.Append(trivia[i].Text[3..]);
        ++i;

        // After that, we consume pairs of newline and doc comment trivia
        while (i + 1 < trivia.Count
            && trivia[i].Kind == Api.Syntax.TriviaKind.Newline
            && trivia[i + 1].Kind == Api.Syntax.TriviaKind.DocumentationComment)
        {
            result
                .Append(trivia[i].Text)
                .Append(trivia[i + 1].Text[3..]);
            i += 2;
        }

        return result.ToString();
    }

    public abstract Api.Syntax.SyntaxNode ToRedNode(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode? parent, int fullWidth);
    public abstract void Accept(SyntaxVisitor visitor);
    public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
