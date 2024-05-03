using System;
using System.Linq;

namespace Draco.Compiler.Api.Syntax;
public partial class StringPartSyntax
{
    public bool IsNewLine => this.Children.Count() == 1 && this.Children.SingleOrDefault() is SyntaxToken and { Kind: TokenKind.StringNewline };
}
