using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using Draco.RedGreenTree.Attributes;
using TokenType = Draco.Compiler.Api.Syntax.TokenType;

namespace Draco.Compiler.Internal.Syntax;

internal abstract partial record class ParseNode
{
    /// <summary>
    /// Represents a piece of source code that has an associated category and can be considered atomic during parsing.
    /// Stores surrounding trivia and lexical errors.
    /// </summary>
    [Ignore(IgnoreFlags.SyntaxFactoryConstruct)]
    internal sealed partial record class Token : ParseNode
    {
        /// <summary>
        /// The <see cref="TokenType"/> of this <see cref="Token"/>.
        /// </summary>
        internal TokenType Type { get; }

        /// <summary>
        /// The textual representation of this <see cref="Token"/>.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// An optional associated value to this <see cref="Token"/>.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// The <see cref="Value"/> in string representation.
        /// </summary>
        public string? ValueText => this.Value?.ToString();

        /// <summary>
        /// The width of this <see cref="Token"/> in characters.
        /// </summary>
        public override int Width { get; }

        /// <summary>
        /// The leading trivia for this <see cref="Token"/>.
        /// </summary>
        public ImmutableArray<Trivia> LeadingTrivia { get; }

        /// <summary>
        /// The trailing trivia for this <see cref="Token"/>.
        /// </summary>
        public ImmutableArray<Trivia> TrailingTrivia { get; }

        /// <summary>
        /// The <see cref="Diagnostic"/> messages attached to this <see cref="Token"/>.
        /// </summary>
        internal override ImmutableArray<Diagnostic> Diagnostics { get; }

        public override IEnumerable<ParseNode> Children => Enumerable.Empty<ParseNode>();

        private Token(
            TokenType type,
            string text,
            object? value,
            ImmutableArray<Trivia> leadingTrivia,
            ImmutableArray<Trivia> trailingTrivia,
            ImmutableArray<Diagnostic> diagnostics)
        {
            this.Type = type;
            this.Text = text;
            this.Value = value;
            this.Width = leadingTrivia.Sum(t => t.Width) + text.Length + trailingTrivia.Sum(t => t.Width);
            this.LeadingTrivia = leadingTrivia;
            this.TrailingTrivia = trailingTrivia;
            this.Diagnostics = diagnostics;
        }

        /// <summary>
        /// Appends a <see cref="Diagnostic"/> to this <see cref="Token"/>.
        /// </summary>
        /// <param name="diagnostic">The <see cref="Diagnostic"/> to append.</param>
        /// <returns>A new <see cref="Token"/> with all information as this one plus the appended
        /// <paramref name="diagnostic"/>.</returns>
        public Token AddDiagnostic(Diagnostic diagnostic) => new(
            type: this.Type,
            text: this.Text,
            value: this.Value,
            leadingTrivia: this.LeadingTrivia,
            trailingTrivia: this.TrailingTrivia,
            diagnostics: this.Diagnostics.Append(diagnostic).ToImmutableArray());

