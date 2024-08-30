using System;
using System.Diagnostics;

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
        TokenKind.CMod => "%",
        TokenKind.COr => "||",
        TokenKind.CAnd => "&&",
        TokenKind.CNot => "!",
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
    /// Returns the replacement token for a given C-heritage token.
    /// </summary>
    /// <param name="tokenKind">The <see cref="TokenKind"/> of the heritage token.</param>
    /// <returns>The syntactically valid token which replaces the <paramref name="tokenKind"/> heritage token, or null if <paramref name="tokenKind"/> is not a heritage token.</returns>
    public static TokenKind? GetHeritageReplacement(TokenKind tokenKind) => tokenKind switch
    {
        TokenKind.CMod => TokenKind.KeywordMod,
        TokenKind.COr => TokenKind.KeywordOr,
        TokenKind.CAnd => TokenKind.KeywordAnd,
        TokenKind.CNot => TokenKind.KeywordNot,
        _ => null
    };

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

    /// <summary>
    /// Extracts the import path from an <see cref="ImportPathSyntax"/> as a string.
    /// </summary>
    /// <param name="path">The path to extract.</param>
    /// <returns>The string representation of <paramref name="path"/>.</returns>
    public static string ImportPathToString(ImportPathSyntax path) => path switch
    {
        RootImportPathSyntax root => root.Name.Text,
        MemberImportPathSyntax member => $"{ImportPathToString(member.Accessed)}.{member.Member.Text}",
        _ => throw new ArgumentOutOfRangeException(nameof(path)),
    };

    /// <summary>
    /// Checks, if the given node is a complete entry.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True, if <paramref name="node"/> is a complete entry, false otherwise.</returns>
    public static bool IsCompleteEntry(SyntaxNode? node)
    {
        static bool IsMissing(SyntaxNode? node) => node switch
        {
            SyntaxToken t => t.Kind != TokenKind.EndOfInput && t.Text.Length == 0,
            UnexpectedDeclarationSyntax d => d.Nodes.Count == 0,
            UnexpectedFunctionBodySyntax b => b.Nodes.Count == 0,
            UnexpectedTypeSyntax t => t.Nodes.Count == 0,
            UnexpectedStatementSyntax s => s.Nodes.Count == 0,
            UnexpectedExpressionSyntax e => e.Nodes.Count == 0,
            UnexpectedStringPartSyntax p => p.Nodes.Count == 0,
            _ => false,
        };

        if (node is null) return true;
        return node switch
        {
            _ when IsMissing(node) => false,
            CompilationUnitSyntax cu => !IsMissing(cu.Declarations[^1]),
            GenericParameterListSyntax gpl => !IsMissing(gpl.CloseBracket),
            ModuleDeclarationSyntax md => !IsMissing(md.CloseBrace),
            ImportDeclarationSyntax id => !IsMissing(id.Semicolon),
            FunctionDeclarationSyntax fd => IsCompleteEntry(fd.Body),
            ParameterSyntax p => IsCompleteEntry(p.Type),
            BlockFunctionBodySyntax bfb => !IsMissing(bfb.CloseBrace),
            InlineFunctionBodySyntax ifb => !IsMissing(ifb.Semicolon),
            LabelDeclarationSyntax ld => !IsMissing(ld.Colon),
            VariableDeclarationSyntax vd => !IsMissing(vd.Semicolon),
            NameTypeSyntax nt => !IsMissing(nt.Name),
            MemberTypeSyntax mt => !IsMissing(mt.Member),
            GenericTypeSyntax gt => !IsMissing(gt.CloseBracket),
            DeclarationStatementSyntax ds => IsCompleteEntry(ds.Declaration),
            ExpressionStatementSyntax es => IsCompleteEntry(es.Expression) && !IsMissing(es.Semicolon),
            StatementExpressionSyntax se => IsCompleteEntry(se.Statement),
            BlockExpressionSyntax be => !IsMissing(be.CloseBrace),
            IfExpressionSyntax ie => IsCompleteEntry(ie.Then) && IsCompleteEntry(ie.Else),
            WhileExpressionSyntax we => IsCompleteEntry(we.Then),
            ForExpressionSyntax fe => IsCompleteEntry(fe.Then),
            GotoExpressionSyntax ge => IsCompleteEntry(ge.Target),
            ReturnExpressionSyntax re => IsCompleteEntry(re.Value),
            LiteralExpressionSyntax le => !IsMissing(le.Literal),
            CallExpressionSyntax ce => !IsMissing(ce.CloseParen),
            IndexExpressionSyntax ie => !IsMissing(ie.CloseBracket),
            GenericExpressionSyntax ge => !IsMissing(ge.CloseBracket),
            NameExpressionSyntax ne => !IsMissing(ne.Name),
            MemberExpressionSyntax me => !IsMissing(me.Member),
            UnaryExpressionSyntax ue => IsCompleteEntry(ue.Operand),
            BinaryExpressionSyntax be => IsCompleteEntry(be.Right),
            RelationalExpressionSyntax re => IsCompleteEntry(re.Comparisons[^1].Right),
            GroupingExpressionSyntax ge => !IsMissing(ge.CloseParen),
            StringExpressionSyntax se => !IsMissing(se.CloseQuotes),
            NameLabelSyntax nl => !IsMissing(nl.Name),
            ScriptEntrySyntax se => se.Value is null
                ? se.Statements.Count == 0 || IsCompleteEntry(se.Statements[^1])
                : IsCompleteEntry(se.Value),
            _ => true,
        };
    }
}
