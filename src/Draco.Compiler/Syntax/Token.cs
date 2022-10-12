using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

// Factory functions
internal partial interface IToken
{
    /// <summary>
    /// Constructs an <see cref="IToken"/> from <paramref name="tokenType"/>.
    /// </summary>
    /// <param name="tokenType">The type of the token.</param>
    /// <returns>The constructed <see cref="Basic"/>.</returns>
    public static Basic From(TokenType tokenType) => new(tokenType);

    /// <summary>
    /// Constructs an <see cref="IToken"/> from <paramref name="tokenType"/> and the corresponding
    /// <paramref name="text"/> it was lexed from.
    /// </summary>
    /// <param name="tokenType">The type of the token.</param>
    /// <param name="text">The text the token was lexed from.</param>
    /// <returns>The constructed <see cref="WithValue{string}"/>.</returns>
    public static WithValue<string> From(TokenType tokenType, string text) =>
        new(tokenType, text, text);

    /// <summary>
    /// Constructs an <see cref="IToken"/> from <paramref name="tokenType"/>, the corresponding
    /// <paramref name="text"/> it was lexed from and the associated <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The type of the associated value.</typeparam>
    /// <param name="tokenType">The type of the token.</param>
    /// <param name="text">The text the token was lexed from.</param>
    /// <param name="value">The associated value.</param>
    /// <returns>The constructed <see cref="WithValue{T}"/>.</returns>
    public static WithValue<T> From<T>(TokenType tokenType, string text, T value) =>
        new(tokenType, text, value);
}

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

    /// <summary>
    /// Adds trivia around this token. It's illegal to call this on tokens that already have trivia.
    /// Note, that this function returns a new <see cref="IToken"/>, does not modify the original.
    /// </summary>
    /// <param name="leadingTrivia">The leading trivia to attach.</param>
    /// <param name="trailingTrivia">The trailing trivia to attach.</param>
    /// <returns>A new <see cref="IToken"/> that has <paramref name="leadingTrivia"/> as
    /// leading trivia and <paramref name="trailingTrivia"/> as trailing trivia.</returns>
    IToken AddTrivia(ValueArray<IToken> leadingTrivia, ValueArray<IToken> trailingTrivia);
}

internal partial interface IToken
{
    /// <summary>
    /// Represents any <see cref="IToken"/> that has an associated value.
    /// </summary>
    public interface IWithValue : IToken
    {
        /// <summary>
        /// The associated value.
        /// </summary>
        public object? Value { get; }
    }

    /// <summary>
    /// Represents any <see cref="IToken"/> that has an associated value.
    /// </summary>
    /// <typeparam name="T">The type of the associated value.</typeparam>
    public interface IWithValue<T> : IWithValue
    {
        /// <summary>
        /// The associated value.
        /// </summary>
        public new T Value { get; }
    }
}

// Private utilities
internal partial interface IToken
{
    /// <summary>
    /// Implements stringification of <see cref="IToken"/>s.
    /// </summary>
    /// <param name="token">The token to stringify.</param>
    /// <returns>The user-friendly string representation of <paramref name="token"/>.</returns>
    private static string ToStringImpl(IToken token)
    {
        static string Unescape(string text) => text
            .Replace("\n", @"\n")
            .Replace("\r", @"\r")
            .Replace("\t", @"\t");

        var result = new StringBuilder();
        result.Append($"\"{Unescape(token.Text)}\": {token.Type}");
        if (token is IWithValue withVal)
        {
            var valueText = withVal.Value?.ToString() ?? "null";
            if (withVal.Value is not string || valueText != token.Text) result.Append($" [value={withVal.Value}]");
        }
        result.AppendLine();
        if (token.LeadingTrivia.Count > 0) result.AppendLine($"  leading trivia: {token.LeadingTrivia}");
        if (token.TrailingTrivia.Count > 0) result.AppendLine($"  trailing trivia: {token.TrailingTrivia}");
        return result.ToString().TrimEnd();
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

        /// <inheritdoc/>
        public IToken AddTrivia(ValueArray<IToken> leadingTrivia, ValueArray<IToken> trailingTrivia) =>
            new WithTrivia(this.Type, leadingTrivia, trailingTrivia);

        /// <inheritdoc/>
        public override string ToString() => ToStringImpl(this);
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
        object? IWithValue.Value => this.Value;

        /// <inheritdoc/>
        public int Width => this.Text.Length;

        /// <inheritdoc/>
        public ValueArray<IToken> LeadingTrivia => ValueArray<IToken>.Empty;

        /// <inheritdoc/>
        public ValueArray<IToken> TrailingTrivia => ValueArray<IToken>.Empty;

        /// <inheritdoc/>
        public IToken AddTrivia(ValueArray<IToken> leadingTrivia, ValueArray<IToken> trailingTrivia) =>
            new WithTriviaAndValue<T>(this.Type, this.Text, this.Value, leadingTrivia, trailingTrivia);

        /// <inheritdoc/>
        public override string ToString() => ToStringImpl(this);
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

        /// <inheritdoc/>
        public IToken AddTrivia(ValueArray<IToken> leadingTrivia, ValueArray<IToken> trailingTrivia) =>
            throw new InvalidOperationException("can not attach trivia to a token that already has trivia");

        /// <inheritdoc/>
        public override string ToString() => ToStringImpl(this);
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
        object? IWithValue.Value => this.Value;

        /// <inheritdoc/>
        public int Width { get; } = LeadingTrivia.Sum(t => t.Width)
                                  + Text.Length
                                  + TrailingTrivia.Sum(t => t.Width);

        /// <inheritdoc/>
        public IToken AddTrivia(ValueArray<IToken> leadingTrivia, ValueArray<IToken> trailingTrivia) =>
            throw new InvalidOperationException("can not attach trivia to a token that already has trivia");

        /// <inheritdoc/>
        public override string ToString() => ToStringImpl(this);
    }
}
