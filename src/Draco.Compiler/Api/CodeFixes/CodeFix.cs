using System;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeFixes;

public record class CodeFix(string DisplayText, ImmutableArray<TextEdit> Edits);
