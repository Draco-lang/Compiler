using System.Diagnostics;
using Draco.Compiler.Internal.Syntax.Formatting;
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
        TokenKind.KeywordFor => "for",
        TokenKind.KeywordFunc => "func",
        TokenKind.KeywordGoto => "goto",
        TokenKind.KeywordIf => "if",
        TokenKind.KeywordImport => "import",
        TokenKind.KeywordIn => "in",
        TokenKind.KeywordInternal => "internal",
        TokenKind.KeywordMod => "mod",
        TokenKind.KeywordModule => "module",
        TokenKind.KeywordNot => "not",
        TokenKind.KeywordOr => "or",
        TokenKind.KeywordPublic => "public",
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
        TokenKind.Ellipsis => "...",
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
    /// Checks, if the given operator <see cref="TokenKind"/> is a compound assignment.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to check.</param>
    /// <param name="nonCompoundKind">The non-compound operator equivalent is written here, in case it is a compound
    /// operator.</param>
    /// <returns>True, if <paramref name="tokenKind"/> is a compound assignment, false otherwise.</returns>
    public static bool TryGetOperatorOfCompoundAssignment(TokenKind tokenKind, out TokenKind nonCompoundKind)
    {
        switch (tokenKind)
        {
        case TokenKind.PlusAssign:
            nonCompoundKind = TokenKind.Plus;
            return true;
        case TokenKind.MinusAssign:
            nonCompoundKind = TokenKind.Minus;
            return true;
        case TokenKind.StarAssign:
            nonCompoundKind = TokenKind.Star;
            return true;
        case TokenKind.SlashAssign:
            nonCompoundKind = TokenKind.Slash;
            return true;
        default:
            nonCompoundKind = default;
            return false;
        }
    }

    /// <summary>
    /// Checks, if a given <see cref="TokenKind"/> corresponds to a compound assignment operator.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to check.</param>
    /// <returns>True, if <paramref name="tokenKind"/> represents a compound assignment operator, false otherwise.</returns>
    public static bool IsCompoundAssignmentOperator(TokenKind tokenKind) =>
        TryGetOperatorOfCompoundAssignment(tokenKind, out _);

    /// <summary>
    /// Checks, if a given <see cref="TokenKind"/> corresponds to a relational operator.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to check.</param>
    /// <returns>True, if <paramref name="tokenKind"/> is a relational operator, false otherwise.</returns>
    public static bool IsRelationalOperator(TokenKind tokenKind) => tokenKind
        is TokenKind.Equal
        or TokenKind.NotEqual
        or TokenKind.GreaterThan
        or TokenKind.LessThan
        or TokenKind.GreaterEqual
        or TokenKind.LessEqual;

    /// <summary>
    /// Checks, if a given <see cref="TokenKind"/> represents a keyword.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> to check.</param>
    /// <returns>True, if <paramref name="tokenKind"/> is a keyword, false otherwise.</returns>
    public static bool IsKeyword(TokenKind tokenKind) =>
        tokenKind.ToString().StartsWith("Keyword");

    /// <summary>
    /// Computes the cutoff sequence that is removed from each line of a multiline string.
    /// </summary>
    /// <param name="str">The string syntax to compute the cutoff for.</param>
    /// <returns>The cutoff string for <paramref name="str"/>.</returns>
    public static string ComputeCutoff(StringExpressionSyntax str) => ComputeCutoff(str.Green);

    /// <summary>
    /// See <see cref="ComputeCutoff(StringExpressionSyntax)"/>.
    /// </summary>
    internal static string ComputeCutoff(Internal.Syntax.StringExpressionSyntax str)
    {
        // Line strings have no cutoff
        if (str.OpenQuotes.Kind == TokenKind.LineStringStart) return string.Empty;
        // Multiline strings
        Debug.Assert(str.CloseQuotes.LeadingTrivia.Count <= 2);
        // If this is true, we have malformed input
        if (str.CloseQuotes.LeadingTrivia.Count == 0) return string.Empty;
        // If this is true, there's only newline, no spaces before
        if (str.CloseQuotes.LeadingTrivia.Count == 1) return string.Empty;
        // The first trivia was newline, the second must be spaces
        Debug.Assert(str.CloseQuotes.LeadingTrivia[1].Kind == TriviaKind.Whitespace);
        return str.CloseQuotes.LeadingTrivia[1].Text;
    }

    public static bool WillTokenMerges(string leftToken, string rightToken)
    {
        var lexer = new Lexer(SourceReader.From(leftToken + rightToken), default);
        lexer.Lex();
        var secondToken = lexer.Lex();
        return secondToken.Kind == TokenKind.EndOfInput;
    }
}
