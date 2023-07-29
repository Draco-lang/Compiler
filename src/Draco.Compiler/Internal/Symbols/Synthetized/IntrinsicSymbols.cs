using System.Collections.Generic;
using System.Collections.Immutable;
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
    public static TypeSymbol Int32 { get; } = new PrimitiveTypeSymbol("int32", isValueType: true);
    public static TypeSymbol Float64 { get; } = new PrimitiveTypeSymbol("float64", isValueType: true);
    public static TypeSymbol String { get; } = new PrimitiveTypeSymbol("string", isValueType: false);
    public static TypeSymbol Bool { get; } = new PrimitiveTypeSymbol("bool", isValueType: true);
    public static TypeSymbol Object { get; } = new PrimitiveTypeSymbol("object", isValueType: false);
    public static ArrayTypeSymbol Array { get; } = new(1);
    public static ArrayConstructorSymbol ArrayCtor { get; } = new(1);

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

    public static FunctionSymbol Bool_Not { get; } = Unary(TokenKind.KeywordNot, Bool, Bool, CodegenNot);

    // Int32

    public static FunctionSymbol Int32_Equal { get; } = Comparison(TokenKind.Equal, Int32, Int32, CodegenEqual);
    public static FunctionSymbol Int32_NotEqual { get; } = Comparison(TokenKind.NotEqual, Int32, Int32, CodegenNotEqual);
    public static FunctionSymbol Int32_GreaterThan { get; } = Comparison(TokenKind.GreaterThan, Int32, Int32, CodegenGreater);
    public static FunctionSymbol Int32_LessThan { get; } = Comparison(TokenKind.LessThan, Int32, Int32, CodegenLess);
    public static FunctionSymbol Int32_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual, Int32, Int32, CodegenGreaterEqual);
    public static FunctionSymbol Int32_LessEqual { get; } = Comparison(TokenKind.LessEqual, Int32, Int32, CodegenLessEqual);

    public static FunctionSymbol Int32_Plus { get; } = Unary(TokenKind.Plus, Int32, Int32, CodegenPlus);
    public static FunctionSymbol Int32_Minus { get; } = Unary(TokenKind.Minus, Int32, Int32, CodegenMinus);

    public static FunctionSymbol Int32_Add { get; } = Binary(TokenKind.Plus, Int32, Int32, Int32, CodegenAdd);
    public static FunctionSymbol Int32_Sub { get; } = Binary(TokenKind.Minus, Int32, Int32, Int32, CodegenSub);
    public static FunctionSymbol Int32_Mul { get; } = Binary(TokenKind.Star, Int32, Int32, Int32, CodegenMul);
    public static FunctionSymbol Int32_Div { get; } = Binary(TokenKind.Slash, Int32, Int32, Int32, CodegenDiv);
    public static FunctionSymbol Int32_Mod { get; } = Binary(TokenKind.KeywordMod, Int32, Int32, Int32, CodegenMod);
    public static FunctionSymbol Int32_Rem { get; } = Binary(TokenKind.KeywordRem, Int32, Int32, Int32, CodegenRem);

    // Float64

    public static FunctionSymbol Float64_Equal { get; } = Comparison(TokenKind.Equal, Float64, Float64, CodegenEqual);
    public static FunctionSymbol Float64_NotEqual { get; } = Comparison(TokenKind.NotEqual, Float64, Float64, CodegenNotEqual);
    public static FunctionSymbol Float64_GreaterThan { get; } = Comparison(TokenKind.GreaterThan, Float64, Float64, CodegenGreater);
    public static FunctionSymbol Float64_LessThan { get; } = Comparison(TokenKind.LessThan, Float64, Float64, CodegenLess);
    public static FunctionSymbol Float64_GreaterEqual { get; } = Comparison(TokenKind.GreaterEqual, Float64, Float64, CodegenGreaterEqual);
    public static FunctionSymbol Float64_LessEqual { get; } = Comparison(TokenKind.LessEqual, Float64, Float64, CodegenLessEqual);

    public static FunctionSymbol Float64_Plus { get; } = Unary(TokenKind.Plus, Float64, Float64, CodegenPlus);
    public static FunctionSymbol Float64_Minus { get; } = Unary(TokenKind.Minus, Float64, Float64, CodegenMinus);

    public static FunctionSymbol Float64_Add { get; } = Binary(TokenKind.Plus, Float64, Float64, Float64, CodegenAdd);
    public static FunctionSymbol Float64_Sub { get; } = Binary(TokenKind.Minus, Float64, Float64, Float64, CodegenSub);
    public static FunctionSymbol Float64_Mul { get; } = Binary(TokenKind.Star, Float64, Float64, Float64, CodegenMul);
    public static FunctionSymbol Float64_Div { get; } = Binary(TokenKind.Slash, Float64, Float64, Float64, CodegenDiv);
    public static FunctionSymbol Float64_Mod { get; } = Binary(TokenKind.KeywordMod, Float64, Float64, Float64, CodegenMod);
    public static FunctionSymbol Float64_Rem { get; } = Binary(TokenKind.KeywordRem, Float64, Float64, Float64, CodegenRem);

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
