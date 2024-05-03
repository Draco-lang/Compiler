using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal record struct TokenMetadata(WhitespaceBehavior Kind,
    string Text,
    [DisallowNull] Box<bool?>? DoesReturnLine,
    Scope ScopeInfo,
    IReadOnlyCollection<string> LeadingComments);