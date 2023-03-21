using System;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a binary operator.
/// </summary>
internal abstract partial class BinaryOperatorSymbol : FunctionSymbol
{
    /// <summary>
    /// Retrieves the name for the binary operator that is referenced by a given token.
    /// </summary>
    /// <param name="token">The <see cref="TokenKind"/> that references the binary operator.</param>
    /// <returns>The name of the symbol to look up the binary operator.</returns>
    public static string GetBinaryOperatorName(TokenKind token) => token switch
    {
        TokenKind.Plus => "operator +",
        TokenKind.Minus => "operator -",
        TokenKind.Star => "operator *",
        TokenKind.Slash => "operator /",
        TokenKind.KeywordMod => "operator mod",
        TokenKind.KeywordRem => "operator rem",
        _ => throw new ArgumentOutOfRangeException(nameof(token)),
    };

    /// <summary>
    /// The left operand of the operator.
    /// </summary>
    public abstract ParameterSymbol Left { get; }

    /// <summary>
    /// The right operand of the operator.
    /// </summary>
    public abstract ParameterSymbol Right { get; }
}
