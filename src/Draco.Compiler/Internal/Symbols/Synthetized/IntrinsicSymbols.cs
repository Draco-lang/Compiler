using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal static class IntrinsicSymbols
{
    public static TypeSymbol ErrorType => ErrorTypeSymbol.Instance;
    public static TypeSymbol Never => NeverTypeSymbol.Instance;
    public static TypeSymbol Unit { get; } = new PrimitiveTypeSymbol("unit", isValueType: true);

    public static TypeSymbol Int8 { get; } = new PrimitiveTypeSymbol("int8", isValueType: true);
    public static TypeSymbol Int16 { get; } = new PrimitiveTypeSymbol("int16", isValueType: true);
    public static TypeSymbol Int32 { get; } = new PrimitiveTypeSymbol("int32", isValueType: true);
    public static TypeSymbol Int64 { get; } = new PrimitiveTypeSymbol("int64", isValueType: true);

    public static TypeSymbol Uint8 { get; } = new PrimitiveTypeSymbol("uint8", isValueType: true);
    public static TypeSymbol Uint16 { get; } = new PrimitiveTypeSymbol("uint16", isValueType: true);
    public static TypeSymbol Uint32 { get; } = new PrimitiveTypeSymbol("uint32", isValueType: true);
    public static TypeSymbol Uint64 { get; } = new PrimitiveTypeSymbol("uint64", isValueType: true);

    public static TypeSymbol Float32 { get; } = new PrimitiveTypeSymbol("float32", isValueType: true);
    public static TypeSymbol Float64 { get; } = new PrimitiveTypeSymbol("float64", isValueType: true);

    public static TypeSymbol String { get; } = new PrimitiveTypeSymbol("string", isValueType: false);
    public static TypeSymbol Bool { get; } = new PrimitiveTypeSymbol("bool", isValueType: true);
    public static TypeSymbol Object { get; } = new PrimitiveTypeSymbol("object", isValueType: false);

    // Operators

    private static FunctionSymbol Unary(TokenKind token, TypeSymbol operandType, TypeSymbol returnType) =>
        IntrinsicFunctionSymbol.UnaryOperator(token, operandType, returnType);
    private static FunctionSymbol Binary(TokenKind token, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol returnType) =>
        IntrinsicFunctionSymbol.BinaryOperator(token, leftType, rightType, returnType);
    private static FunctionSymbol Comparison(TokenKind token, TypeSymbol leftType, TypeSymbol rightType) =>
        IntrinsicFunctionSymbol.ComparisonOperator(token, leftType, rightType);
    private static FunctionSymbol Function(string name, IEnumerable<TypeSymbol> paramTypes, TypeSymbol returnType) =>
        new IntrinsicFunctionSymbol(name, paramTypes, returnType);

    public static FunctionSymbol Bool_Not { get; } = Unary(TokenKind.KeywordNot, Bool, Bool);

    public static ImmutableArray<Symbol> OperatorSymbols
    {
        get
        {
            if (operatorSymbols is null) operatorSymbols = GetOperatorSymbols();
            return (ImmutableArray<Symbol>)operatorSymbols;
        }
    }

    private static ImmutableArray<Symbol>? operatorSymbols;

    private static ImmutableArray<Symbol> GetOperatorSymbols()
    {
        var array = ImmutableArray.CreateBuilder<Symbol>();
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Int8));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Int16));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Int32));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Int64));

        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint8, true));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint16, true));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint32, true));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint64, true));

        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Float32));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Float64));
        return array.ToImmutable();
    }

    private static ImmutableArray<Symbol> GetOperatorSymbols(Type type, bool isUnsigned = false)
    {
        var array = ImmutableArray.CreateBuilder<Symbol>();
        array.Add(Comparison(TokenKind.Equal, type, type));
        array.Add(Comparison(TokenKind.NotEqual, type, type));
        array.Add(Comparison(TokenKind.GreaterThan, type, type));
        array.Add(Comparison(TokenKind.LessThan, type, type));
        array.Add(Comparison(TokenKind.GreaterEqual, type, type));
        array.Add(Comparison(TokenKind.LessEqual, type, type));

        array.Add(Unary(TokenKind.Plus, type, type));
        if (!isUnsigned) array.Add(Unary(TokenKind.Minus, type, type));

        array.Add(Binary(TokenKind.Plus, type, type, type));
        array.Add(Binary(TokenKind.Minus, type, type, type));
        array.Add(Binary(TokenKind.Star, type, type, type));
        array.Add(Binary(TokenKind.Slash, type, type, type));
        array.Add(Binary(TokenKind.KeywordMod, type, type, type));
        array.Add(Binary(TokenKind.KeywordRem, type, type, type));
        return array.ToImmutable();
    }
}
