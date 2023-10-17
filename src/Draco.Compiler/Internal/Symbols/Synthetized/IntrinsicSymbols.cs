using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Intrinsic symbols.
/// </summary>
internal sealed partial class IntrinsicSymbols
{
    /// <summary>
    /// A utility for all intrinsic symbols.
    /// </summary>
    public ImmutableArray<Symbol> AllSymbols => InterlockedUtils.InitializeDefault(ref this.allSymbols, this.BuildAllSymbols);
    private ImmutableArray<Symbol> allSymbols;

    // Types that never change

    public static TypeSymbol Never => WellKnownTypes.Never;
    public static TypeSymbol ErrorType => WellKnownTypes.ErrorType;
    public static TypeSymbol UninferredType => WellKnownTypes.UninferredType;
    public static TypeSymbol Unit => WellKnownTypes.Unit;

    // Forwarded from metadata

    public TypeSymbol Char => this.WellKnownTypes.SystemChar;
    public TypeSymbol Bool => this.WellKnownTypes.SystemBoolean;

    public TypeSymbol Uint8 => this.WellKnownTypes.SystemByte;
    public TypeSymbol Uint16 => this.WellKnownTypes.SystemUInt16;
    public TypeSymbol Uint32 => this.WellKnownTypes.SystemUInt32;
    public TypeSymbol Uint64 => this.WellKnownTypes.SystemUInt64;

    public TypeSymbol Int8 => this.WellKnownTypes.SystemSByte;
    public TypeSymbol Int16 => this.WellKnownTypes.SystemInt16;
    public TypeSymbol Int32 => this.WellKnownTypes.SystemInt32;
    public TypeSymbol Int64 => this.WellKnownTypes.SystemInt64;

    public TypeSymbol Float32 => this.WellKnownTypes.SystemSingle;
    public TypeSymbol Float64 => this.WellKnownTypes.SystemDouble;

    public TypeSymbol String => this.WellKnownTypes.SystemString;
    public TypeSymbol Object => this.WellKnownTypes.SystemObject;

    // Generated

    public ArrayTypeSymbol Array => InterlockedUtils.InitializeNull(ref this.array, () => new(1, this.Int32));
    private ArrayTypeSymbol? array;

    public ArrayConstructorSymbol ArrayCtor => InterlockedUtils.InitializeNull(ref this.arrayCtor, () => new(this.Array));
    private ArrayConstructorSymbol? arrayCtor;

    public FunctionSymbol Bool_Not => InterlockedUtils.InitializeNull(
        ref this.bool_not,
        () => this.Unary(TokenKind.KeywordNot, this.Bool, this.Bool, this.CodegenNot));
    private FunctionSymbol? bool_not;

    private WellKnownTypes WellKnownTypes => this.compilation.WellKnownTypes;

    private readonly Compilation compilation;

    public IntrinsicSymbols(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public TypeSymbol InstantiateArray(TypeSymbol elementType, int rank = 1) => rank switch
    {
        1 => this.Array.GenericInstantiate(elementType),
        int n => new ArrayTypeSymbol(n, this.Int32).GenericInstantiate(elementType),
    };

    private ImmutableArray<Symbol> BuildAllSymbols() =>
        this.GenerateIntrinsicSymbols().ToImmutableArray();

    // NOTE: We don't yield each primitive directly, we need to alias them
    private IEnumerable<Symbol> GenerateIntrinsicSymbols()
    {
        // Primitive aliases
        yield return Alias("char", this.Char);
        yield return Alias("bool", this.Bool);

        yield return Alias("uint8", this.Uint8);
        yield return Alias("uint16", this.Uint16);
        yield return Alias("uint32", this.Uint32);
        yield return Alias("uint64", this.Uint64);

        yield return Alias("int8", this.Int8);
        yield return Alias("int16", this.Int16);
        yield return Alias("int32", this.Int32);
        yield return Alias("int64", this.Int64);

        yield return Alias("float32", this.Float32);
        yield return Alias("float64", this.Float64);

        yield return Alias("string", this.String);
        yield return Alias("object", this.Object);

        // 1D array
        yield return this.Array;
        yield return this.ArrayCtor;

        // Array types from 2D to 8D
        for (var i = 2; i <= 8; ++i)
        {
            // Type
            var arrayType = new ArrayTypeSymbol(i, this.Int32);
            yield return arrayType;
            // Ctor
            yield return new ArrayConstructorSymbol(arrayType);
        }

        // Bool negation
        yield return this.Bool_Not;

        // Numeric operators
        foreach (var type in new[]
        {
            this.Int8, this.Int16, this.Int32, this.Int64,
            this.Uint8, this.Uint16, this.Uint32, this.Uint64,
            this.Float32, this.Float64,
        })
        {
            // Comparison
            yield return this.Comparison(TokenKind.Equal, type, type, this.CodegenEqual);
            yield return this.Comparison(TokenKind.NotEqual, type, type, this.CodegenNotEqual);
            yield return this.Comparison(TokenKind.GreaterThan, type, type, this.CodegenGreater);
            yield return this.Comparison(TokenKind.LessThan, type, type, this.CodegenLess);
            yield return this.Comparison(TokenKind.GreaterEqual, type, type, this.CodegenGreaterEqual);
            yield return this.Comparison(TokenKind.LessEqual, type, type, this.CodegenLessEqual);

            // Unary
            yield return this.Unary(TokenKind.Plus, type, type, this.CodegenPlus);
            yield return this.Unary(TokenKind.Minus, type, type, this.CodegenMinus);

            // Binary
            yield return this.Binary(TokenKind.Plus, type, type, type, this.CodegenAdd);
            yield return this.Binary(TokenKind.Minus, type, type, type, this.CodegenSub);
            yield return this.Binary(TokenKind.Star, type, type, type, this.CodegenMul);
            yield return this.Binary(TokenKind.Slash, type, type, type, this.CodegenDiv);
            yield return this.Binary(TokenKind.KeywordMod, type, type, type, this.CodegenMod);
            yield return this.Binary(TokenKind.KeywordRem, type, type, type, this.CodegenRem);
        }
    }

    private static TypeAliasSymbol Alias(string name, TypeSymbol type) =>
        new SynthetizedTypeAliasSymbol(name, type);

    // Operators

    private FunctionSymbol Unary(
        TokenKind token,
        TypeSymbol operandType,
        TypeSymbol returnType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        DelegateIrFunctionSymbol.UnaryOperator(token, operandType, returnType, codegen);
    private FunctionSymbol Binary(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        TypeSymbol returnType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        DelegateIrFunctionSymbol.BinaryOperator(token, leftType, rightType, returnType, codegen);
    private FunctionSymbol Comparison(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        IrFunctionSymbol.CodegenDelegate codegen) =>
        DelegateIrFunctionSymbol.ComparisonOperator(token, leftType, rightType, this.Bool, codegen);

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
