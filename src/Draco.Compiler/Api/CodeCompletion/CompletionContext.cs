using System;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a code context of a completion (For example if the completion is part of expression/ModuleImport/...).
/// </summary>
[Flags]
public enum CompletionContext
{
    /// <summary>
    /// No context.
    /// </summary>
    None = 0,
    /// <summary>
    /// Part of an expression (can be also keyword if its usable in expressions, for example 'or').
    /// </summary>
    Expression = 1,

    /// <summary>
    /// Expression where only valid suggestions are types.
    /// </summary>
    Type = 2,

    /// <summary>
    /// Inside some kind of member access syntax.
    /// </summary>
    MemberAccess = 4,

    /// <summary>
    /// Import statement, where only modules are valid suggestions.
    /// </summary>
    ModuleImport = 8,

    /// <summary>
    /// Keyword that starts a declaration.
    /// </summary>
    DeclarationKeyword = 16,
}
