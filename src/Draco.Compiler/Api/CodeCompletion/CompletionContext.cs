using System;

namespace Draco.Compiler.Api.CodeCompletion;

/// <summary>
/// Represents a code context of a completion. For example, if the completion is part of expression/module import/...
/// Contexts can be combined. For example, when the cursor points at a member access expression the context will be MemberAccess | Expression.
/// </summary>
[Flags]
public enum CompletionContext
{
    /// <summary>
    /// No context.
    /// </summary>
    None = 0,

    /// <summary>
    /// Declaration start.
    /// </summary>
    Declaration = 1 << 0,

    /// <summary>
    /// Part of an expression (can be also keyword if its usable in expressions, for example 'or').
    /// </summary>
    Expression = 1 << 1,

    /// <summary>
    /// Expression where only valid suggestions are types.
    /// </summary>
    Type = 1 << 2,

    /// <summary>
    /// Inside import.
    /// </summary>
    Import = 1 << 3,

    /// <summary>
    /// Inside member access syntax.
    /// </summary>
    Member = 1 << 4,
}
