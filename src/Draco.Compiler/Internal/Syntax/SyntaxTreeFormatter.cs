using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Settings that the formatter will use.
/// </summary>
internal sealed record class SyntaxTreeFormatterSettings(string Indentation)
{
    internal static readonly SyntaxTreeFormatterSettings Default = new("    ");
}

/// <summary>
/// The formatter for <see cref="SyntaxNode"/>.
/// </summary>
internal sealed class SyntaxTreeFormatter : SyntaxRewriter
{
    private static readonly SyntaxList<SyntaxTrivia> oneSpaceTrivia = CreateTrivia(TriviaType.Whitespace, " ");
    private static readonly SyntaxList<SyntaxTrivia> noSpaceTrivia = CreateTrivia(TriviaType.Whitespace, "");
    private static readonly SyntaxList<SyntaxTrivia> newlineTrivia = CreateTrivia(TriviaType.Newline, Environment.NewLine);

    private TokenType? lastToken;
    private TokenType? nextToken;
    private IEnumerator<SyntaxToken>? tokens;
    private readonly SyntaxTreeFormatterSettings settings;
    private int indentCount = 0;
    private string Indentation
    {
        get
        {
            var result = new StringBuilder();
            for (var i = 0; i < this.indentCount; ++i) result.Append(this.settings.Indentation);
            return result.ToString();
        }
    }

    internal SyntaxTreeFormatter(SyntaxTreeFormatterSettings settings)
    {
        this.settings = settings;
    }

    private SyntaxToken.Builder AddIndentation(SyntaxToken.Builder newToken)
    {
        this.AddIndentation();
        return newToken;
    }

    private SyntaxToken.Builder RemoveIndentation(SyntaxToken.Builder newToken)
    {
        this.RemoveIndentation();
        return newToken;
    }

    private void AddIndentation() => this.indentCount++;

    private void RemoveIndentation() => this.indentCount--;

    /// <summary>
    /// Formats the given <see cref="SyntaxTree"/>.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxTree"/> to be formatted.</param>
    /// <returns>The formatted <paramref name="tree"/>.</returns>
    public SyntaxTree Format(SyntaxTree tree) => new(
        sourceText: tree.SourceText,
        greenRoot: this.Format(tree.GreenRoot),
        // TODO: Anything smarter to pass here?
        syntaxDiagnostics: new());

    /// <summary>
    /// Formats the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="tree">The <see cref="SyntaxNode"/> to be formatted.</param>
    /// <returns>The formatted <paramref name="tree"/>.</returns>
    public SyntaxNode Format(SyntaxNode tree)
    {
        this.tokens = tree.Tokens.GetEnumerator();
        this.lastToken = TokenType.EndOfInput;
        // We need to be one token ahead, because the next token affects the current one, so we must advance twice here
        if (!(this.tokens.MoveNext() && this.tokens.MoveNext())) return tree;
        this.nextToken = this.tokens.Current.Type;
        return tree.Accept(this);
    }

    public override SyntaxNode VisitLabelDeclaration(LabelDeclarationSyntax node)
    {
        // Labels are indented one less than te rest of the code
        this.RemoveIndentation();
        var trIdentifier = this.VisitSyntaxToken(node.Name);
        var trColonToken = SetTrivia(SyntaxToken.Builder.From(node.Colon), noSpaceTrivia, newlineTrivia).Build();
        this.AddIndentation();
        var colonTokenChanged = CheckTriviaEqual(trColonToken, node.Colon);
        // We need to advance to the next token by hand, because we don't call TransformToken
        this.lastToken = TokenType.Colon;
        if (this.tokens!.MoveNext()) this.nextToken = this.tokens!.Current.Type;
        else this.nextToken = TokenType.EndOfInput;
        var identifierChanged = !ReferenceEquals(node.Name, trIdentifier);
        var changed = identifierChanged || colonTokenChanged;
        if (!changed) return node;
        return new LabelDeclarationSyntax(trIdentifier, trColonToken);
    }

    public override SyntaxToken VisitSyntaxToken(SyntaxToken node)
    {
        if (node.Type == TokenType.EndOfInput) return node;
        var resultToken = this.FormatToken(node);
        this.lastToken = resultToken.Type;
        this.tokens!.MoveNext();
        if (this.tokens.Current is null) this.nextToken = TokenType.EndOfInput;
        else this.nextToken = this.tokens.Current.Type;
        if (resultToken is not null)
        {
            // If the original token andthe new one have the same trivia, we set  changed to false, so the entire subtree is not recalculated
            if (!CheckTriviaEqual(resultToken, node)) return resultToken;
        }
        // No change
        return node;
    }

    private SyntaxToken FormatToken(SyntaxToken token)
    {
        var newToken = token.ToBuilder();

        newToken = newToken.Type switch
        {
            // TODO: Maybe use dictionary in future to allow user to alter "stickiness" of some tokens
            // Tokens that always have one space after
            // Note: TokenType.Comma is exception, in case it is in label declaration, it is handeled outside of this function
            TokenType.Assign or TokenType.Colon or TokenType.Comma or TokenType.Equal or
            TokenType.GreaterEqual or TokenType.GreaterThan or TokenType.InterpolationStart or
            TokenType.KeywordAnd or TokenType.KeywordFrom or TokenType.KeywordImport or
            TokenType.KeywordMod or TokenType.KeywordNot or TokenType.KeywordOr or
            TokenType.KeywordRem or TokenType.LessEqual or TokenType.LessThan or
            TokenType.Minus or TokenType.MinusAssign or TokenType.NotEqual or
            TokenType.Plus or TokenType.PlusAssign or TokenType.Slash or
            TokenType.SlashAssign or TokenType.Star or TokenType.StarAssign => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),

            // Tokens that statement can start with, which means they need to have indentation before them
            // Note: TokenType.Identifier is handeled separately, base on tokens that are before or after the Identifier
            TokenType.KeywordVal or TokenType.KeywordVar or TokenType.KeywordFunc or
            TokenType.KeywordReturn or TokenType.KeywordGoto or TokenType.KeywordWhile =>
                SetTrivia(newToken, CreateTrivia(TriviaType.Whitespace, this.Indentation), oneSpaceTrivia),

            TokenType.ParenOpen => SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),

