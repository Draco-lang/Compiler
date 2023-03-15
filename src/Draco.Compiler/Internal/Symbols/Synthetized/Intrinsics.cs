using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal static class Intrinsics
{
    // Types

    private static Symbol Type(Type type) => type switch
    {
        BuiltinType builtin => new SynthetizedTypeSymbol(builtin),
        _ => throw new System.ArgumentOutOfRangeException(nameof(type)),
    };

    public static Symbol Int32 { get; } = Type(Types.Intrinsics.Int32);
    public static Symbol Bool { get; } = Type(Types.Intrinsics.Bool);

    // Operators

    private static ComparisonOperatorSymbol Comparison(TokenKind token) => new SynthetizedComparisonOperatorSymbol(token);
    private static UnaryOperatorSymbol Unary(TokenKind token, Type operandType, Type returnType) =>
        new SynthetizedUnaryOperatorSymbol(token, operandType, returnType);

    public static UnaryOperatorSymbol Bool_Not { get; } = Unary(TokenKind.KeywordNot, Types.Intrinsics.Bool, Types.Intrinsics.Bool);

    public static ComparisonOperatorSymbol Int32_Equal { get; } = Comparison(TokenKind.Equal);
    public static ComparisonOperatorSymbol Int32_NotEqual { get; } = Comparison(TokenKind.NotEqual);
    public static ComparisonOperatorSymbol Int32_GreaterThan { get; } = Comparison(TokenKind.GreaterThan);
    public static ComparisonOperatorSymbol Int32_LessThan { get; } = Comparison(TokenKind.LessThan);
    public static ComparisonOperatorSymbol Int32_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual);
    public static ComparisonOperatorSymbol Int32_LessEqual { get; } = Comparison(TokenKind.LessEqual);

    public static UnaryOperatorSymbol Int32_Plus { get; } = Unary(TokenKind.Plus, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static UnaryOperatorSymbol Int32_Minus { get; } = Unary(TokenKind.Minus, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
}
