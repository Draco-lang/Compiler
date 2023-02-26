using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a comparison operator.
/// </summary>
internal abstract partial class ComparisonOperatorSymbol : BinaryOperatorSymbol
{
    /// <summary>
    /// Retrieves the name for the comparison operator that is referenced by a given token.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> that references the comparison operator.</param>
    /// <returns>The name of the symbol to look up the comparison operator.</returns>
    public static string GetComparisonOperatorName(TokenKind token) => token switch
    {
        TokenKind.Equal => "operator ==",
        TokenKind.NotEqual => "operator !=",
        TokenKind.GreaterThan => "operator >",
        TokenKind.LessThan => "operator <",
        TokenKind.GreaterEqual => "operator >=",
        TokenKind.LessEqual => "operator <=",
        _ => throw new ArgumentOutOfRangeException(nameof(token)),
    };
}
