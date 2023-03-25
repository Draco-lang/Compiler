using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

/// <summary>
/// A single TypeScript token.
/// </summary>
/// <param name="Type">The type of the token.</param>
/// <param name="Text">The token text.</param>
/// <param name="LeadingComment">The comment before the token.</param>
internal sealed record class Token(
    TokenKind Type,
    string Text,
    string? LeadingComment);
