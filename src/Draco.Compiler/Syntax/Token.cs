using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Diagnostics;
using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Represents a piece of source code that has an associated category and can be considered atomic during parsing.
/// Stores surrounding trivia and lexical errors.
/// </summary>
internal sealed partial record class Token : ParseTree
{
    /// <summary>
    /// The <see cref="TokenType"/> of this <see cref="Token"/>.
    /// </summary>
    public TokenType Type { get; }

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
    public int Width { get; }

    /// <summary>
    /// The leading trivia for this <see cref="Token"/>.
    /// </summary>
    public ValueArray<Token> LeadingTrivia { get; }

    /// <summary>
    /// The trailing trivia for this <see cref="Token"/>.
    /// </summary>
    public ValueArray<Token> TrailingTrivia { get; }

    /// <summary>
    /// The <see cref="Diagnostic"/> messages attached to this <see cref="Token"/>.
    /// </summary>
    public override ValueArray<Diagnostic> Diagnostics { get; }

    private Token(
        TokenType type,
        string text,
        object? value,
        ValueArray<Token> leadingTrivia,
        ValueArray<Token> trailingTrivia,
        ValueArray<Diagnostic> diagnostics)
    {
        this.Type = type;
        this.Text = text;
        this.Value = value;
        this.Width = leadingTrivia.Sum(t => t.Width) + text.Length + trailingTrivia.Sum(t => t.Width);
        this.LeadingTrivia = leadingTrivia;
        this.TrailingTrivia = trailingTrivia;
        this.Diagnostics = diagnostics;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        static string Unescape(string text) => text
            .Replace("\n", @"\n")
            .Replace("\r", @"\r")
            .Replace("\t", @"\t");

        var result = new StringBuilder();
        result.Append($"\"{Unescape(this.Text)}\": {this.Type}");
        if (this.Value is not null)
        {
            var valueText = this.Value.ToString() ?? "null";
            if (this.Value is not string || valueText != this.Text) result.Append($" [value={this.Value}]");
        }
        result.AppendLine();
        if (this.LeadingTrivia.Count > 0) result.AppendLine($"  leading trivia: {this.LeadingTrivia}");
        if (this.TrailingTrivia.Count > 0) result.AppendLine($"  trailing trivia: {this.TrailingTrivia}");
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
        leadingTrivia: ValueArray<Token>.Empty,
        trailingTrivia: ValueArray<Token>.Empty,
        diagnostics: ValueArray<Diagnostic>.Empty);

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
        leadingTrivia: ValueArray<Token>.Empty,
        trailingTrivia: ValueArray<Token>.Empty,
        diagnostics: ValueArray<Diagnostic>.Empty);
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
        public ValueArray<Token>? LeadingTrivia { get; private set; }
        public ValueArray<Token>? TrailingTrivia { get; private set; }
        public ValueArray<Diagnostic>? Diagnostics { get; private set; }

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
            var leadingTriv = this.LeadingTrivia ?? ValueArray<Token>.Empty;
            var trailingTriv = this.TrailingTrivia ?? ValueArray<Token>.Empty;
            var diags = this.Diagnostics ?? ValueArray<Diagnostic>.Empty;
            return new(
                type: tt,
                text: text,
                value: this.Value,
                leadingTrivia: leadingTriv,
                trailingTrivia: trailingTriv,
                diagnostics: diags);
        }

        public Builder SetType(TokenType tokenType)
        {
            if (this.Type is not null) throw new InvalidOperationException("token type already set");
            this.Type = tokenType;
            return this;
        }

        public Builder SetText(string text)
        {
            if (this.Text is not null) throw new InvalidOperationException("text already set");
            this.Text = text;
            return this;
        }

        public Builder SetValue<T>(T value)
        {
            if (this.Value is not null) throw new InvalidOperationException("value already set");
            this.Value = value;
            return this;
        }

        public Builder SetLeadingTrivia(ValueArray<Token> trivia)
        {
            if (this.LeadingTrivia is not null) throw new InvalidOperationException("leading trivia already set");
            this.LeadingTrivia = trivia;
            return this;
        }

        public Builder SetTrailingTrivia(ValueArray<Token> trivia)
        {
            if (this.TrailingTrivia is not null) throw new InvalidOperationException("trailing trivia already set");
            this.TrailingTrivia = trivia;
            return this;
        }

        public Builder SetDiagnostics(ValueArray<Diagnostic> diagnostics)
        {
            if (this.Diagnostics is not null) throw new InvalidOperationException("diagnostics already set");
            this.Diagnostics = diagnostics;
            return this;
        }
    }
}
