using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Holds constants for syntax errors.
/// </summary>
internal static class SyntaxErrors
{
    private static string Code(int index) => DiagnosticTemplate.CreateDiagnosticCode(DiagnosticCategory.Syntax, index);

    /// <summary>
    /// An illegal character appears in a character literal.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalCharacterLiteral = DiagnosticTemplate.Create(
        title: "illegal character literal",
        severity: DiagnosticSeverity.Error,
        format: "illegal character literal (code: {0})",
        code: Code(1));

    /// <summary>
    /// The character literal closing quote was missing.
    /// </summary>
    public static readonly DiagnosticTemplate UnclosedCharacterLiteral = DiagnosticTemplate.Create(
        title: "unclosed character literal",
        severity: DiagnosticSeverity.Error,
        format: "unclosed character literal",
        code: Code(2));

    /// <summary>
    /// A \u{...} construct was left empty.
    /// </summary>
    public static readonly DiagnosticTemplate ZeroLengthUnicodeCodepoint = DiagnosticTemplate.Create(
        title: "zero length unicode codepoint",
        severity: DiagnosticSeverity.Error,
        format: "zero length unicode codepoint",
        code: Code(3));

    /// <summary>
    /// A \u{...} construct was left unclosed.
    /// </summary>
    public static readonly DiagnosticTemplate UnclosedUnicodeCodepoint = DiagnosticTemplate.Create(
        title: "unclosed unicode codepoint escape sequence",
        severity: DiagnosticSeverity.Error,
        format: "unclosed unicode codepoint escape sequence",
        code: Code(4));

    /// <summary>
    /// An illegal escape character after '\'.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalEscapeCharacter = DiagnosticTemplate.Create(
        title: "illegal escape character",
        severity: DiagnosticSeverity.Error,
        format: "illegal escape character '{0}'",
        code: Code(5));

    /// <summary>
    /// A certain kind of token was expected while parsing.
    /// </summary>
    public static readonly DiagnosticTemplate ExpectedToken = DiagnosticTemplate.Create(
        title: "expected token",
        severity: DiagnosticSeverity.Error,
        format: "expected token {0}",
        code: Code(6));

    /// <summary>
    /// Some kind of unexpected input while parsing.
    /// </summary>
    public static readonly DiagnosticTemplate UnexpectedInput = DiagnosticTemplate.Create(
        title: "unexpected input",
        severity: DiagnosticSeverity.Error,
        format: "unexpected input while parsing {0}",
        code: Code(7));

    /// <summary>
    /// Insufficient indentation in a multiline string.
    /// </summary>
    public static readonly DiagnosticTemplate InsufficientIndentationInMultiLinString = DiagnosticTemplate.Create(
        title: "insufficient indentation",
        severity: DiagnosticSeverity.Error,
        format: "insufficient indentation in multiline string",
        code: Code(8));

    /// <summary>
    /// There are extra tokens inline with the opening quotes of a multiline string.
    /// </summary>
    public static readonly DiagnosticTemplate ExtraTokensInlineWithOpenQuotesOfMultiLineString = DiagnosticTemplate.Create(
        title: "illegal tokens",
        severity: DiagnosticSeverity.Error,
        format: "illegal tokens inline with opening quotes of multiline string",
        code: Code(9));

    /// <summary>
    /// The closing quotes of a multiline string are not on a new line.
    /// </summary>
    public static readonly DiagnosticTemplate ClosingQuotesOfMultiLineStringNotOnNewLine = DiagnosticTemplate.Create(
        title: "closing quotes are not on a new line",
        severity: DiagnosticSeverity.Error,
        format: "closing quotes are not on a new line of multiline string",
        code: Code(10));

    /// <summary>
    /// The literal ended unexpectedly.
    /// </summary>
    public static readonly DiagnosticTemplate UnexpectedFloatingPointLiteralEnd = DiagnosticTemplate.Create(
        title: "unexpected floating-point literal end",
        severity: DiagnosticSeverity.Error,
        format: "unexpected end of scientific notation floating-point literal, expected one or more digits after exponent",
        code: Code(11));

    /// <summary>
    /// The character literal ended unexpectedly.
    /// </summary>
    public static readonly DiagnosticTemplate UnexpectedCharacterLiteralEnd = DiagnosticTemplate.Create(
        title: "unexpected character literal end",
        severity: DiagnosticSeverity.Error,
        format: "unexpected end of character literal",
        code: Code(12));

    /// <summary>
    /// The escape sequence ended unexpectedly.
    /// </summary>
    public static readonly DiagnosticTemplate UnexpectedEscapeSequenceEnd = DiagnosticTemplate.Create(
        title: "unexpected escape sequence end",
        severity: DiagnosticSeverity.Error,
        format: "unexpected end of escape sequence",
        code: Code(13));

    /// <summary>
    /// An illegal language element in the context.
    /// </summary>
    public static readonly DiagnosticTemplate IllegalElementInContext = DiagnosticTemplate.Create(
        title: "illegal element in context",
        severity: DiagnosticSeverity.Error,
        format: "illegal language element {0} in context",
        code: Code(14));
}
