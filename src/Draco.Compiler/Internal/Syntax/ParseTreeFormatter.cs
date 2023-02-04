using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Token = Draco.Compiler.Internal.Syntax.ParseNode.Token;
using Trivia = Draco.Compiler.Internal.Syntax.ParseNode.Trivia;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Settings that the formatter will use.
/// </summary>
internal sealed record class ParseTreeFormatterSettings(string Indentation)
{
    internal static readonly ParseTreeFormatterSettings Default = new("    ");
}

/// <summary>
/// The formatter for <see cref="ParseNode"/>.
/// </summary>
internal sealed class ParseTreeFormatter : ParseTreeTransformerBase
{
    private static readonly ImmutableArray<Trivia> oneSpaceTrivia = CreateTrivia(TriviaType.Whitespace, " ");
    private static readonly ImmutableArray<Trivia> noSpaceTrivia = CreateTrivia(TriviaType.Whitespace, "");
    private static readonly ImmutableArray<Trivia> newlineTrivia = CreateTrivia(TriviaType.Newline, Environment.NewLine);

    private TokenType? lastToken;
    private TokenType? nextToken;
    private IEnumerator<Token>? tokens;
    private readonly ParseTreeFormatterSettings settings;
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

    internal ParseTreeFormatter(ParseTreeFormatterSettings settings)
    {
        this.settings = settings;
    }

    private static IEnumerable<Token> GetTokens(ParseNode tree) =>
        tree.InOrderTraverse().OfType<Token>();

    private Token.Builder AddIndentation(Token.Builder newToken)
    {
        this.AddIndentation();
        return newToken;
    }

    private Token.Builder RemoveIndentation(Token.Builder newToken)
    {
        this.RemoveIndentation();
        return newToken;
    }

    private void AddIndentation() => this.indentCount++;

    private void RemoveIndentation() => this.indentCount--;

    /// <summary>
    /// Formats the given <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="tree">The <see cref="ParseTree"/> to be formatted.</param>
    /// <returns>The formatted <paramref name="tree"/>.</returns>
    public ParseTree Format(ParseTree tree) => new(SourceText: tree.SourceText, Root: this.Format(tree.Root));

    /// <summary>
    /// Formats the given <see cref="ParseNode"/>.
    /// </summary>
    /// <param name="tree">The <see cref="ParseNode"/> to be formatted.</param>
    /// <returns>The formatted <paramref name="tree"/>.</returns>
    public ParseNode Format(ParseNode tree)
    {
        this.tokens = GetTokens(tree).GetEnumerator();
        this.lastToken = TokenType.EndOfInput;
        // We need to be one token ahead, because the next token affects the current one, so we must advance twice here
        if (!(this.tokens.MoveNext() && this.tokens.MoveNext())) return tree;
        this.nextToken = this.tokens.Current.Type;
        return this.Transform(tree, out _);
    }

    public override ParseNode.Decl TransformLabelDecl(ParseNode.Decl.Label node, out bool changed)
    {
        // Labels are indented one less than te rest of the code
        this.RemoveIndentation();
        var trIdentifier = this.TransformToken(node.Identifier, out var identifierChanged);
        var trColonToken = Token.Builder.From(node.ColonToken).SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(newlineTrivia).Build();
        this.AddIndentation();
        var colonTokenChanged = CheckTriviaEqual(trColonToken, node.ColonToken);
        // We need to advance to the next token by hand, because we don't call TransformToken
        this.lastToken = TokenType.Colon;
        if (this.tokens!.MoveNext()) this.nextToken = this.tokens!.Current.Type;
        else this.nextToken = TokenType.EndOfInput;
        changed = identifierChanged || colonTokenChanged;
        if (!changed) return node;
        return new Draco.Compiler.Internal.Syntax.ParseNode.Decl.Label(trIdentifier, trColonToken);
    }

    public override Token TransformToken(Token token, out bool changed)
    {
        if (token.Type == TokenType.EndOfInput)
        {
            changed = false;
            return token;
        }
        var resultToken = this.FormatToken(token);
        this.lastToken = resultToken.Type;
        this.tokens!.MoveNext();
        if (this.tokens.Current is null) this.nextToken = TokenType.EndOfInput;
        else this.nextToken = this.tokens.Current.Type;
        if (resultToken is not null)
        {
            // If the original token andthe new one have the same trivia, we set  changed to false, so the entire subtree is not recalculated
            changed = !CheckTriviaEqual(resultToken, token);
            return resultToken;
        }
        changed = false;
        return token;
    }

    private Token FormatToken(Token token)
    {
        var newToken = Token.Builder.From(token);

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
            TokenType.SlashAssign or TokenType.Star or TokenType.StarAssign =>
            newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia),

            // Tokens that statement can start with, which means they need to have indentation before them
            // Note: TokenType.Identifier is handeled separately, base on tokens that are before or after the Identifier
            TokenType.KeywordVal or TokenType.KeywordVar or TokenType.KeywordFunc or
            TokenType.KeywordReturn or TokenType.KeywordGoto or TokenType.KeywordWhile =>
            newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(oneSpaceTrivia),

