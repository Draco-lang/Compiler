using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.TypeScript;

/// <summary>
/// A single TypeScript token.
/// </summary>
internal sealed class Token
{
    /// <summary>
    /// The kind of this token.
    /// </summary>
    public TokenKind Kind { get; }

    /// <summary>
    /// The text this token was produced from.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The leading comment before this token.
    /// </summary>
    public string? LeadingComment { get; }

    public Token(TokenKind kind, string text, string? leadingComment)
    {
        this.Kind = kind;
        this.Text = text;
        this.LeadingComment = leadingComment;
    }
}
