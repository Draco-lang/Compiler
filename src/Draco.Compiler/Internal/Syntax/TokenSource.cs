using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A source of <see cref="SyntaxSyntaxToken"/>s.
/// </summary>
internal interface ISyntaxTokenSource
{
    /// <summary>
    /// Peeks ahead <paramref name="offset"/> of tokens in the source without consuming it.
    /// If the source is past the end, it should return a <see cref="SyntaxSyntaxToken"/> with kind
    /// <see cref="SyntaxTokenKind.EndOfInput"/>.
    /// </summary>
    /// <param name="offset">The offset from the current source position.</param>
    /// <returns>The <see cref="SyntaxSyntaxToken"/> that is <paramref name="offset"/> amount of tokens ahead.</returns>
    public SyntaxToken Peek(int offset = 0);

    /// <summary>
    /// Advances in the source <paramref name="amount"/> amount of tokens.
    /// </summary>
    /// <param name="amount">The amount of tokens to advance.</param>
    public void Advance(int amount = 1);
}

/// <summary>
/// Factory functions for constructing <see cref="ISyntaxTokenSource"/>s.
/// </summary>
internal static class SyntaxTokenSource
{
    private sealed class LexerSyntaxTokenSource : ISyntaxTokenSource
    {
        private readonly Lexer lexer;
        private readonly RingBuffer<SyntaxToken> lookahead = new();

        public LexerSyntaxTokenSource(Lexer lexer)
        {
            this.lexer = lexer;
        }

        public SyntaxToken Peek(int offset = 0)
        {
            while (offset >= this.lookahead.Count) this.lookahead.AddBack(this.lexer.Lex());
            return this.lookahead[offset];
        }

        public void Advance(int amount = 1)
        {
            this.Peek();
            this.lookahead.RemoveFront();
        }
    }

    private sealed class MemorySyntaxTokenSource : ISyntaxTokenSource
    {
        private readonly IEnumerator<SyntaxToken> tokens;
        private readonly RingBuffer<SyntaxToken> lookahead = new();

        public MemorySyntaxTokenSource(IEnumerable<SyntaxToken> tokens)
        {
            this.tokens = tokens.GetEnumerator();
        }

        public SyntaxToken Peek(int offset = 0)
        {
            while (offset >= this.lookahead.Count)
            {
                SyntaxToken? token = null;
                if (this.tokens.MoveNext()) token = this.tokens.Current;
                else token = SyntaxToken.From(TokenKind.EndOfInput);
                this.lookahead.AddBack(token);
            }
            return this.lookahead[offset];
        }

        public void Advance(int amount = 1)
        {
            this.Peek();
            this.lookahead.RemoveFront();
        }
    }

    /// <summary>
    /// Constructs a new <see cref="ISyntaxTokenSource"/> that reads tokens from <paramref name="lexer"/>.
    /// </summary>
    /// <param name="lexer">The <see cref="Lexer"/> to read <see cref="SyntaxSyntaxToken"/>s from.</param>
    /// <returns>The constructed <see cref="ISyntaxTokenSource"/> that reads from <paramref name="lexer"/>.</returns>
    public static ISyntaxTokenSource From(Lexer lexer) => new LexerSyntaxTokenSource(lexer);

    /// <summary>
    /// Constructs a new <see cref="ISyntaxTokenSource"/> that reads tokens from a generic token sequence.
    /// </summary>
    /// <param name="tokens">The sequence to read from.</param>
    /// <returns>The constructed <see cref="ISyntaxTokenSource"/> that reads tokens from <paramref name="tokens">.</returns>
    public static ISyntaxTokenSource From(IEnumerable<SyntaxToken> tokens) => new MemorySyntaxTokenSource(tokens);
}
