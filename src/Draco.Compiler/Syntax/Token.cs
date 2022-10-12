using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Interface for all different kinds of tokens.
/// </summary>
internal partial interface IToken
{
    /// <summary>
    /// The <see cref="TokenType"/> of this <see cref="IToken"/>.
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// The textual representation of this <see cref="IToken"/>.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The width of this <see cref="IToken"/> in characters.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The leading trivia for this <see cref="IToken"/>.
    /// </summary>
    public ValueArray<IToken> LeadingTrivia { get; }

    /// <summary>
    /// The trailing trivia for this <see cref="IToken"/>.
    /// </summary>
    public ValueArray<IToken> TrailingTrivia { get; }
}

internal partial interface IToken
{
    /// <summary>
    /// Represents any <see cref="IToken"/> that has an associated value.
    /// </summary>
    /// <typeparam name="T">The type of the associated value.</typeparam>
    public interface IWithValue<T> : IToken
    {
        /// <summary>
        /// The associated value.
        /// </summary>
        public T Value { get; }
    }
}

internal partial interface IToken
{
    /// <summary>
    /// The most basic kind of token with only a <see cref="TokenType"/>.
    /// </summary>
    /// <param name="Type">The type of this token.</param>
    public sealed record class Basic(TokenType Type) : IToken
    {
        /// <inheritdoc/>
        public string Text => this.Type.GetTokenText();

        /// <inheritdoc/>
        public int Width => this.Text.Length;

        /// <inheritdoc/>
        public ValueArray<IToken> LeadingTrivia => ValueArray<IToken>.Empty;

        /// <inheritdoc/>
        public ValueArray<IToken> TrailingTrivia => ValueArray<IToken>.Empty;
    }
}

internal partial interface IToken
{
    /// <summary>
    /// A token with a lexed value.
    /// </summary>
    /// <typeparam name="T">The type of the lexed value.</typeparam>
    /// <param name="Type">The type of this token.</param>
    /// <param name="Text">The text the token was lexed from.</param>
    /// <param name="Value">The interpreted value of the token.</param>
    public sealed record class WithValue<T>(TokenType Type, string Text, T Value) : IWithValue<T>
    {
        /// <inheritdoc/>
        public int Width => this.Text.Length;

        /// <inheritdoc/>
        public ValueArray<IToken> LeadingTrivia => ValueArray<IToken>.Empty;

        /// <inheritdoc/>
        public ValueArray<IToken> TrailingTrivia => ValueArray<IToken>.Empty;
    }
}

internal partial interface IToken
{
    /// <summary>
    /// A basic token with <see cref="TokenType"/> and trivia.
    /// </summary>
    /// <param name="Type">The type of this token.</param>
    /// <param name="LeadingTrivia">The leading trivia of this token.</param>
    /// <param name="TrailingTrivia">The trailing trivia of this token.</param>
    public sealed record class WithTrivia(
        TokenType Type,
        ValueArray<IToken> LeadingTrivia,
        ValueArray<IToken> TrailingTrivia) : IToken
    {
        /// <inheritdoc/>
        public string Text => this.Type.GetTokenText();

        /// <inheritdoc/>
        public int Width { get; } = LeadingTrivia.Sum(t => t.Width)
                                  + Type.GetTokenText().Length
                                  + TrailingTrivia.Sum(t => t.Width);
    }
}

internal partial interface IToken
{
    /// <summary>
    /// A token with associated value and trivia.
    /// </summary>
    /// <typeparam name="T">The type of the lexed value.</typeparam>
    /// <param name="Type">The type of this token.</param>
    /// <param name="Text">The text the token was lexed from.</param>
    /// <param name="Value">The interpreted value of the token.</param>
    /// <param name="LeadingTrivia">The leading trivia of this token.</param>
    /// <param name="TrailingTrivia">The trailing trivia of this token.</param>
    public sealed record class WithTriviaAndValue<T>(
        TokenType Type,
        string Text,
        T Value,
        ValueArray<IToken> LeadingTrivia,
        ValueArray<IToken> TrailingTrivia) : IWithValue<T>
    {
        /// <inheritdoc/>
        public int Width { get; } = LeadingTrivia.Sum(t => t.Width)
                                  + Text.Length
                                  + TrailingTrivia.Sum(t => t.Width);
    }
}
