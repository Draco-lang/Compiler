using System;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeFixes;

/// <summary>
/// Represents a possible codefix.
/// </summary>
/// <param name="DisplayText">The user-friendly description of this <see cref="CodeFix"/>.</param>
/// <param name="Edits">The <see cref="TextEdit"/>s this <see cref="CodeFix"/> will do, if aplied.</param>
public sealed record class CodeFix(string DisplayText, ImmutableArray<TextEdit> Edits);
