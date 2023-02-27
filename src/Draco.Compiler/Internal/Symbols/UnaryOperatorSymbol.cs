using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an unary operator.
/// </summary>
internal abstract partial class UnaryOperatorSymbol : FunctionSymbol
{
    /// <summary>
    /// Retrieves the name for the unary operator that is referenced by a given token.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> that references the unary operator.</param>
    /// <returns>The name of the symbol to look up the unary operator.</returns>
    public static string GetUnaryOperatorName(TokenKind token) => token switch
    {
        TokenKind.Plus => "unary +",
        TokenKind.Minus => "unary -",
        _ => throw new ArgumentOutOfRangeException(nameof(token)),
    };

    /// <summary>
    /// The operand of the operator.
    /// </summary>
    public abstract ParameterSymbol Operand { get; }
}
