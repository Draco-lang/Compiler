using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Syntax.Formatting;

public record struct TokenMetadata(
    WhitespaceBehavior Kind,
    string Text,
    [DisallowNull] Box<bool?>? DoesReturnLine,
    Scope ScopeInfo,
    List<string> LeadingTrivia);
