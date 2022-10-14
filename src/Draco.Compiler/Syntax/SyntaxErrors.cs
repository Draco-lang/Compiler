using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Holds constants for syntax errors.
/// </summary>
internal static class SyntaxErrors
{
    /// <summary>
    /// An illegal character appears in a character literal.
    /// </summary>
    public static readonly string IllegalCharacterLiteral = "illegal character literal (code: {0})";

    /// <summary>
    /// The character literal closing quote was missing.
    /// </summary>
    public static readonly string UnclosedCharacterLiteral = "unclosed character literal";

    /// <summary>
    /// A \u{...} construct was left empty.
    /// </summary>
    public static readonly string ZeroLengthUnicodeCodepoint = "zero length unicode codepoint";

    /// <summary>
    /// A \u{...} construct was left unclosed.
    /// </summary>
    public static readonly string UnclosedUnicodeCodepoint = "unclosed unicode codepoint escape sequence";

    /// <summary>
    /// An illegal escape character after '\'.
    /// </summary>
    public static readonly string IllegalEscapeCharacter = "illegal escape character '{0}'";
}
