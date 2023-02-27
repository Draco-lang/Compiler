using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal static class Intrinsics
{
    private static Symbol Comparison(TokenKind token) => new SynthetizedComparisonOperatorSymbol(token);
    private static Symbol Unary(TokenKind token) => new SynthetizedUnaryOperatorSymbol(token);

    public static Symbol Int32_Equal { get; } = Comparison(TokenKind.Equal);
    public static Symbol Int32_NotEqual { get; } = Comparison(TokenKind.NotEqual);
    public static Symbol Int32_GreaterThan { get; } = Comparison(TokenKind.GreaterThan);
    public static Symbol Int32_LessThan { get; } = Comparison(TokenKind.LessThan);
    public static Symbol Int32_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual);
    public static Symbol Int32_LessEqual { get; } = Comparison(TokenKind.LessEqual);

    public static Symbol Int32_Plus { get; } = Unary(TokenKind.Plus);
    public static Symbol Int32_Minus { get; } = Unary(TokenKind.Minus);
}
