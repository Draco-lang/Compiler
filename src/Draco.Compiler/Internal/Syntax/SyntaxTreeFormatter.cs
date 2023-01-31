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
    private static readonly SyntaxList<SyntaxTrivia> oneSpaceTrivia = CreateTrivia(TriviaKind.Whitespace, " ");
    private static readonly SyntaxList<SyntaxTrivia> noSpaceTrivia = CreateTrivia(TriviaKind.Whitespace, "");
    private static readonly SyntaxList<SyntaxTrivia> newlineTrivia = CreateTrivia(TriviaKind.Newline, Environment.NewLine);

    private TokenKind? lastToken;
    private TokenKind? nextToken;
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
        this.lastToken = TokenKind.EndOfInput;
        // We need to be one token ahead, because the next token affects the current one, so we must advance twice here
        if (!(this.tokens.MoveNext() && this.tokens.MoveNext())) return tree;
        this.nextToken = this.tokens.Current.Kind;
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
        this.lastToken = TokenKind.Colon;
        if (this.tokens!.MoveNext()) this.nextToken = this.tokens!.Current.Kind;
        else this.nextToken = TokenKind.EndOfInput;
        var identifierChanged = !ReferenceEquals(node.Name, trIdentifier);
        var changed = identifierChanged || colonTokenChanged;
        if (!changed) return node;
        return new LabelDeclarationSyntax(trIdentifier, trColonToken);
    }

    public override SyntaxToken VisitSyntaxToken(SyntaxToken node)
    {
        if (node.Kind == TokenKind.EndOfInput) return node;
        var resultToken = this.FormatToken(node);
        this.lastToken = resultToken.Kind;
        this.tokens!.MoveNext();
        if (this.tokens.Current is null) this.nextToken = TokenKind.EndOfInput;
        else this.nextToken = this.tokens.Current.Kind;
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
            // Note: TokenKind.Comma is exception, in case it is in label declaration, it is handeled outside of this function
            TokenKind.Assign or TokenKind.Colon or TokenKind.Comma or TokenKind.Equal or
            TokenKind.GreaterEqual or TokenKind.GreaterThan or TokenKind.InterpolationStart or
            TokenKind.KeywordAnd or TokenKind.KeywordFrom or TokenKind.KeywordImport or
            TokenKind.KeywordMod or TokenKind.KeywordNot or TokenKind.KeywordOr or
            TokenKind.KeywordRem or TokenKind.LessEqual or TokenKind.LessThan or
            TokenKind.Minus or TokenKind.MinusAssign or TokenKind.NotEqual or
            TokenKind.Plus or TokenKind.PlusAssign or TokenKind.Slash or
            TokenKind.SlashAssign or TokenKind.Star or TokenKind.StarAssign => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),

            // Tokens that statement can start with, which means they need to have indentation before them
            // Note: TokenKind.Identifier is handeled separately, base on tokens that are before or after the Identifier
            TokenKind.KeywordVal or TokenKind.KeywordVar or TokenKind.KeywordFunc or
            TokenKind.KeywordReturn or TokenKind.KeywordGoto or TokenKind.KeywordWhile =>
                SetTrivia(newToken, CreateTrivia(TriviaKind.Whitespace, this.Indentation), oneSpaceTrivia),

            TokenKind.ParenOpen => SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),

            TokenKind.ParenClose => this.nextToken switch
            {
                TokenKind.ParenClose or TokenKind.Semicolon => SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            TokenKind.Semicolon => this.nextToken switch
            {
                TokenKind.KeywordElse => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, newlineTrivia),
            },

            TokenKind.CurlyOpen => this.lastToken switch
            {
                TokenKind.Semicolon or TokenKind.CurlyOpen or TokenKind.CurlyClose or
                TokenKind.EndOfInput or TokenKind.Colon =>
                    this.AddIndentation(SetTrivia(newToken, CreateTrivia(TriviaKind.Whitespace, this.Indentation), newlineTrivia)),
                _ => SetTrailingTrivia(this.AddIndentation(newToken), newlineTrivia),
            },

            TokenKind.CurlyClose =>
                SetTrivia(this.RemoveIndentation(newToken), CreateTrivia(TriviaKind.Whitespace, this.Indentation), newlineTrivia),

            // If and else keywords are formatted diferently when they are used as expressions and when they are used as statements
            TokenKind.KeywordIf => this.lastToken switch
            {
                TokenKind.Semicolon or TokenKind.CurlyClose or TokenKind.CurlyOpen or TokenKind.Colon =>
                    SetTrivia(newToken, CreateTrivia(TriviaKind.Whitespace, this.Indentation), oneSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            TokenKind.KeywordElse => this.lastToken switch
            {
                TokenKind.CurlyClose or TokenKind.Colon =>
                    SetTrivia(newToken, CreateTrivia(TriviaKind.Whitespace, this.Indentation), oneSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            // Identifier is handeled based on the context in which it is used
            TokenKind.Identifier => (this.lastToken, this.nextToken) switch
            {
                { lastToken: TokenKind.KeywordVal or TokenKind.KeywordVar, nextToken: TokenKind.Colon } =>
                    SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),
                {
                    lastToken: TokenKind.KeywordFrom or TokenKind.KeywordVal or TokenKind.KeywordVar or TokenKind.Colon,
                    nextToken: TokenKind.Semicolon or TokenKind.Assign or TokenKind.PlusAssign or
                    TokenKind.MinusAssign or TokenKind.Slash or TokenKind.StarAssign
                } => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),

                {
                    lastToken: TokenKind.Semicolon or TokenKind.CurlyOpen or
                    TokenKind.CurlyClose or TokenKind.EndOfInput or TokenKind.Colon,
                    nextToken: TokenKind.Assign or TokenKind.PlusAssign or
                    TokenKind.MinusAssign or TokenKind.Slash or TokenKind.StarAssign
                } => SetTrivia(newToken, CreateTrivia(TriviaKind.Whitespace, this.Indentation), oneSpaceTrivia),

                { lastToken: TokenKind.Semicolon or TokenKind.CurlyOpen or TokenKind.CurlyClose or TokenKind.EndOfInput or TokenKind.Colon } =>
                    SetTrivia(newToken, CreateTrivia(TriviaKind.Whitespace, this.Indentation), noSpaceTrivia),

                { nextToken: TokenKind.Semicolon or TokenKind.ParenOpen or TokenKind.ParenClose } =>
                    SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),

                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            // Literals are handeled based on the context in which they are used
            // Note: TokenKind.MultiLineStringEnd is handeled separately, beacause we can't alter its leading trivia
            TokenKind.LiteralInteger or TokenKind.LiteralFloat or TokenKind.KeywordFalse or TokenKind.KeywordTrue or TokenKind.LiteralCharacter or TokenKind.LineStringEnd => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenKind.Semicolon or TokenKind.ParenClose } => SetTrivia(newToken, noSpaceTrivia, noSpaceTrivia),
                _ => SetTrivia(newToken, noSpaceTrivia, oneSpaceTrivia),
            },

            TokenKind.MultiLineStringEnd => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenKind.Semicolon or TokenKind.ParenClose } => SetTrailingTrivia(newToken, noSpaceTrivia),
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

    private static SyntaxList<SyntaxTrivia> CreateTrivia(TriviaKind type, string text) =>
        SyntaxList.Create(SyntaxTrivia.From(type, text));
}
