using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Syntax.Formatting;

public record struct TokenMetadata(
    WhitespaceBehavior Kind,
    string Text,
    [DisallowNull] Box<bool?>? DoesReturnLine,
    Scope ScopeInfo,
    List<string> LeadingTrivia)
{
    public override readonly string ToString()
    {
        var merged = string.Join(
            ',',
            this.ScopeInfo.Parents.Select(x => x.ToString())
        );
        var returnLine = this.DoesReturnLine == null ? "?" : !this.DoesReturnLine.Value.HasValue ? "?" : this.DoesReturnLine.Value.Value ? "Y" : "N";
        return $"{merged} {returnLine}";
    }
}
