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
    public static Symbol Float64 { get; } = Type(Types.Intrinsics.Float64);
    public static Symbol Bool { get; } = Type(Types.Intrinsics.Bool);

    // Operators

    private static FunctionSymbol Unary(TokenKind token, Type operandType, Type returnType) =>
        SynthetizedFunctionSymbol.UnaryOperator(token, operandType, returnType);
    private static FunctionSymbol Binary(TokenKind token, Type leftType, Type rightType, Type returnType) =>
        SynthetizedFunctionSymbol.BinaryOperator(token, leftType, rightType, returnType);
    private static FunctionSymbol Comparison(TokenKind token, Type leftType, Type rightType) =>
        SynthetizedFunctionSymbol.ComparisonOperator(token, leftType, rightType);

    public static FunctionSymbol Bool_Not { get; } = Unary(TokenKind.KeywordNot, Types.Intrinsics.Bool, Types.Intrinsics.Bool);

    public static FunctionSymbol Int32_Equal { get; } = Comparison(TokenKind.Equal, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_NotEqual { get; } = Comparison(TokenKind.NotEqual, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_GreaterThan { get; } = Comparison(TokenKind.GreaterThan, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_LessThan { get; } = Comparison(TokenKind.LessThan, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_LessEqual { get; } = Comparison(TokenKind.LessEqual, Types.Intrinsics.Int32, Types.Intrinsics.Int32);

    public static FunctionSymbol Int32_Plus { get; } = Unary(TokenKind.Plus, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_Minus { get; } = Unary(TokenKind.Minus, Types.Intrinsics.Int32, Types.Intrinsics.Int32);

    public static FunctionSymbol Int32_Add { get; } = Binary(TokenKind.Plus, Types.Intrinsics.Int32, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_Sub { get; } = Binary(TokenKind.Minus, Types.Intrinsics.Int32, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_Mul { get; } = Binary(TokenKind.Star, Types.Intrinsics.Int32, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_Div { get; } = Binary(TokenKind.Slash, Types.Intrinsics.Int32, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_Mod { get; } = Binary(TokenKind.KeywordMod, Types.Intrinsics.Int32, Types.Intrinsics.Int32, Types.Intrinsics.Int32);
    public static FunctionSymbol Int32_Rem { get; } = Binary(TokenKind.KeywordRem, Types.Intrinsics.Int32, Types.Intrinsics.Int32, Types.Intrinsics.Int32);

    // TODO: Rest of float operators
    public static FunctionSymbol Float64_Mul { get; } = Binary(TokenKind.Star, Types.Intrinsics.Float64, Types.Intrinsics.Float64, Types.Intrinsics.Float64);
}
