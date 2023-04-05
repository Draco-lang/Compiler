using System;
using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A source of <see cref="SyntaxToken"/>s.
/// </summary>
internal interface ITokenSource
{
    /// <summary>
    /// Peeks ahead <paramref name="offset"/> of tokens in the source without consuming it.
    /// If the source is past the end, it should return a <see cref="SyntaxToken"/> with kind
    /// <see cref="TokenKind.EndOfInput"/>.
    /// </summary>
    /// <param name="offset">The offset from the current source position.</param>
    /// <returns>The <see cref="SyntaxToken"/> that is <paramref name="offset"/> amount of tokens ahead.</returns>
    public SyntaxToken Peek(int offset = 0);

    /// <summary>
    /// Advances in the source <paramref name="amount"/> amount of tokens.
    /// </summary>
    /// <param name="amount">The amount of tokens to advance.</param>
    public void Advance(int amount = 1);
}

/// <summary>
/// Factory functions for constructing <see cref="ITokenSource"/>s.
/// </summary>
internal static class TokenSource
{
    private sealed class MemoryTokenSource : ITokenSource
    {
        private readonly ReadOnlyMemory<SyntaxToken> tokens;
        private int index;

        public MemoryTokenSource(ReadOnlyMemory<SyntaxToken> tokens)
        {
            this.tokens = tokens;
        }

        public SyntaxToken Peek(int offset = 0) => this.tokens.Span[this.index + offset];

        public void Advance(int amount = 1) => this.index += amount;
    }

    private sealed class EnumerableTokenSource : ITokenSource
    {
        private readonly IEnumerator<SyntaxToken> tokens;
        private readonly RingBuffer<SyntaxToken> lookahead = new();

        public EnumerableTokenSource(IEnumerable<SyntaxToken> tokens)
        {
            this.tokens = tokens.GetEnumerator();
        }

        public SyntaxToken Peek(int offset = 0)
        {
            while (offset >= this.lookahead.Count)
            {
                if (!this.tokens.MoveNext()) return SyntaxToken.From(TokenKind.EndOfInput);
                this.lookahead.AddBack(this.tokens.Current);
            }
            return this.lookahead[offset];
        }

        public void Advance(int amount = 1)
        {
            this.Peek();
            this.lookahead.RemoveFront();
        }
    }

    private sealed class LexerTokenSource : ITokenSource
    {
        private readonly Lexer lexer;
        private readonly RingBuffer<SyntaxToken> lookahead = new();

        public LexerTokenSource(Lexer lexer)
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

    /// <summary>
    /// Constructs a new <see cref="ITokenSource"/> that reads tokens from a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <param name="memory">The memory to read <see cref="SyntaxToken"/>s from.</param>
    /// <returns>The constructed <see cref="ITokenSource"/> that reads from <paramref name="memory"/>.</returns>
    public static ITokenSource From(ReadOnlyMemory<SyntaxToken> memory) => new MemoryTokenSource(memory);

    /// <summary>
    /// Constructs a new <see cref="ITokenSource"/> that reads tokens from a generic token sequence.
    /// </summary>
    /// <param name="tokens">The sequence to read from.</param>
    /// <returns>The constructed <see cref="ITokenSource"/> that reads tokens from <paramref name="tokens">.</returns>
    public static ITokenSource From(IEnumerable<SyntaxToken> tokens) => new EnumerableTokenSource(tokens);

    /// <summary>
    /// Constructs a new <see cref="ITokenSource"/> that reads tokens from <paramref name="lexer"/>.
    /// </summary>
    /// <param name="lexer">The <see cref="Lexer"/> to read <see cref="SyntaxToken"/>s from.</param>
    /// <returns>The constructed <see cref="ITokenSource"/> that reads from <paramref name="lexer"/>.</returns>
    public static ITokenSource From(Lexer lexer) => new LexerTokenSource(lexer);
}
