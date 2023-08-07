using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols.Error;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal sealed class IntrinsicSymbols
{
    /// <summary>
    /// A utility for all intrinsic symbols.
    /// </summary>
    public ImmutableArray<Symbol> AllSymbols => InterlockedUtils.InitializeDefault(ref this.allSymbols, this.BuildAllSymbols);
    private ImmutableArray<Symbol> allSymbols;

    // Types that never change

    public static TypeSymbol Never => NeverTypeSymbol.Instance;
    public static TypeSymbol ErrorType { get; } = new ErrorTypeSymbol("<error>");
    public static TypeSymbol UninferredType { get; } = new ErrorTypeSymbol("?");
    public static TypeSymbol Unit { get; } = new PrimitiveTypeSymbol("unit", isValueType: true);

    // Types backed by metadata potentially, nonstatic

    public TypeSymbol Int8 { get; } = new PrimitiveTypeSymbol("int8", isValueType: true);
    public TypeSymbol Int16 { get; } = new PrimitiveTypeSymbol("int16", isValueType: true);
    public TypeSymbol Int32 { get; } = new PrimitiveTypeSymbol("int32", isValueType: true);
    public TypeSymbol Int64 { get; } = new PrimitiveTypeSymbol("int64", isValueType: true);

    public TypeSymbol UInt8 { get; } = new PrimitiveTypeSymbol("uint8", isValueType: true);
    public TypeSymbol UInt16 { get; } = new PrimitiveTypeSymbol("uint16", isValueType: true);
    public TypeSymbol UInt32 { get; } = new PrimitiveTypeSymbol("uint32", isValueType: true);
    public TypeSymbol UInt64 { get; } = new PrimitiveTypeSymbol("uint64", isValueType: true);

    public TypeSymbol Float32 { get; } = new PrimitiveTypeSymbol("float32", isValueType: true);
    public TypeSymbol Float64 { get; } = new PrimitiveTypeSymbol("float64", isValueType: true);

    public TypeSymbol Char { get; } = new PrimitiveTypeSymbol("char", isValueType: true);
    public TypeSymbol Bool { get; } = new PrimitiveTypeSymbol("bool", isValueType: true);

    public TypeSymbol Object { get; } = new PrimitiveTypeSymbol("object", isValueType: false);
    public TypeSymbol String { get; } = new PrimitiveTypeSymbol("string", isValueType: false);

    public ArrayTypeSymbol Array { get; }
    public ArrayConstructorSymbol ArrayCtor { get; }

    public FunctionSymbol Bool_Not => InterlockedUtils.InitializeNull(
        ref this.bool_not,
        () => Unary(TokenKind.KeywordNot, this.Bool, this.Bool, this.CodegenNot));
    private FunctionSymbol? bool_not;

    private readonly Compilation compilation;

    public IntrinsicSymbols(Compilation compilation)
    {
        this.compilation = compilation;
        this.Array = new(1, this.Int32);
        this.ArrayCtor = new(1);
    }

    public TypeSymbol InstantiateArray(TypeSymbol elementType, int rank = 1) => rank switch
    {
        1 => this.Array.GenericInstantiate(elementType),
        int n => new ArrayTypeSymbol(n, this.Int32).GenericInstantiate(elementType),
    };

    private ImmutableArray<Symbol> BuildAllSymbols() => typeof(IntrinsicSymbols)
        .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
        .Where(prop => prop.PropertyType.IsAssignableTo(typeof(Symbol)))
        .Select(prop => prop.GetValue(this))
        .Cast<Symbol>()
        .Concat(this.GenerateIntrinsicSymbols())
        .ToImmutableArray();

    private IEnumerable<Symbol> GenerateIntrinsicSymbols()
    {
        // Array types from 2D to 8D
        for (var i = 2; i <= 8; ++i)
        {
            // Type
            yield return new ArrayTypeSymbol(i, this.Int32);
            // Ctor
            yield return new ArrayConstructorSymbol(i);
        }

        // Numeric operators
        foreach (var type in new[]
        {
            this.Int8, this.Int16, this.Int32, this.Int64,
            this.UInt8, this.UInt16, this.UInt32, this.UInt64,
            this.Float32, this.Float64,
        })
        {
            // Comparison
            yield return Comparison(TokenKind.Equal, type, type, this.CodegenEqual);
            yield return Comparison(TokenKind.NotEqual, type, type, this.CodegenNotEqual);
            yield return Comparison(TokenKind.GreaterThan, type, type, this.CodegenGreater);
            yield return Comparison(TokenKind.LessThan, type, type, this.CodegenLess);
            yield return Comparison(TokenKind.GreaterEqual, type, type, this.CodegenGreaterEqual);
            yield return Comparison(TokenKind.LessEqual, type, type, this.CodegenLessEqual);

            // Unary
            yield return Unary(TokenKind.Plus, type, type, this.CodegenPlus);
            yield return Unary(TokenKind.Minus, type, type, this.CodegenMinus);

            // Binary
            yield return Binary(TokenKind.Plus, type, type, type, this.CodegenAdd);
            yield return Binary(TokenKind.Minus, type, type, type, this.CodegenSub);
            yield return Binary(TokenKind.Star, type, type, type, this.CodegenMul);
            yield return Binary(TokenKind.Slash, type, type, type, this.CodegenDiv);
            yield return Binary(TokenKind.KeywordMod, type, type, type, this.CodegenMod);
            yield return Binary(TokenKind.KeywordRem, type, type, type, this.CodegenRem);
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

    // Codegen

    private void CodegenPlus(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // No-op
    }

    private void CodegenMinus(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Mul(target, operands[0], new Constant(-1, this.Int32)));

    private void CodegenNot(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Equal(target, operands[0], new Constant(false, this.Bool)));

    private void CodegenAdd(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Add(target, operands[0], operands[1]));

    private void CodegenSub(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Sub(target, operands[0], operands[1]));

    private void CodegenMul(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Mul(target, operands[0], operands[1]));

    private void CodegenDiv(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Div(target, operands[0], operands[1]));

    private void CodegenRem(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Rem(target, operands[0], operands[1]));

    private void CodegenMod(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
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

    private void CodegenLess(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Less(target, operands[0], operands[1]));

    private void CodegenGreater(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        // a > b
        //  <=>
        // b < a
        codegen.Write(Less(target, operands[1], operands[0]));

    private void CodegenLessEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a <= b
        //  <=>
        // (b < a) == false
        var tmp = codegen.DefineRegister(this.Bool);
        codegen.Write(Less(tmp, operands[1], operands[0]));
        codegen.Write(Equal(target, tmp, new Constant(false, this.Bool)));
    }

    private void CodegenGreaterEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a >= b
        //  <=>
        // (a < b) == false
        var tmp = codegen.DefineRegister(this.Bool);
        codegen.Write(Less(tmp, operands[0], operands[1]));
        codegen.Write(Equal(target, tmp, new Constant(false, this.Bool)));
    }

    private void CodegenEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Equal(target, operands[0], operands[1]));

    private void CodegenNotEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a != b
        //  <=>
        // (a == b) == false
        var tmp = codegen.DefineRegister(this.Bool);
        codegen.Write(Equal(tmp, operands[0], operands[1]));
        codegen.Write(Equal(target, tmp, new Constant(false, this.Bool)));
    }
}
