using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Services.CodeFixes;

/// <summary>
/// Represents a proposed codefix.
/// </summary>
public sealed class CodeFix(string displayText, ImmutableArray<TextEdit> edits)
{
    /// <summary>
    /// The user-friendly description of this <see cref="CodeFix"/>.
    /// </summary>
    public string DisplayText { get; } = displayText;

    /// <summary>
    /// The <see cref="TextEdit"/>s this <see cref="CodeFix"/> will perform.
    /// </summary>
    public ImmutableArray<TextEdit> Edits { get; } = edits;
}
