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
    /// Attempts to retrieve the textual representation of a <see cref="TokenKind"/>.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to get the text of.</param>
    /// <returns>The textual representation of <paramref name="tokenKind"/>, or null, if it doesn't have a
    /// unique representation.</returns>
    public static string? GetTokenText(TokenKind tokenKind) => tokenKind switch
    {
        TokenKind.EndOfInput => string.Empty,
        TokenKind.InterpolationEnd => "}",
        TokenKind.KeywordAnd => "and",
        TokenKind.KeywordElse => "else",
        TokenKind.KeywordFalse => "false",
        TokenKind.KeywordFrom => "from",
        TokenKind.KeywordFunc => "func",
        TokenKind.KeywordGoto => "goto",
        TokenKind.KeywordIf => "if",
        TokenKind.KeywordImport => "import",
        TokenKind.KeywordMod => "mod",
        TokenKind.KeywordNot => "not",
        TokenKind.KeywordOr => "or",
        TokenKind.KeywordRem => "rem",
        TokenKind.KeywordReturn => "return",
        TokenKind.KeywordTrue => "true",
        TokenKind.KeywordVal => "val",
        TokenKind.KeywordVar => "var",
        TokenKind.KeywordWhile => "while",
        TokenKind.ParenOpen => "(",
        TokenKind.ParenClose => ")",
        TokenKind.CurlyOpen => "{",
        TokenKind.CurlyClose => "}",
        TokenKind.BracketOpen => "[",
        TokenKind.BracketClose => "]",
        TokenKind.Dot => ".",
        TokenKind.Comma => ",",
        TokenKind.Colon => ":",
        TokenKind.Semicolon => ";",
        TokenKind.Plus => "+",
        TokenKind.Minus => "-",
        TokenKind.Star => "*",
        TokenKind.Slash => "/",
        TokenKind.LessThan => "<",
        TokenKind.GreaterThan => ">",
        TokenKind.LessEqual => "<=",
        TokenKind.GreaterEqual => ">=",
        TokenKind.Equal => "==",
        TokenKind.NotEqual => "!=",
        TokenKind.Assign => "=",
        TokenKind.PlusAssign => "+=",
        TokenKind.MinusAssign => "-=",
        TokenKind.StarAssign => "*=",
        TokenKind.SlashAssign => "/=",
        _ => null,
    };

    /// <summary>
    /// Attempts to retrieve a user-friendly name for a <see cref="TokenKind"/>.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to get the user-friendly name for.</param>
    /// <returns>The user-friendly name of <paramref name="tokenKind"/>.</returns>
    public static string GetUserFriendlyName(TokenKind tokenKind) => tokenKind switch
    {
        TokenKind.EndOfInput => "end of file",
        TokenKind.LineStringEnd or TokenKind.MultiLineStringEnd => "end of string literal",
        _ => GetTokenText(tokenKind) ?? tokenKind.ToString().ToLower(),
    };

    /// <summary>
    /// Retrieves the operator that a given <see cref="TokenKind"/> has for compound assignment.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to check.</param>
    /// <returns>The corresponding <see cref="TokenKind"/> of the operator, if <paramref name="tokenKind"/> is
    /// a compound assignment, null otherwise..</returns>
    public static TokenKind? GetOperatorOfCompoundAssignment(TokenKind tokenKind) => tokenKind switch
    {
        TokenKind.PlusAssign => TokenKind.Plus,
        TokenKind.MinusAssign => TokenKind.Minus,
        TokenKind.StarAssign => TokenKind.Star,
        TokenKind.SlashAssign => TokenKind.Slash,
        _ => null,
    };

    /// <summary>
    /// Checks, if a given <see cref="TokenKind"/> corresponds to a compound assignment operator.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to check.</param>
    /// <returns>True, if <paramref name="tokenKind"/> represents a compound assignment operator, false otherwise.</returns>
    public static bool IsCompoundAssignmentOperator(TokenKind tokenKind) =>
        GetOperatorOfCompoundAssignment(tokenKind) is not null;
}
