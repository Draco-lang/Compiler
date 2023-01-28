using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax;

internal sealed partial class SyntaxToken
{
    /// <summary>
    /// Builder for a <see cref="SyntaxToken"/>.
    /// </summary>
    public sealed class Builder
    {
        /// <summary>
        /// Creates a builder from an already constructed <see cref="SyntaxToken"/>.
        /// </summary>
        /// <param name="token">The <see cref="SyntaxToken"/> to fill up the builder data with.</param>
        /// <returns>The constructed <see cref="Builder"/> with all data from <paramref name="token"/>.</returns>
        public static Builder From(SyntaxToken token) => new()
        {
            Type = token.Type,
            Text = token.Text,
            Value = token.Value,
            LeadingTrivia = token.LeadingTrivia.ToBuilder(),
            TrailingTrivia = token.TrailingTrivia.ToBuilder(),
        };

        /// <summary>
        /// The <see cref="TokenType"/> of the <see cref="SyntaxToken"/> being built.
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// The text the <see cref="SyntaxToken"/> is constructed from.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// The value the <see cref="SyntaxToken"/> represents.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// The <see cref="SyntaxTrivia"/> before the token.
        /// </summary>
        public SyntaxList<SyntaxTrivia>.Builder LeadingTrivia { get; set; } = new();

        /// <summary>
        /// The <see cref="SyntaxTrivia"/> after the token.
        /// </summary>
        public SyntaxList<SyntaxTrivia>.Builder TrailingTrivia { get; set; } = new();

        /// <summary>
        /// Builds a <see cref="SyntaxToken"/> from the data written into the builder.
        /// </summary>
        /// <returns>The built <see cref="SyntaxToken"/>.</returns>
        public SyntaxToken Build() => new(
            type: this.Type,
            text: this.Text ?? this.Type.GetTokenText(),
            value: this.Value,
            leadingTrivia: this.LeadingTrivia.ToSyntaxList(),
            trailingTrivia: this.TrailingTrivia.ToSyntaxList());

        /// <summary>
        /// Clears all data from this builder.
        /// </summary>
        public void Clear()
        {
            this.Type = TokenType.Unknown;
            this.Text = null;
            this.Value = null;
            this.LeadingTrivia.Clear();
            this.TrailingTrivia.Clear();
        }

        /// <summary>
        /// Sets the <see cref="Type"/> for the token to be built.
        /// </summary>
        /// <param name="type">The <see cref="TokenType"/> to set.</param>
        /// <returns>The <see cref="Builder"/> instance the method was called on.</returns>
        public Builder SetType(TokenType type)
        {
            this.Type = type;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Text"/> for the token to be built.
        /// </summary>
        /// <param name="text">The text to set.</param>
        /// <returns>The <see cref="Builder"/> instance the method was called on.</returns>
        public Builder SetText(string text)
        {
            this.Text = text;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Value"/> for the token to be built.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The <see cref="Builder"/> instance the method was called on.</returns>
        public Builder SetValue(object? value)
        {
            this.Value = value;
            return this;
        }
    }
}