        /// <inheritdoc/>
        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append($"\"{StringUtils.Unescape(this.Text)}\": {this.Type}");
            if (this.Value is not null)
            {
                var valueText = this.Value.ToString() ?? "null";
                if (this.Value is not string || valueText != this.Text) result.Append($" [value={this.Value}]");
            }
            result.AppendLine();
            if (this.LeadingTrivia.Length > 0) result.AppendLine($"  leading trivia: {this.LeadingTrivia}");
            if (this.TrailingTrivia.Length > 0) result.AppendLine($"  trailing trivia: {this.TrailingTrivia}");
            return result.ToString().TrimEnd();
        }
    }

    // Factory functions
    internal partial record class Token
    {
        /// <summary>
        /// Constructs a <see cref="Token"/> from <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="TokenType"/> of the token to be constructed.</param>
        /// <returns>The constructed <see cref="Token"/> with type <paramref name="type"/>.</returns>
        public static Token From(TokenType type) => new(
            type: type,
            text: type.GetTokenText(),
            value: null,
            leadingTrivia: ImmutableArray<Trivia>.Empty,
            trailingTrivia: ImmutableArray<Trivia>.Empty,
            diagnostics: ImmutableArray<Diagnostic>.Empty);

        /// <summary>
        /// Constructs a <see cref="Token"/> from <paramref name="type"/> and <paramref name="text"/>.
        /// </summary>
        /// <param name="type">The <see cref="TokenType"/> of the token to be constructed.</param>
        /// <param name="text">The text the <see cref="Token"/> is produced from.</param>
        /// <returns>The constructed <see cref="Token"/> with type <paramref name="type"/>
        /// and text <paramref name="text"/>.</returns>
        public static Token From(TokenType type, string text) => new(
            type: type,
            text: text,
            value: null,
            leadingTrivia: ImmutableArray<Trivia>.Empty,
            trailingTrivia: ImmutableArray<Trivia>.Empty,
            diagnostics: ImmutableArray<Diagnostic>.Empty);

        /// <summary>
        /// Constructs a <see cref="Token"/> from <paramref name="type"/>, <paramref name="text"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="type">The <see cref="TokenType"/> of the token to be constructed.</param>
        /// <param name="text">The text the <see cref="Token"/> is produced from.</param>
        /// <param name="value">The value associated with the <see cref="Token"/>.</param>
        /// <returns>The constructed <see cref="Token"/> with type <paramref name="type"/>,
        /// text <paramref name="text"/> and value <paramref name="value"/>.</returns>
        public static Token From(TokenType type, string text, object? value) => new(
            type: type,
            text: text,
            value: value,
            leadingTrivia: ImmutableArray<Trivia>.Empty,
            trailingTrivia: ImmutableArray<Trivia>.Empty,
            diagnostics: ImmutableArray<Diagnostic>.Empty);

        /// <summary>
        /// Constructs a <see cref="Token"/> from <paramref name="type"/>, <paramref name="text"/> and
        /// <paramref name="diagnostics"/>.
        /// </summary>
        /// <param name="type">The <see cref="TokenType"/> of the token to be constructed.</param>
        /// <param name="text">The text the <see cref="Token"/> is produced from.</param>
        /// <param name="diagnostics">The list of <see cref="Diagnostic"/> messages associated with this
        /// <see cref="Token"/>.</param>
        /// <returns>The constructed <see cref="Token"/> with type <paramref name="type"/>,
        /// text <paramref name="text"/> and diagnostic list <paramref name="diagnostics"/>.</returns>
        public static Token From(TokenType type, string text, ImmutableArray<Diagnostic> diagnostics) => new(
            type: type,
            text: text,
            value: null,
            leadingTrivia: ImmutableArray<Trivia>.Empty,
            trailingTrivia: ImmutableArray<Trivia>.Empty,
            diagnostics: diagnostics);
    }

    // Builder type
    internal partial record class Token
    {
        /// <summary>
        /// A builder type for <see cref="Token"/>.
        /// </summary>
        public sealed class Builder
        {
            public TokenType? Type { get; private set; }
            public string? Text { get; private set; }
            public object? Value { get; private set; }
            public ImmutableArray<Trivia>? LeadingTrivia { get; private set; }
            public ImmutableArray<Trivia>? TrailingTrivia { get; private set; }
            public ImmutableArray<Diagnostic>? Diagnostics { get; private set; }

            /// <summary>
            /// Clears this builder.
            /// </summary>
            public void Clear()
            {
                this.Type = null;
                this.Text = null;
                this.Value = null;
                this.LeadingTrivia = null;
                this.TrailingTrivia = null;
                this.Diagnostics = null;
            }

            /// <summary>
            /// Builds a <see cref="Token"/> from the set data.
            /// Can throw <see cref="InvalidOperationException"/> on invalid configuration.
            /// </summary>
            /// <returns>The constructed <see cref="Token"/>.</returns>
            public Token Build()
            {
                var tt = this.Type ?? throw new InvalidOperationException("specifying token type is required");
                var text = this.Text ?? tt.GetTokenText();
                var leadingTriv = this.LeadingTrivia ?? ImmutableArray<Trivia>.Empty;
                var trailingTriv = this.TrailingTrivia ?? ImmutableArray<Trivia>.Empty;
                var diags = this.Diagnostics ?? ImmutableArray<Diagnostic>.Empty;
                return new(
                    type: tt,
                    text: text,
                    value: this.Value,
                    leadingTrivia: leadingTriv,
                    trailingTrivia: trailingTriv,
                    diagnostics: diags);
            }

            public static Builder From(Token token) => new Builder()
                .SetType(token.Type)
                .SetText(token.Text)
                .SetValue(token.Value)
                .SetLeadingTrivia(token.LeadingTrivia)
                .SetTrailingTrivia(token.TrailingTrivia)
                .SetDiagnostics(token.Diagnostics);

            public Builder SetType(TokenType tokenType)
            {
                this.Type = tokenType;
                return this;
            }

            public Builder SetText(string text)
            {
                this.Text = text;
                return this;
            }

            public Builder SetValue<T>(T value)
            {
                this.Value = value;
                return this;
            }

            public Builder SetLeadingTrivia(ImmutableArray<Trivia> trivia)
            {
                this.LeadingTrivia = trivia;
                return this;
            }

            public Builder SetTrailingTrivia(ImmutableArray<Trivia> trivia)
            {
                this.TrailingTrivia = trivia;
                return this;
            }

            public Builder SetDiagnostics(ImmutableArray<Diagnostic> diagnostics)
            {
                this.Diagnostics = diagnostics;
                return this;
            }
        }
    }
}
