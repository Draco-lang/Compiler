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
    /// <summary>
    /// The end of the read source.
    /// </summary>
    EndOfInput,

    /// <summary>
    /// Any unknown character.
    /// </summary>
    Unknown,

    /// <summary>
    /// Any horizontal whitespace.
    /// </summary>
    Whitespace,

    /// <summary>
    /// Any newline sequence.
    /// </summary>
    Newline,

    /// <summary>
    /// Single line comments.
    /// </summary>
    LineComment,

    /// <summary>
    /// Non-keyword names.
    /// </summary>
    Identifier,

    /// <summary>
    /// An integer literal.
    /// </summary>
    LiteralInteger,

    /// <summary>
    /// The start of a single-line string literal.
    /// </summary>
    LineStringStart,

    /// <summary>
    /// The end of a single-line string literal.
    /// </summary>
    LineStringEnd,

    /// <summary>
    /// The start of a multi-line string literal.
    /// </summary>
    MultiLineStringStart,

    /// <summary>
    /// The end of a multi-line string literal.
    /// </summary>
    MultiLineStringEnd,

    /// <summary>
    /// A sequence of characters in a string-literal.
    /// </summary>
    StringContent,

    /// <summary>
    /// The start of string interpolation.
    /// </summary>
    InterpolationStart,

    /// <summary>
    /// The end of string interpolation.
    /// </summary>
    InterpolationEnd,

    /// <summary>
    /// An escape sequence inside the string literal.
    /// </summary>
    EscapeSequence,

    /// <summary>
    /// The keyword 'from'.
    /// </summary>
    KeywordFrom,

    /// <summary>
    /// The keyword 'func'.
    /// </summary>
    KeywordFunc,

    /// <summary>
    /// The keyword 'import'.
    /// </summary>
    KeywordImport,

    /// <summary>
    /// '('.
    /// </summary>
    ParenOpen,

    /// <summary>
    /// ')'.
    /// </summary>
    ParenClose,

    /// <summary>
    /// '{'.
    /// </summary>
    CurlyOpen,

    /// <summary>
    /// '}'.
    /// </summary>
    CurlyClose,

    /// <summary>
    /// '['.
    /// </summary>
    BracketOpen,

    /// <summary>
    /// ']'.
    /// </summary>
    BracketClose,

    /// <summary>
    /// '.'.
    /// </summary>
    Dot,

    /// <summary>
    /// ','.
    /// </summary>
    Comma,

    /// <summary>
    /// ':'.
    /// </summary>
    Colon,

    /// <summary>
    /// ';'.
    /// </summary>
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
