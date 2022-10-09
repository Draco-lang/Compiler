using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// The different kinds of tokens the lexer can recognize.
/// </summary>
public enum TokenType
{
    EndOfInput,
    Unknown,

    Whitespace,
    Newline,
    LineComment,

    Identifier,

    LiteralInteger,

    KeywordFrom,
    KeywordFunc,
    KeywordImport,

    ParenOpen,
    ParenClose,
    CurlyOpen,
    CurlyClose,
    BracketOpen,
    BracketClose,

    Dot,
    Comma,
    Colon,
    Semicolon,
}

/// <summary>
/// Extension functionality on <see cref="TokenType"/>.
/// </summary>
public static class TokenTypeExtensions
{
    /// <summary>
    /// Checks, if <paramref name="tokenType"/> counts as a trivia category.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to check.</param>
    /// <returns>True, if <paramref name="tokenType"/> is a trivia category.</returns>
    public static bool IsTrivia(this TokenType tokenType) =>
           tokenType == TokenType.Whitespace
        || tokenType == TokenType.Newline
        || tokenType == TokenType.LineComment;
}
