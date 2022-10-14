using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// The different kinds of tokens the lexer can recognize.
/// </summary>
internal enum TokenType
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
    /// A character literal.
    /// </summary>
    LiteralCharacter,

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
    /// A newline in multiline strings.
    /// </summary>
    StringNewline,

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
    /// The keyword 'and'.
    /// </summary>
    KeywordAnd,

    /// <summary>
    /// The keyword 'else'.
    /// </summary>
    KeywordElse,

    /// <summary>
    /// The keyword 'false'.
    /// </summary>
    KeywordFalse,

    /// <summary>
    /// The keyword 'from'.
    /// </summary>
    KeywordFrom,

    /// <summary>
    /// The keyword 'func'.
    /// </summary>
    KeywordFunc,

    /// <summary>
    /// The keyword 'goto'.
    /// </summary>
    KeywordGoto,

    /// <summary>
    /// The keyword 'if'.
    /// </summary>
    KeywordIf,

    /// <summary>
    /// The keyword 'import'.
    /// </summary>
    KeywordImport,

    /// <summary>
    /// The keyword 'mod'.
    /// </summary>
    KeywordMod,

    /// <summary>
    /// The keyword 'not'.
    /// </summary>
    KeywordNot,

    /// <summary>
    /// The keyword 'or'.
    /// </summary>
    KeywordOr,

    /// <summary>
    /// The keyword 'rem'.
    /// </summary>
    KeywordRem,

    /// <summary>
    /// The keyword 'return'.
    /// </summary>
    KeywordReturn,

    /// <summary>
    /// The keyword 'true'.
    /// </summary>
    KeywordTrue,

    /// <summary>
    /// The keyword 'val'.
    /// </summary>
    KeywordVal,

    /// <summary>
    /// The keyword 'var'.
    /// </summary>
    KeywordVar,

    /// <summary>
    /// The keyword 'while'
    /// </summary>
    KeywordWhile,

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

    /// <summary>
    /// '+'.
    /// </summary>
    Plus,

    /// <summary>
    /// '-'.
    /// </summary>
    Minus,

    /// <summary>
    /// '*'.
    /// </summary>
    Star,

    /// <summary>
    /// '/'.
    /// </summary>
    Slash,

    /// <summary>
    /// '<'.
    /// </summary>
    LessThan,

    /// <summary>
    /// '>'.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// '<='.
    /// </summary>
    LessEqual,

    /// <summary>
    /// '>='.
    /// </summary>
    GreaterEqual,

    /// <summary>
    /// '=='.
    /// </summary>
    Equal,

    /// <summary>
    /// '!='.
    /// </summary>
    NotEqual,

    /// <summary>
    /// '='.
    /// </summary>
    Assign,

    /// <summary>
    /// '+='.
    /// </summary>
    PlusAssign,

    /// <summary>
    /// '-='.
    /// </summary>
    MinusAssign,

    /// <summary>
    /// '*='.
    /// </summary>
    StarAssign,

    /// <summary>
    /// '/='.
    /// </summary>
    SlashAssign,
}

/// <summary>
/// Extension functionality on <see cref="TokenType"/>.
/// </summary>
internal static class TokenTypeExtensions
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

    /// <summary>
    /// Retrieves the textual representation of a token with a type <paramref name="tokenType"/>.
    /// Illegal to call for token types that have no unique textual representations.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to get the text of.</param>
    /// <returns>The textual representation of a token with type <paramref name="tokenType"/>.</returns>
    public static string GetTokenText(this TokenType tokenType) => tokenType switch
    {
        TokenType.EndOfInput => string.Empty,
        TokenType.InterpolationEnd => "}",
        TokenType.KeywordAnd => "and",
        TokenType.KeywordElse => "else",
        TokenType.KeywordFalse => "false",
        TokenType.KeywordFrom => "from",
        TokenType.KeywordFunc => "func",
        TokenType.KeywordGoto => "goto",
        TokenType.KeywordIf => "if",
        TokenType.KeywordImport => "import",
        TokenType.KeywordMod => "mod",
        TokenType.KeywordNot => "not",
        TokenType.KeywordOr => "or",
        TokenType.KeywordRem => "rem",
        TokenType.KeywordReturn => "return",
        TokenType.KeywordTrue => "true",
        TokenType.KeywordVal => "val",
        TokenType.KeywordVar => "var",
        TokenType.KeywordWhile => "while",
        TokenType.ParenOpen => "(",
        TokenType.ParenClose => ")",
        TokenType.CurlyOpen => "{",
        TokenType.CurlyClose => "}",
        TokenType.BracketOpen => "[",
        TokenType.BracketClose => "]",
        TokenType.Dot => ".",
        TokenType.Comma => ",",
        TokenType.Colon => ":",
        TokenType.Semicolon => ";",
        TokenType.Plus => "+",
        TokenType.Minus => "-",
        TokenType.Star => "*",
        TokenType.Slash => "/",
        TokenType.LessThan => "<",
        TokenType.GreaterThan => ">",
        TokenType.LessEqual => "<=",
        TokenType.GreaterEqual => ">=",
        TokenType.Equal => "==",
        TokenType.NotEqual => "!=",
        TokenType.Assign => "=",
        TokenType.PlusAssign => "+=",
        TokenType.MinusAssign => "-=",
        TokenType.StarAssign => "*=",
        TokenType.SlashAssign => "/=",
        _ => throw new InvalidOperationException($"{tokenType} has no unique text representation"),
    };
}
