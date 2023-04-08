using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal static class IntrinsicSymbols
{
    public static TypeSymbol Int32 { get; } = Type(IntrinsicTypes.Int32);
    public static TypeSymbol Float64 { get; } = Type(IntrinsicTypes.Float64);
    public static TypeSymbol String { get; } = Type(IntrinsicTypes.String);
    public static TypeSymbol Bool { get; } = Type(IntrinsicTypes.Bool);

    // Operators

    private static FunctionSymbol Unary(TokenKind token, TypeSymbol operandType, TypeSymbol returnType) =>
        SynthetizedFunctionSymbol.UnaryOperator(token, operandType, returnType);
    private static FunctionSymbol Binary(TokenKind token, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol returnType) =>
        SynthetizedFunctionSymbol.BinaryOperator(token, leftType, rightType, returnType);
    private static FunctionSymbol Comparison(TokenKind token, TypeSymbol leftType, TypeSymbol rightType) =>
        SynthetizedFunctionSymbol.ComparisonOperator(token, leftType, rightType);
    private static FunctionSymbol Function(string name, IEnumerable<TypeSymbol> paramTypes, TypeSymbol returnType) =>
        new SynthetizedFunctionSymbol(name, paramTypes, returnType);

    public static FunctionSymbol Bool_Not { get; } = Unary(TokenKind.KeywordNot, Bool, Bool);

    // Int32

    public static FunctionSymbol Int32_Equal { get; } = Comparison(TokenKind.Equal, Int32, Int32);
    public static FunctionSymbol Int32_NotEqual { get; } = Comparison(TokenKind.NotEqual, Int32, Int32);
    public static FunctionSymbol Int32_GreaterThan { get; } = Comparison(TokenKind.GreaterThan, Int32, Int32);
    public static FunctionSymbol Int32_LessThan { get; } = Comparison(TokenKind.LessThan, Int32, Int32);
    public static FunctionSymbol Int32_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual, Int32, Int32);
    public static FunctionSymbol Int32_LessEqual { get; } = Comparison(TokenKind.LessEqual, Int32, Int32);

    public static FunctionSymbol Int32_Plus { get; } = Unary(TokenKind.Plus, Int32, Int32);
    public static FunctionSymbol Int32_Minus { get; } = Unary(TokenKind.Minus, Int32, Int32);

    public static FunctionSymbol Int32_Add { get; } = Binary(TokenKind.Plus, Int32, Int32, Int32);
    public static FunctionSymbol Int32_Sub { get; } = Binary(TokenKind.Minus, Int32, Int32, Int32);
    public static FunctionSymbol Int32_Mul { get; } = Binary(TokenKind.Star, Int32, Int32, Int32);
    public static FunctionSymbol Int32_Div { get; } = Binary(TokenKind.Slash, Int32, Int32, Int32);
    public static FunctionSymbol Int32_Mod { get; } = Binary(TokenKind.KeywordMod, Int32, Int32, Int32);
    public static FunctionSymbol Int32_Rem { get; } = Binary(TokenKind.KeywordRem, Int32, Int32, Int32);

    // Float64

    public static FunctionSymbol Float64_Equal { get; } = Comparison(TokenKind.Equal, Float64, Float64);
    public static FunctionSymbol Float64_NotEqual { get; } = Comparison(TokenKind.NotEqual, Float64, Float64);
    public static FunctionSymbol Float64_GreaterThan { get; } = Comparison(TokenKind.GreaterThan, Float64, Float64);
    public static FunctionSymbol Float64_LessThan { get; } = Comparison(TokenKind.LessThan, Float64, Float64);
    public static FunctionSymbol Float64_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual, Float64, Float64);
    public static FunctionSymbol Float64_LessEqual { get; } = Comparison(TokenKind.LessEqual, Float64, Float64);

    public static FunctionSymbol Float64_Plus { get; } = Unary(TokenKind.Plus, Float64, Float64);
    public static FunctionSymbol Float64_Minus { get; } = Unary(TokenKind.Minus, Float64, Float64);

    public static FunctionSymbol Float64_Add { get; } = Binary(TokenKind.Plus, Float64, Float64, Float64);
    public static FunctionSymbol Float64_Sub { get; } = Binary(TokenKind.Minus, Float64, Float64, Float64);
    public static FunctionSymbol Float64_Mul { get; } = Binary(TokenKind.Star, Float64, Float64, Float64);
    public static FunctionSymbol Float64_Div { get; } = Binary(TokenKind.Slash, Float64, Float64, Float64);
    public static FunctionSymbol Float64_Mod { get; } = Binary(TokenKind.KeywordMod, Float64, Float64, Float64);
    public static FunctionSymbol Float64_Rem { get; } = Binary(TokenKind.KeywordRem, Float64, Float64, Float64);
}
