using System;
using System.Collections.Generic;
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
        /// Builds a <see cref="SyntaxToken"/> from the data written into the builder.
        /// </summary>
        /// <returns>The built <see cref="SyntaxToken"/>.</returns>
        public SyntaxToken Build()
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears all data from this builder.
        /// </summary>
        public void Clear()
        {
            // TODO
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
