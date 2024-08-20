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
    DocumentationComment,

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
    /// Quotes for interpolation.
    /// </summary>
    InterpolationQuotes,

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
    /// A keyword for visibility.
    /// </summary>
    VisibilityKeyword,

    /// <summary>
    /// Name of a method.
    /// </summary>
    FunctionName,

    /// <summary>
    /// The name of a reference type.
    /// </summary>
    ReferenceTypeName,

    /// <summary>
    /// The name of a value type.
    /// </summary>
    ValueTypeName,

    /// <summary>
    /// The name of a variable.
    /// </summary>
    VariableName,

    /// <summary>
    /// The name of a parameter.
    /// </summary>
    ParameterName,

    /// <summary>
    /// The name of a field.
    /// </summary>
    FieldName,

    /// <summary>
    /// The name of a property.
    /// </summary>
    PropertyName,

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

    /// <summary>
    /// Pairs of different parentheses.
    /// </summary>
    Parenthesis,
}
