using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
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
        public static ITypeDefinition Bool { get; } = MakeIntrinsicTypeDefinition("bool", Type.Bool);
        public static ITypeDefinition String { get; } = MakeIntrinsicTypeDefinition("string", Type.String);
        public static ITypeDefinition Char { get; } = MakeIntrinsicTypeDefinition("char", Type.Char);
    }

    public static class Operators
    {
        public static IUnaryOperator Not_Bool { get; } = MakeIntrinsicUnaryOperator(TokenType.KeywordNot, Type.Bool, Type.Bool);

        public static IBinaryOperator Add_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.Plus, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator Sub_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.Minus, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator Mul_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.Star, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator Div_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.Slash, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator Mod_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.KeywordMod, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator Rem_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.KeywordRem, Type.Int32, Type.Int32, Type.Int32);

        public static IBinaryOperator Less_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.LessThan, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator LessEqual_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.LessEqual, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator Greater_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.GreaterThan, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator GreaterEqual_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.GreaterEqual, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator Equal_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.Equal, Type.Int32, Type.Int32, Type.Int32);
        public static IBinaryOperator NotEqual_Int32 { get; } = MakeIntrinsicBinaryOperator(TokenType.NotEqual, Type.Int32, Type.Int32, Type.Int32);
    }
}
