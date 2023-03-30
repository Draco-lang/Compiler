using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Elements of the source that are not significant for the semantics, like spaces and comments.
/// </summary>
internal sealed class SyntaxTrivia : SyntaxNode
{
    /// <summary>
    /// Construct a <see cref="SyntaxTrivia"/> from the given data.
    /// </summary>
    /// <param name="kind">The <see cref="TriviaKind"/>.</param>
    /// <param name="text">The text the trivia was constructed from.</param>
    /// <returns>A new <see cref="SyntaxTrivia"/> with <see cref="Kind"/> <paramref name="kind"/> and
    /// <see cref="Text"/> <paramref name="text"/>.</returns>
    public static SyntaxTrivia From(TriviaKind kind, string text) => new(kind, text);

    /// <summary>
    /// The <see cref="TriviaKind"/> of this trivia.
    /// </summary>
    public TriviaKind Kind { get; }

    /// <summary>
    /// The text the trivia was produced from.
    /// </summary>
    public string Text { get; }

    public override int FullWidth => this.Text.Length;

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    public SyntaxTrivia(TriviaKind kind, string text)
    {
        this.Kind = kind;
        this.Text = text;
    }

    public override Api.Syntax.SyntaxTrivia ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent) => new(tree, parent, this);
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSyntaxTrivia(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSyntaxTrivia(this);
}
