using System;
using System.Linq;

namespace Draco.Compiler.Api.Syntax;
public partial class StringPartSyntax
{
    /// <summary>
    /// <see langword="true"/> when this <see cref="StringPartSyntax"/> is a <see cref="SyntaxToken"/> with <see cref="TokenKind.StringNewline"/>.
    /// </summary>
    public bool IsNewLine => this.Children.Count() == 1 && this.Children.SingleOrDefault() is SyntaxToken and { Kind: TokenKind.StringNewline };
}
