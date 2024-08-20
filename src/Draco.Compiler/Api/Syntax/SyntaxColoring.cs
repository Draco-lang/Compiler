namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Different syntax coloring categories for the highlighter service.
/// </summary>
public enum SyntaxColoring
{
    /// <summary>
    /// An element with unknown syntax coloring.
    /// </summary>
    Unknown,

    /// <summary>
    /// A line comment.
    /// </summary>
    LineComment,

    /// <summary>
    /// A documentation comment.
    /// </summary>
    DocComment,

    /// <summary>
    /// An element with syntax coloring for whitespace.
    /// </summary>
    Whitespace,

    /// <summary>
    /// Starting or ending string quotes.
    /// </summary>
    StringQuotes,

    /// <summary>
    /// Starting or ending character quotes.
    /// </summary>
    CharacterQuotes,

    /// <summary>
    /// String content.
    /// </summary>
    StringContent,

    /// <summary>
    /// Character literal content.
    /// </summary>
    CharacterContent,

    /// <summary>
    /// A boolean literal.
    /// </summary>
    BooleanLiteral,

    /// <summary>
    /// Some number literal.
    /// </summary>
    NumberLiteral,

    /// <summary>
    /// An escape sequence in a string or character literal.
    /// </summary>
    EscapeSequence,

    /// <summary>
    /// A keyword for a declaration.
    /// </summary>
    DeclarationKeyword,

    /// <summary>
    /// A keyword for control flow.
    /// </summary>
    ControlFlowKeyword,

    /// <summary>
    /// Name of a method.
    /// </summary>
    MethodName,

    /// <summary>
    /// The name of a type.
    /// </summary>
    TypeName,

    /// <summary>
    /// The name of a variable.
    /// </summary>
    VariableName,

    /// <summary>
    /// The name of a module.
    /// </summary>
    ModuleName,

    /// <summary>
    /// Some punctuation.
    /// </summary>
    Punctuation,

    /// <summary>
    /// An operator.
    /// </summary>
    Operator,
}
