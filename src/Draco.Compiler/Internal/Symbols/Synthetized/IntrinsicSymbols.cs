using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal static class IntrinsicSymbols
{
    // Types

    private static Symbol Type(Type type) => type switch
    {
        BuiltinType builtin => new SynthetizedTypeSymbol(builtin),
        _ => throw new System.ArgumentOutOfRangeException(nameof(type)),
    };

    public static Symbol Int32 { get; } = Type(IntrinsicTypes.Int32);
    public static Symbol Float64 { get; } = Type(IntrinsicTypes.Float64);
    public static Symbol Bool { get; } = Type(IntrinsicTypes.Bool);

    // Operators

    private static FunctionSymbol Unary(TokenKind token, Type operandType, Type returnType) =>
        SynthetizedFunctionSymbol.UnaryOperator(token, operandType, returnType);
    private static FunctionSymbol Binary(TokenKind token, Type leftType, Type rightType, Type returnType) =>
        SynthetizedFunctionSymbol.BinaryOperator(token, leftType, rightType, returnType);
    private static FunctionSymbol Comparison(TokenKind token, Type leftType, Type rightType) =>
        SynthetizedFunctionSymbol.ComparisonOperator(token, leftType, rightType);
    private static FunctionSymbol Function(string name, IEnumerable<Type> paramTypes, Type returnType) =>
        new SynthetizedFunctionSymbol(name, paramTypes, returnType);

    public static FunctionSymbol Bool_Not { get; } = Unary(TokenKind.KeywordNot, IntrinsicTypes.Bool, IntrinsicTypes.Bool);

    public static FunctionSymbol Int32_Equal { get; } = Comparison(TokenKind.Equal, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_NotEqual { get; } = Comparison(TokenKind.NotEqual, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_GreaterThan { get; } = Comparison(TokenKind.GreaterThan, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_LessThan { get; } = Comparison(TokenKind.LessThan, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_LessEqual { get; } = Comparison(TokenKind.LessEqual, IntrinsicTypes.Int32, IntrinsicTypes.Int32);

    public static FunctionSymbol Int32_Plus { get; } = Unary(TokenKind.Plus, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_Minus { get; } = Unary(TokenKind.Minus, IntrinsicTypes.Int32, IntrinsicTypes.Int32);

    public static FunctionSymbol Int32_Add { get; } = Binary(TokenKind.Plus, IntrinsicTypes.Int32, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_Sub { get; } = Binary(TokenKind.Minus, IntrinsicTypes.Int32, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_Mul { get; } = Binary(TokenKind.Star, IntrinsicTypes.Int32, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_Div { get; } = Binary(TokenKind.Slash, IntrinsicTypes.Int32, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_Mod { get; } = Binary(TokenKind.KeywordMod, IntrinsicTypes.Int32, IntrinsicTypes.Int32, IntrinsicTypes.Int32);
    public static FunctionSymbol Int32_Rem { get; } = Binary(TokenKind.KeywordRem, IntrinsicTypes.Int32, IntrinsicTypes.Int32, IntrinsicTypes.Int32);

    // TODO: Rest of float operators
    public static FunctionSymbol Float64_Mul { get; } = Binary(TokenKind.Star, IntrinsicTypes.Float64, IntrinsicTypes.Float64, IntrinsicTypes.Float64);

    // NOTE: Temporary until we access BCL
    public static FunctionSymbol Println { get; } = Function("println", new[] { IntrinsicTypes.String }, IntrinsicTypes.Unit);
}
