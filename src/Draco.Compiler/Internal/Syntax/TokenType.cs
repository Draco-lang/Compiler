using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Extension functionality on <see cref="TokenType"/>.
/// </summary>
internal static class TokenTypeExtensions
{
    ///// <summary>
    ///// Checks, if <paramref name="tokenType"/> counts as a trivia category.
    ///// </summary>
    ///// <param name="tokenType">The <see cref="TokenType"/> to check.</param>
    ///// <returns>True, if <paramref name="tokenType"/> is a trivia category.</returns>
    //public static bool IsTrivia(this TokenType tokenType) =>
    //       tokenType == TokenType.Whitespace
    //    || tokenType == TokenType.Newline
    //    || tokenType == TokenType.LineComment;

    /// <summary>
    /// Attempts to retrieve the textual representation of a token with a type <paramref name="tokenType"/>.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to get the text of.</param>
    /// <returns>The textual representation of a token with type <paramref name="tokenType"/>,
    /// or null, if it doesn't have any.</returns>
    public static string? GetTokenTextOrNull(this TokenType tokenType) => tokenType switch
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
    /// Retrieves the textual representation of a token with a type <paramref name="tokenType"/>.
    /// Illegal to call for token types that have no unique textual representations.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to get the text of.</param>
    /// <returns>The textual representation of a token with type <paramref name="tokenType"/>.</returns>
    public static string GetTokenText(this TokenType tokenType) =>
           tokenType.GetTokenTextOrNull()
        ?? throw new InvalidOperationException($"{tokenType} has no unique text representation");

    /// <summary>
    /// Retrieves a user-friendly name for <paramref name="tokenType"/>.
    /// </summary>
    /// <param name="tokenType">The <see cref="TokenType"/> to get the user-friendly name for.</param>
    /// <returns>The user-friendly name of <paramref name="tokenType"/>.</returns>
    public static string GetUserFriendlyName(this TokenType tokenType) => tokenType switch
    {
        TokenType.EndOfInput => "end of file",
        _ => tokenType.GetTokenTextOrNull() ?? tokenType.ToString().ToLower(),
    };
}
