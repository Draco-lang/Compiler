namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// The different kinds of trivia.
/// </summary>
public enum TriviaKind
{
    /// <summary>
    /// Any horizontal whitespace.
    /// </summary>
    Whitespace,

    /// <summary>
    /// Any newline sequence.
    /// </summary>
    Newline,

    /// <summary>
    /// Single line comments.
    /// </summary>
    LineComment,

    /// <summary>
    /// Documentation comment.
    /// </summary>
    DocumentationComment,
}