            TokenType.ParenClose => this.nextToken switch
            {
                TokenType.ParenClose or TokenType.Semicolon => SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            TokenType.Semicolon => this.nextToken switch
            {
                TokenType.KeywordElse => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, newlineTrivia),
            },

            TokenType.CurlyOpen => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyOpen or TokenType.CurlyClose or
                TokenType.EndOfInput or TokenType.Colon =>
                    this.AddIndentation(SetTrivia(newToken, CreateTrivia(TriviaType.Whitespace, this.Indentation), newlineTrivia)),
                _ => SetTrailingTrivia(this.AddIndentation(newToken), newlineTrivia),
            },

            TokenType.CurlyClose =>
                SetTrivia(this.RemoveIndentation(newToken), CreateTrivia(TriviaType.Whitespace, this.Indentation), newlineTrivia),

            // If and else keywords are formatted diferently when they are used as expressions and when they are used as statements
            TokenType.KeywordIf => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose or TokenType.CurlyOpen or TokenType.Colon =>
                    SetTrivia(newToken, CreateTrivia(TriviaType.Whitespace, this.Indentation), oneSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            TokenType.KeywordElse => this.lastToken switch
            {
                TokenType.CurlyClose or TokenType.Colon =>
                    SetTrivia(newToken, CreateTrivia(TriviaType.Whitespace, this.Indentation), oneSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            // Identifier is handeled based on the context in which it is used
            TokenType.Identifier => (this.lastToken, this.nextToken) switch
            {
                { lastToken: TokenType.KeywordVal or TokenType.KeywordVar, nextToken: TokenType.Colon } =>
                    SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),
                {
                    lastToken: TokenType.KeywordFrom or TokenType.KeywordVal or TokenType.KeywordVar or TokenType.Colon,
                    nextToken: TokenType.Semicolon or TokenType.Assign or TokenType.PlusAssign or
                    TokenType.MinusAssign or TokenType.Slash or TokenType.StarAssign
                } => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),

                {
                    lastToken: TokenType.Semicolon or TokenType.CurlyOpen or
                    TokenType.CurlyClose or TokenType.EndOfInput or TokenType.Colon,
                    nextToken: TokenType.Assign or TokenType.PlusAssign or
                    TokenType.MinusAssign or TokenType.Slash or TokenType.StarAssign
                } => SetTrivia(newToken, CreateTrivia(TriviaType.Whitespace, this.Indentation), oneSpaceTrivia),

                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen or TokenType.CurlyClose or TokenType.EndOfInput or TokenType.Colon } =>
                    SetTrivia(newToken, CreateTrivia(TriviaType.Whitespace, this.Indentation), noSpaceTrivia),

                { nextToken: TokenType.Semicolon or TokenType.ParenOpen or TokenType.ParenClose } =>
                    SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),

                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            // Literals are handeled based on the context in which they are used
            // Note: TokenType.MultiLineStringEnd is handeled separately, beacause we can't alter its leading trivia
            TokenType.LiteralInteger or TokenType.LiteralFloat or TokenType.KeywordFalse or TokenType.KeywordTrue or TokenType.LiteralCharacter or TokenType.LineStringEnd => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenType.Semicolon or TokenType.ParenClose } => SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            TokenType.MultiLineStringEnd => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenType.Semicolon or TokenType.ParenClose } => SetTrailingTrivia(newToken, noSpaceTrivia),
                _ => SetTrailingTrivia(newToken, oneSpaceTrivia),
            },

            _ => newToken,
        };

        return newToken.Build();
    }

    private static SyntaxToken.Builder SetTrivia(
        SyntaxToken.Builder builder,
        SyntaxList<SyntaxTrivia> leading,
        SyntaxList<SyntaxTrivia> trailing) => SetTrailingTrivia(SetLeadingTrivia(builder, leading), trailing);

    private static SyntaxToken.Builder SetLeadingTrivia(SyntaxToken.Builder builder, SyntaxList<SyntaxTrivia> trivia)
    {
        builder.LeadingTrivia.Clear();
        builder.LeadingTrivia.AddRange(trivia);
        return builder;
    }

    private static SyntaxToken.Builder SetTrailingTrivia(SyntaxToken.Builder builder, SyntaxList<SyntaxTrivia> trivia)
    {
        builder.TrailingTrivia.Clear();
        builder.TrailingTrivia.AddRange(trivia);
        return builder;
    }

    private static bool CheckTriviaEqual(SyntaxToken tok1, SyntaxToken tok2)
    {
        if (tok1.TrailingTrivia.Count != tok2.TrailingTrivia.Count) return false;
        if (tok1.LeadingTrivia.Count != tok2.LeadingTrivia.Count) return false;

        for (var i = 0; i < tok1.TrailingTrivia.Count; i++)
        {
            if (tok1.TrailingTrivia[i].Text != tok2.TrailingTrivia[i].Text) return false;
        }
        for (var i = 0; i < tok1.LeadingTrivia.Count; i++)
        {
            if (tok1.LeadingTrivia[i].Text != tok2.LeadingTrivia[i].Text) return false;
        }

        return true;
    }

    private static SyntaxList<SyntaxTrivia> CreateTrivia(TriviaType type, string text) =>
        SyntaxList.Create(SyntaxTrivia.From(type, text));
}
