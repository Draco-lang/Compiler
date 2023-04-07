using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

    public static Symbol Int8 { get; } = Type(IntrinsicTypes.Int8);
    public static Symbol Int16 { get; } = Type(IntrinsicTypes.Int16);
    public static Symbol Int32 { get; } = Type(IntrinsicTypes.Int32);
    public static Symbol Int64 { get; } = Type(IntrinsicTypes.Int64);

    public static Symbol Uint8 { get; } = Type(IntrinsicTypes.Uint8);
    public static Symbol Uint16 { get; } = Type(IntrinsicTypes.Uint16);
    public static Symbol Uint32 { get; } = Type(IntrinsicTypes.Uint32);
    public static Symbol Uint64 { get; } = Type(IntrinsicTypes.Uint64);

    public static Symbol Float32 { get; } = Type(IntrinsicTypes.Float32);
    public static Symbol Float64 { get; } = Type(IntrinsicTypes.Float64);
    public static Symbol String { get; } = Type(IntrinsicTypes.String);
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

        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint8));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint16));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint32));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Uint64));

        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Float32));
        array.AddRange(GetOperatorSymbols(IntrinsicTypes.Float64));
        return array.ToImmutable();
    }

    private static ImmutableArray<Symbol> GetOperatorSymbols(Type type) => ImmutableArray.Create<Symbol>(
        Comparison(TokenKind.Equal, type, type),
        Comparison(TokenKind.NotEqual, type, type),
        Comparison(TokenKind.GreaterThan, type, type),
        Comparison(TokenKind.LessThan, type, type),
        Comparison(TokenKind.GreaterEqual, type, type),
        Comparison(TokenKind.LessEqual, type, type),

        Unary(TokenKind.Plus, type, type),
        Unary(TokenKind.Minus, type, type),

        Binary(TokenKind.Plus, type, type, type),
        Binary(TokenKind.Minus, type, type, type),
        Binary(TokenKind.Star, type, type, type),
        Binary(TokenKind.Slash, type, type, type),
        Binary(TokenKind.KeywordMod, type, type, type),
        Binary(TokenKind.KeywordRem, type, type, type)
    );

    // NOTE: Temporary until we access BCL
    public static FunctionSymbol Print_String { get; } = Function("print", new[] { IntrinsicTypes.String }, IntrinsicTypes.Unit);
    public static FunctionSymbol Print_Int32 { get; } = Function("print", new[] { IntrinsicTypes.Int32 }, IntrinsicTypes.Unit);
    public static FunctionSymbol Println_String { get; } = Function("println", new[] { IntrinsicTypes.String }, IntrinsicTypes.Unit);
    public static FunctionSymbol Println_Int32 { get; } = Function("println", new[] { IntrinsicTypes.Int32 }, IntrinsicTypes.Unit);
}
