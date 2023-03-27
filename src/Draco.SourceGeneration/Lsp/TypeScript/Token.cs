using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.TypeScript;

/// <summary>
/// A single TypeScript token.
/// </summary>
/// <param name="Kind">The kind of this token.</param>
/// <param name="Text">The text this token was produced from.</param>
/// <param name="LeadingComment">The leading comment before this token.</param>
internal sealed record class Token(
    TokenKind Kind,
    string Text,
    string? LeadingComment);
