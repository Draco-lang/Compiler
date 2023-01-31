using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Utilities for syntax.
/// </summary>
public static class SyntaxFacts
{
    /// <summary>
    /// Attempts to retrieve the textual representation of a token type.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to get the text of.</param>
    /// <returns>The textual representation of <paramref name="tokenType"/>, or null, if it doesn't have a
    /// unique representation.</returns>
    public static string? GetTokenText(TokenType tokenType) => tokenType switch
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
        _ => null,
    };

    /// <summary>
    /// Attempts to retrieve a user-friendly name for a <see cref="TokenType"/>.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to get the user-friendly name for.</param>
    /// <returns>The user-friendly name of <paramref name="tokenType"/>.</returns>
    public static string GetUserFriendlyName(TokenType tokenType) => tokenType switch
    {
        TokenType.EndOfInput => "end of file",
        TokenType.LineStringEnd or TokenType.MultiLineStringEnd => "end of string literal",
        _ => tokenType.GetTokenTextOrNull() ?? tokenType.ToString().ToLower(),
    };

    /// <summary>
    /// Retrieves the operator that a given <see cref="TokenType"/> has for compound assignment.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to check.</param>
    /// <returns>The corresponding <see cref="TokenType"/> of the operator, if <paramref name="tokenType"/> is
    /// a compound assignment, null otherwise..</returns>
    public static TokenType? GetOperatorOfCompoundAssignment(TokenType tokenType) => tokenType switch
    {
        TokenType.PlusAssign => TokenType.Plus,
        TokenType.MinusAssign => TokenType.Minus,
        TokenType.StarAssign => TokenType.Star,
        TokenType.SlashAssign => TokenType.Slash,
        _ => null,
    };
}
