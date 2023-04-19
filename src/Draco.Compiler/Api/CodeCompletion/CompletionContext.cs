namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a context of a completion.
/// </summary>
public enum CompletionContext
{
    /// <summary>
    /// Part of an expression (can be also keyword if its usable in expressions, for example 'or').
    /// </summary>
    ExpressionContent,

    /// <summary>
    /// Expression where only valid suggestions are types.
    /// </summary>
    TypeExpression,

    /// <summary>
    /// Member access expression.
    /// </summary>
    MemberAccess,

    /// <summary>
    /// Import statement where only modules are valid suggestions.
    /// </summary>
    ModuleImport,

    /// <summary>
    /// Keyword that starts declaration.
    /// </summary>
    DeclarationKeyword,
}
