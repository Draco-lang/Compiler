using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A single token in the source code, possibly surrounded by trivia.
/// </summary>
internal sealed partial class SyntaxToken : SyntaxNode
{
    /// <summary>
    /// The <see cref="TokenType"/> of this token.
    /// </summary>
    public TokenType Type => throw new NotImplementedException();

    /// <summary>
    /// The text the token was produced from.
    /// </summary>
    public string Text => throw new NotImplementedException();

    /// <summary>
    /// The <see cref="SyntaxTrivia"/> before this token.
    /// </summary>
    public SyntaxList<SyntaxTrivia> LeadingTrivia { get; }

    /// <summary>
    /// The <see cref="SyntaxTrivia"/> after this token.
    /// </summary>
    public SyntaxList<SyntaxTrivia> TrailingTrivia { get; }

    public override IEnumerable<SyntaxNode> Children => throw new NotImplementedException();

    public override Api.Syntax.SyntaxToken ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent) => throw new NotImplementedException();
    public override void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();
}
