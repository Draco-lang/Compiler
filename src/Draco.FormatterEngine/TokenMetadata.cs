using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// Represent the formatting metadata of a token.
/// </summary>
/// <param name="Kind">The whitespace behavior of this token.</param>
/// <param name="Text">The text of the token.</param>
/// <param name="DoesReturnLine">
/// <para>
///     An optional reference to a nullable boolean, indicate whether the line returns or not.
/// </para>
/// <para>
///     If the field is null, it means there was no decision was made on this token, we default to not returning a line.
///</para>
/// <para>
///     If the field is set to a reference, but the value of the Box null, it's because this is a pending
/// </para>
/// </param>
/// <param name="ScopeInfo">The deepest scope this token belong to.</param>
/// <param name="LeadingTrivia"> Lines of the leading trivia. Each line have an newline apppended to it when outputing the text. </param>
public record struct TokenMetadata(
    WhitespaceBehavior Kind,
    string Text,
    [DisallowNull] Box<bool?>? DoesReturnLine,
    Scope ScopeInfo,
    List<string> LeadingTrivia)
{
    public readonly override string ToString()
    {
        var merged = string.Join(',', this.ScopeInfo.ThisAndParents);
        var returnLine = this.DoesReturnLine?.Value.HasValue == true ? this.DoesReturnLine.Value.Value ? "Y" : "N" : "?";
        return $"{merged} {returnLine}";
    }
}
