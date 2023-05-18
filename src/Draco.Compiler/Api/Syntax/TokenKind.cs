namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// The different kinds of tokens in the syntax tree.
/// </summary>
public enum TokenKind
{
    /// <summary>
    /// The end of the read source.
    /// </summary>
    EndOfInput,

    /// <summary>
    /// Any unknown character.
    /// </summary>
    Unknown,

    /// <summary>
    /// Non-keyword names.
    /// </summary>
    Identifier,

    /// <summary>
    /// An integer literal.
    /// </summary>
    LiteralInteger,

    /// <summary>
    /// An integer literal.
    /// </summary>
    LiteralFloat,

    /// <summary>
    /// A character literal.
    /// </summary>
    LiteralCharacter,

    /// <summary>
    /// The start of a single-line string literal.
    /// </summary>
    LineStringStart,

    /// <summary>
    /// The end of a single-line string literal.
    /// </summary>
    LineStringEnd,

    /// <summary>
    /// The start of a multi-line string literal.
    /// </summary>
    MultiLineStringStart,

    /// <summary>
    /// The end of a multi-line string literal.
    /// </summary>
    MultiLineStringEnd,

    /// <summary>
    /// A sequence of characters in a string-literal.
    /// </summary>
    StringContent,

    /// <summary>
    /// A newline in multiline strings.
    /// </summary>
    StringNewline,

    /// <summary>
    /// The start of string interpolation.
    /// </summary>
    InterpolationStart,

    /// <summary>
    /// The end of string interpolation.
    /// </summary>
    InterpolationEnd,

    /// <summary>
    /// The keyword 'and'.
    /// </summary>
    KeywordAnd,

    /// <summary>
    /// The keyword 'else'.
    /// </summary>
    KeywordElse,

    /// <summary>
    /// The keyword 'false'.
    /// </summary>
    KeywordFalse,

    /// <summary>
    /// The keyword 'from'.
    /// </summary>
    KeywordFrom,

    /// <summary>
    /// The keyword 'func'.
    /// </summary>
    KeywordFunc,

    /// <summary>
    /// The keyword 'goto'.
    /// </summary>
    KeywordGoto,

    /// <summary>
    /// The keyword 'if'.
    /// </summary>
    KeywordIf,

    /// <summary>
    /// The keyword 'import'.
    /// </summary>
    KeywordImport,

    /// <summary>
    /// The keyword 'internal'.
    /// </summary>
    KeywordInternal,

    /// <summary>
    /// The keyword 'mod'.
    /// </summary>
    KeywordMod,

    /// <summary>
    /// The keyword 'not'.
    /// </summary>
    KeywordNot,

    /// <summary>
    /// The keyword 'or'.
    /// </summary>
    KeywordOr,

    /// <summary>
    /// The keyword 'public'.
    /// </summary>
    KeywordPublic,

    /// <summary>
    /// The keyword 'rem'.
    /// </summary>
    KeywordRem,

    /// <summary>
    /// The keyword 'return'.
    /// </summary>
    KeywordReturn,

    /// <summary>
    /// The keyword 'true'.
    /// </summary>
    KeywordTrue,

    /// <summary>
    /// The keyword 'val'.
    /// </summary>
    KeywordVal,

    /// <summary>
    /// The keyword 'var'.
    /// </summary>
    KeywordVar,

    /// <summary>
    /// The keyword 'while'
    /// </summary>
    KeywordWhile,

    /// <summary>
    /// '('.
    /// </summary>
    ParenOpen,

    /// <summary>
    /// ')'.
    /// </summary>
    ParenClose,

    /// <summary>
    /// '{'.
    /// </summary>
    CurlyOpen,

    /// <summary>
    /// '}'.
    /// </summary>
    CurlyClose,

    /// <summary>
    /// '['.
    /// </summary>
    BracketOpen,

    /// <summary>
    /// ']'.
    /// </summary>
    BracketClose,

    /// <summary>
    /// '.'.
    /// </summary>
    Dot,

    /// <summary>
    /// ','.
    /// </summary>
    Comma,

    /// <summary>
    /// ':'.
    /// </summary>
    Colon,

    /// <summary>
    /// ';'.
    /// </summary>
    Semicolon,

    /// <summary>
    /// '+'.
    /// </summary>
    Plus,

    /// <summary>
    /// '-'.
    /// </summary>
    Minus,

    /// <summary>
    /// '*'.
    /// </summary>
    Star,

    /// <summary>
    /// '/'.
    /// </summary>
    Slash,

    /// <summary>
    /// '<'.
    /// </summary>
    LessThan,

    /// <summary>
    /// '>'.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// '<='.
    /// </summary>
    LessEqual,

    /// <summary>
    /// '>='.
    /// </summary>
    GreaterEqual,

    /// <summary>
    /// '=='.
    /// </summary>
    Equal,

    /// <summary>
    /// '!='.
    /// </summary>
    NotEqual,

    /// <summary>
    /// '='.
    /// </summary>
    Assign,

    /// <summary>
    /// '+='.
    /// </summary>
    PlusAssign,

    /// <summary>
    /// '-='.
    /// </summary>
    MinusAssign,

    /// <summary>
    /// '*='.
    /// </summary>
    StarAssign,

    /// <summary>
    /// '/='.
    /// </summary>
    SlashAssign,
}
