using System;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a code context of a completion. For example, if the completion is part of expression/module import/...
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
    Expression = 1 << 0,

    /// <summary>
    /// Expression where only valid suggestions are types.
    /// </summary>
    Type = 1 << 1,

    /// <summary>
    /// Inside expression member access syntax.
    /// </summary>
    MemberExpressionAccess = 1 << 2,

    /// <summary>
    /// Inside type member access syntax.
    /// </summary>
    MemberTypeAccess = 1 << 3,

    /// <summary>
    /// Member import statement, where only modules are valid suggestions.
    /// </summary>
    MemberModuleImport = 1 << 4,

    /// <summary>
    /// Root module import statement, where only modules are valid suggestions.
    /// </summary>
    RootModuleImport = 1 << 5,

    /// <summary>
    /// Keyword that starts a declaration.
    /// </summary>
    DeclarationKeyword = 1 << 6,
}
