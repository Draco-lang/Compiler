using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal record struct TokenMetadata(WhitespaceBehavior Kind,
    Api.Syntax.SyntaxToken Token,
    [DisallowNull] string? TokenOverride,
    [DisallowNull] Box<bool?>? DoesReturnLine,
    Scope ScopeInfo,
    IReadOnlyCollection<string> LeadingComments);
