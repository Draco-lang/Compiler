using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Diagnostics;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Holds constants for syntax errors.
/// </summary>
internal static class SyntaxErrors
{
    /// <summary>
    /// An illegal character appears in a character literal.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalCharacterLiteral = DiagnosticTemplate.Create(
        title: "illegal character literal",
        severity: DiagnosticSeverity.Error,
        format: "illegal character literal (code: {0})");

    /// <summary>
    /// The character literal closing quote was missing.
    /// </summary>
    public static readonly DiagnosticTemplate UnclosedCharacterLiteral = DiagnosticTemplate.Create(
        title: "unclosed character literal",
        severity: DiagnosticSeverity.Error,
        format: "unclosed character literal");

    /// <summary>
    /// A \u{...} construct was left empty.
    /// </summary>
    public static readonly DiagnosticTemplate ZeroLengthUnicodeCodepoint = DiagnosticTemplate.Create(
        title: "zero length unicode codepoint",
        severity: DiagnosticSeverity.Error,
        format: "zero length unicode codepoint");

    /// <summary>
    /// A \u{...} construct was left unclosed.
    /// </summary>
    public static readonly DiagnosticTemplate UnclosedUnicodeCodepoint = DiagnosticTemplate.Create(
        title: "unclosed unicode codepoint escape sequence",
        severity: DiagnosticSeverity.Error,
        format: "unclosed character literal");

    /// <summary>
    /// An illegal escape character after '\'.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalEscapeCharacter = DiagnosticTemplate.Create(
        title: "illegal escape character '{0}'",
        severity: DiagnosticSeverity.Error,
        format: "unclosed character literal");
}
