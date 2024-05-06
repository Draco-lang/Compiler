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
    public readonly override string ToString()
    {
        var merged = string.Join(
            ',',
            this.ScopeInfo.ThisAndParents
        );
        var returnLine = !(this.DoesReturnLine?.Value.HasValue ?? false) ? "?" : this.DoesReturnLine.Value.Value ? "Y" : "N";
        return $"{merged} {returnLine}";
    }
}
