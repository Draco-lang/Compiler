using System;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.CodeFixes;

internal record class CodeFix(string DisplayText, Func<ImmutableArray<TextEdit>> ApplyFix);