            TokenType.ParenOpen => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(noSpaceTrivia),

            TokenType.ParenClose => this.nextToken switch
            {
                TokenType.ParenClose or TokenType.Semicolon => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(noSpaceTrivia),
                _ => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia)
            },

            TokenType.Semicolon => this.nextToken switch
            {
                TokenType.KeywordElse => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia),
                _ => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(newlineTrivia)
            },

            TokenType.CurlyOpen => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyOpen or TokenType.CurlyClose or
                TokenType.EndOfInput or TokenType.Colon =>
                this.AddIndentation(newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(newlineTrivia)),
                _ => this.AddIndentation(newToken).SetTrailingTrivia(newlineTrivia)
            },

            TokenType.CurlyClose => this.nextToken switch
            {
                TokenType.Semicolon => this.RemoveIndentation(newToken).SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(noSpaceTrivia),
                _ => this.RemoveIndentation(newToken).SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(newlineTrivia)
            },

            // If and else keywords are formatted diferently when they are used as expressions and when they are used as statements
            TokenType.KeywordIf => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose or TokenType.CurlyOpen or TokenType.Colon =>
                newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(oneSpaceTrivia),
                _ => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia)
            },

            TokenType.KeywordElse => this.lastToken switch
            {
                TokenType.CurlyClose or TokenType.Colon =>
                newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(oneSpaceTrivia),
                _ => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia)
            },

            // Identifier is handeled based on the context in which it is used
            TokenType.Identifier => (this.lastToken, this.nextToken) switch
            {
                { lastToken: TokenType.KeywordVal or TokenType.KeywordVar, nextToken: TokenType.Colon } =>
                newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(noSpaceTrivia),

                {
                    lastToken: TokenType.KeywordFrom or TokenType.KeywordVal or TokenType.KeywordVar or TokenType.Colon,
                    nextToken: TokenType.Semicolon or TokenType.Assign or TokenType.PlusAssign or
                    TokenType.MinusAssign or TokenType.Slash or TokenType.StarAssign
                } => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia),

                {
                    lastToken: TokenType.Semicolon or TokenType.CurlyOpen or
                    TokenType.CurlyClose or TokenType.EndOfInput or TokenType.Colon,
                    nextToken: TokenType.Assign or TokenType.PlusAssign or
                    TokenType.MinusAssign or TokenType.Slash or TokenType.StarAssign
                } => newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(oneSpaceTrivia),

                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen or TokenType.CurlyClose or TokenType.EndOfInput or TokenType.Colon } =>
                newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(noSpaceTrivia),

                { nextToken: TokenType.Semicolon or TokenType.ParenOpen or TokenType.ParenClose } =>
                newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(noSpaceTrivia),
                _ => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia)
            },

            // Literals are handeled based on the context in which they are used
            // Note: TokenType.MultiLineStringEnd is handeled separately, beacause we can't alter its leading trivia
            TokenType.LiteralInteger or TokenType.LiteralFloat or TokenType.KeywordFalse or TokenType.KeywordTrue or TokenType.LiteralCharacter or TokenType.LineStringEnd => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenType.Semicolon or TokenType.ParenClose } => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(noSpaceTrivia),

                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen, nextToken: TokenType.CurlyClose } =>
                newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(newlineTrivia),

                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen } =>
                newToken.SetLeadingTrivia(CreateTrivia(TriviaType.Whitespace, this.Indentation)).SetTrailingTrivia(oneSpaceTrivia),
                _ => newToken.SetLeadingTrivia(noSpaceTrivia).SetTrailingTrivia(oneSpaceTrivia)
            },

            TokenType.MultiLineStringEnd => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenType.Semicolon or TokenType.ParenClose } => newToken.SetTrailingTrivia(noSpaceTrivia),
                _ => newToken.SetTrailingTrivia(oneSpaceTrivia)
            },
            _ => newToken
        };
        return newToken.Build();
    }

    private static bool CheckTriviaEqual(Token tok1, Token tok2)
    {
        if (tok1.TrailingTrivia.Length != tok2.TrailingTrivia.Length) return false;
        for (var i = 0; i < tok1.TrailingTrivia.Length; i++)
        {
            if (tok1.TrailingTrivia[i].Text != tok2.TrailingTrivia[i].Text) return false;
        }

        if (tok1.LeadingTrivia.Length != tok2.LeadingTrivia.Length) return false;
        for (var i = 0; i < tok1.LeadingTrivia.Length; i++)
        {
            if (tok1.LeadingTrivia[i].Text != tok2.LeadingTrivia[i].Text) return false;
        }
        return true;
    }

    private static ImmutableArray<Trivia> CreateTrivia(TriviaType type, string text) =>
        ImmutableArray.Create(Trivia.From(type, text));
}
