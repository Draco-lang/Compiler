using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
using static Draco.Compiler.Internal.Semantics.Symbols.Symbol;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.Symbols;

/// <summary>
/// A collection of compiler intrinsics.
/// </summary>
internal static class Intrinsics
{
    public static class Types
    {
        public static ITypeDefinition Unit { get; } = MakeIntrinsicTypeDefinition("unit", Type.Unit);
        public static ITypeDefinition Int32 { get; } = MakeIntrinsicTypeDefinition("int32", Type.Int32);
        public static ITypeDefinition Float64 { get; } = MakeIntrinsicTypeDefinition("float64", Type.Float64);
        public static ITypeDefinition Bool { get; } = MakeIntrinsicTypeDefinition("bool", Type.Bool);
        public static ITypeDefinition String { get; } = MakeIntrinsicTypeDefinition("string", Type.String);
        public static ITypeDefinition Char { get; } = MakeIntrinsicTypeDefinition("char", Type.Char);
    }

    public static class Operators
    {
        public static IFunction Not_Bool { get; } = MakeIntrinsicUnaryOperator(TokenKind.KeywordNot, Type.Bool, Type.Bool);
        public static IFunction Pos_Int32 { get; } = MakeIntrinsicUnaryOperator(TokenKind.Plus, Type.Int32, Type.Int32);
        public static IFunction Neg_Int32 { get; } = MakeIntrinsicUnaryOperator(TokenKind.Minus, Type.Int32, Type.Int32);

        public static IFunction Add_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenKind.Plus, Type.Int32, Type.Int32, Type.Int32);
        public static IFunction Sub_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenKind.Minus, Type.Int32, Type.Int32, Type.Int32);
        public static IFunction Mul_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenKind.Star, Type.Int32, Type.Int32, Type.Int32);
        public static IFunction Div_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenKind.Slash, Type.Int32, Type.Int32, Type.Int32);
        public static IFunction Mod_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenKind.KeywordMod, Type.Int32, Type.Int32, Type.Int32);
        public static IFunction Rem_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenKind.KeywordRem, Type.Int32, Type.Int32, Type.Int32);

        public static IFunction Less_Int32 { get; } = MakeIntrinsicRelationalOperator(TokenKind.LessThan, Type.Int32, Type.Int32, Type.Bool);
        public static IFunction LessEqual_Int32 { get; } = MakeIntrinsicRelationalOperator(TokenKind.LessEqual, Type.Int32, Type.Int32, Type.Bool);
        public static IFunction Greater_Int32 { get; } = MakeIntrinsicRelationalOperator(TokenKind.GreaterThan, Type.Int32, Type.Int32, Type.Bool);
        public static IFunction GreaterEqual_Int32 { get; } = MakeIntrinsicRelationalOperator(TokenKind.GreaterEqual, Type.Int32, Type.Int32, Type.Bool);
        public static IFunction Equal_Int32 { get; } = MakeIntrinsicRelationalOperator(TokenKind.Equal, Type.Int32, Type.Int32, Type.Bool);
        public static IFunction NotEqual_Int32 { get; } = MakeIntrinsicRelationalOperator(TokenKind.NotEqual, Type.Int32, Type.Int32, Type.Bool);
    }

    public static class Functions
    {
        public static IFunction Println { get; } = MakeIntrinsicFunction("println", ImmutableArray.Create(Type.String), Type.Unit);
    }
}
