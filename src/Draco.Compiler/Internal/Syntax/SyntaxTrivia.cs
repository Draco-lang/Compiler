using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// <param name="type">The <see cref="TriviaType"/>.</param>
    /// <param name="text">The text the trivia was constructed from.</param>
    /// <returns>A new <see cref="SyntaxTrivia"/> with <see cref="Type"/> <paramref name="type"/> and
    /// <see cref="Text"/> <paramref name="text"/>.</returns>
    public static SyntaxTrivia From(TriviaType type, string text) => new(type, text);

    /// <summary>
    /// The <see cref="TriviaType"/> of this trivia.
    /// </summary>
    public TriviaType Type { get; }

    /// <summary>
    /// The text the trivia was produced from.
    /// </summary>
    public string Text { get; }

    public override int Width => this.Text.Length;

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    public SyntaxTrivia(TriviaType type, string text)
    {
        this.Type = type;
        this.Text = text;
    }

    public override Api.Syntax.SyntaxTrivia ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent) => throw new NotImplementedException();
    public override void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();
}
