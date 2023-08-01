using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols.Error;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal static class IntrinsicSymbols
{
    public static TypeSymbol Never => NeverTypeSymbol.Instance;
    public static TypeSymbol ErrorType { get; } = new ErrorTypeSymbol("<error>");
    public static TypeSymbol UninferredType { get; } = new ErrorTypeSymbol("?");

    public static TypeSymbol Unit { get; } = new PrimitiveTypeSymbol("unit", isValueType: true);

    public static TypeSymbol Int8 { get; } = new PrimitiveTypeSymbol("int8", isValueType: true);
    public static TypeSymbol Int16 { get; } = new PrimitiveTypeSymbol("int16", isValueType: true);
    public static TypeSymbol Int32 { get; } = new PrimitiveTypeSymbol("int32", isValueType: true);
    public static TypeSymbol Int64 { get; } = new PrimitiveTypeSymbol("int64", isValueType: true);

    public static TypeSymbol UInt8 { get; } = new PrimitiveTypeSymbol("uint8", isValueType: true);
    public static TypeSymbol UInt16 { get; } = new PrimitiveTypeSymbol("uint16", isValueType: true);
    public static TypeSymbol UInt32 { get; } = new PrimitiveTypeSymbol("uint32", isValueType: true);
    public static TypeSymbol UInt64 { get; } = new PrimitiveTypeSymbol("uint64", isValueType: true);

    public static TypeSymbol Float32 { get; } = new PrimitiveTypeSymbol("float32", isValueType: true);
    public static TypeSymbol Float64 { get; } = new PrimitiveTypeSymbol("float64", isValueType: true);

    public static TypeSymbol Char { get; } = new PrimitiveTypeSymbol("char", isValueType: true);
    public static TypeSymbol Bool { get; } = new PrimitiveTypeSymbol("bool", isValueType: true);

    public static TypeSymbol Object { get; } = new PrimitiveTypeSymbol("object", isValueType: false);
    public static TypeSymbol String { get; } = new PrimitiveTypeSymbol("string", isValueType: false);

    public static ArrayTypeSymbol Array { get; } = new(1);
    public static ArrayConstructorSymbol ArrayCtor { get; } = new(1);

    public static FunctionSymbol Bool_Not { get; } = Unary(TokenKind.KeywordNot, Bool, Bool, CodegenNot);

    public static IEnumerable<Symbol> GenerateIntrinsicSymbols()
    {
        // Array types from 2D to 8D
        for (var i = 2; i <= 8; ++i)
        {
            // Type
            yield return new ArrayTypeSymbol(i);
            // Ctor
            yield return new ArrayConstructorSymbol(i);
        }

        // Numeric operators
        foreach (var type in new[]
        {
            Int8, Int16, Int32, Int64,
            UInt8, UInt16, UInt32, UInt64,
            Float32, Float64,
        })
        {
            // Comparison
            yield return Comparison(TokenKind.Equal, type, type, CodegenEqual);
            yield return Comparison(TokenKind.NotEqual, type, type, CodegenNotEqual);
            yield return Comparison(TokenKind.GreaterThan, type, type, CodegenGreater);
            yield return Comparison(TokenKind.LessThan, type, type, CodegenLess);
            yield return Comparison(TokenKind.GreaterEqual, type, type, CodegenGreaterEqual);
            yield return Comparison(TokenKind.LessEqual, type, type, CodegenLessEqual);

            // Unary
            yield return Unary(TokenKind.Plus, type, type, CodegenPlus);
            yield return Unary(TokenKind.Minus, type, type, CodegenMinus);

            // Binary
            yield return Binary(TokenKind.Plus, type, type, type, CodegenAdd);
            yield return Binary(TokenKind.Minus, type, type, type, CodegenSub);
            yield return Binary(TokenKind.Star, type, type, type, CodegenMul);
            yield return Binary(TokenKind.Slash, type, type, type, CodegenDiv);
            yield return Binary(TokenKind.KeywordMod, type, type, type, CodegenMod);
            yield return Binary(TokenKind.KeywordRem, type, type, type, CodegenRem);
        }
    }

    // Operators

    private static FunctionSymbol Unary(
        TokenKind token,
        TypeSymbol operandType,
        TypeSymbol returnType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        IntrinsicFunctionSymbol.UnaryOperator(token, operandType, returnType, codegen);
    private static FunctionSymbol Binary(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        TypeSymbol returnType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        IntrinsicFunctionSymbol.BinaryOperator(token, leftType, rightType, returnType, codegen);
    private static FunctionSymbol Comparison(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        IntrinsicFunctionSymbol.ComparisonOperator(token, leftType, rightType, codegen);
    private static FunctionSymbol Function(string name, IEnumerable<TypeSymbol> paramTypes, TypeSymbol returnType) =>
        new IntrinsicFunctionSymbol(name, paramTypes, returnType);

    // Codegen

    private static void CodegenPlus(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // No-op
    }

    private static void CodegenMinus(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Mul(target, operands[0], new Constant(-1)));

    private static void CodegenNot(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Equal(target, operands[0], new Constant(false)));

    private static void CodegenAdd(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Add(target, operands[0], operands[1]));

    private static void CodegenSub(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Sub(target, operands[0], operands[1]));

    private static void CodegenMul(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Mul(target, operands[0], operands[1]));

    private static void CodegenDiv(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Div(target, operands[0], operands[1]));

    private static void CodegenRem(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Rem(target, operands[0], operands[1]));

    private static void CodegenMod(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a mod b
        //  <=>
        // (a rem b + b) rem b
        var tmp1 = codegen.DefineRegister(target.Type);
        var tmp2 = codegen.DefineRegister(target.Type);
        codegen.Write(Rem(tmp1, operands[0], operands[1]));
        codegen.Write(Add(tmp2, tmp1, operands[1]));
        codegen.Write(Rem(target, tmp1, operands[1]));
    }

    private static void CodegenLess(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Less(target, operands[0], operands[1]));

    private static void CodegenGreater(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        // a > b
        //  <=>
        // b < a
        codegen.Write(Less(target, operands[1], operands[0]));

    private static void CodegenLessEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a <= b
        //  <=>
        // (b < a) == false
        var tmp = codegen.DefineRegister(Bool);
        codegen.Write(Less(tmp, operands[1], operands[0]));
        codegen.Write(Equal(target, tmp, new Constant(false)));
    }

    private static void CodegenGreaterEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a >= b
        //  <=>
        // (a < b) == false
        var tmp = codegen.DefineRegister(Bool);
        codegen.Write(Less(tmp, operands[0], operands[1]));
        codegen.Write(Equal(target, tmp, new Constant(false)));
    }

    private static void CodegenEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Equal(target, operands[0], operands[1]));

    private static void CodegenNotEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a != b
        //  <=>
        // (a == b) == false
        var tmp = codegen.DefineRegister(Bool);
        codegen.Write(Equal(tmp, operands[0], operands[1]));
        codegen.Write(Equal(target, tmp, new Constant(false)));
    }
}
