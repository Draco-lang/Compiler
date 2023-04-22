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

    // TODO: Doc
    Declaration = 1 << 0,

    /// <summary>
    /// Part of an expression (can be also keyword if its usable in expressions, for example 'or').
    /// </summary>
    Expression = 1 << 1,

    /// <summary>
    /// Expression where only valid suggestions are types.
    /// </summary>
    Type = 1 << 2,

    // TODO: Doc
    Import = 1 << 3,

    /// <summary>
    /// Inside member access syntax.
    /// </summary>
    MemberAccess = 1 << 4,
}
