using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// A significant atomic piece of syntax in terms of formatting.
///
/// This means general token and comments from trivia.
/// Newlines and whitespace are not considered atoms.
/// </summary>
/// <param name="TokenKind">The token kind, in case it's a token.</param>
/// <param name="TriviaKind>">The trivia kind, in case it's trivia.</param>
/// <param name="Text">The text the atom represents.</param>
internal readonly record struct FormatAtom(
    TokenKind? TokenKind,
    TriviaKind? TriviaKind,
    string? Text);
