using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.Syntax.ParseNode;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A source of <see cref="Token"/>s.
/// </summary>
internal interface ITokenSource
{
    /// <summary>
    /// Peeks ahead <paramref name="offset"/> of tokens in the source without consuming it.
    /// If the source is past the end, it should return a <see cref="Token"/> with type
    /// <see cref="TokenType.EndOfInput"/>.
    /// </summary>
    /// <param name="offset">The offset from the current source position.</param>
    /// <returns>The <see cref="Token"/> that is <paramref name="offset"/> amount of tokens ahead.</returns>
    public Token Peek(int offset = 0);

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
    private sealed class LexerTokenSource : ITokenSource
    {
        private readonly Lexer lexer;
        private readonly RingBuffer<Token> lookahead = new();

        public LexerTokenSource(Lexer lexer)
        {
            this.lexer = lexer;
        }

        public Token Peek(int offset = 0)
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
    /// Constructs a new <see cref="ITokenSource"/> that reads tokens from <paramref name="lexer"/>.
    /// </summary>
    /// <param name="lexer">The <see cref="Lexer"/> to read <see cref="Token"/>s from.</param>
    /// <returns>The constructed <see cref="ITokenSource"/> that reads from <paramref name="lexer"/>.</returns>
    public static ITokenSource From(Lexer lexer) => new LexerTokenSource(lexer);
}
