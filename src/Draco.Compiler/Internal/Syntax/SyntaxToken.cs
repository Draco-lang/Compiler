using System;
using System.Collections.Generic;
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

    public override IEnumerable<SyntaxNode> Children => throw new NotImplementedException();

    public override Api.Syntax.SyntaxToken ToRedNode(SyntaxTree tree, Api.Syntax.SyntaxNode? parent) => throw new NotImplementedException();
    public override void Accept(SyntaxVisitor visitor) => throw new NotImplementedException();
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => throw new NotImplementedException();
}
