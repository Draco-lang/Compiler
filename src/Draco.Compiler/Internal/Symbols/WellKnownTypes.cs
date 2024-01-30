using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;
using System;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A collection of well-known types that the compiler needs.
/// </summary>
internal sealed partial class WellKnownTypes
{
    #region AllSymbols Helper
    /// <summary>
    /// A utility for all intrinsic symbols.
    /// </summary>
    public ImmutableArray<Symbol> AllSymbols => InterlockedUtils.InitializeDefault(ref this.allSymbols, this.BuildAllSymbols);
    private ImmutableArray<Symbol> allSymbols;
    private ImmutableArray<Symbol> BuildAllSymbols() =>
        this.GenerateWellKnownTypes().ToImmutableArray();

    // NOTE: We don't yield each primitive directly, we need to alias them
    private IEnumerable<Symbol> GenerateWellKnownTypes()
    {
        // Primitive aliases
        yield return Alias("char", this.SystemChar);
        yield return Alias("bool", this.SystemBoolean);

        yield return Alias("uint8", this.SystemByte);
        yield return Alias("uint16", this.SystemUInt16);
        yield return Alias("uint32", this.SystemUInt32);
        yield return Alias("uint64", this.SystemUInt64);

        yield return Alias("int8", this.SystemByte);
        yield return Alias("int16", this.SystemInt16);
        yield return Alias("int32", this.SystemInt32);
        yield return Alias("int64", this.SystemInt64);

        yield return Alias("float32", this.SystemSingle);
        yield return Alias("float64", this.SystemDouble);

        yield return Alias("string", this.SystemString);
        yield return Alias("object", this.SystemObject);

        // 1D array
        yield return this.ArrayType;
        yield return this.ArrayCtor;

        // Array types from 2D to 8D
        for (var i = 2; i <= 8; ++i)
        {
            // Type
            var arrayType = new ArrayTypeSymbol(i, this.SystemInt32);
            yield return arrayType;
            // Ctor
            yield return new ArrayConstructorSymbol(arrayType);
        }

        // Bool negation
        yield return this.Bool_Not;

        // Numeric operators
        foreach (var type in new[]
        {
         this.SystemSByte, this.SystemInt16, this.SystemInt32, this.SystemInt64,
         this.SystemByte, this.SystemUInt16, this.SystemUInt32, this.SystemUInt64,
         this.SystemSingle, this.SystemDouble,
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

        // The operators that make sense for characters too
        {
            yield return this.Comparison(TokenKind.Equal, this.SystemChar, this.SystemChar, this.CodegenEqual);
            yield return this.Comparison(TokenKind.NotEqual, this.SystemChar, this.SystemChar, this.CodegenNotEqual);
            yield return this.Comparison(TokenKind.GreaterThan, this.SystemChar, this.SystemChar, this.CodegenGreater);
            yield return this.Comparison(TokenKind.LessThan, this.SystemChar, this.SystemChar, this.CodegenLess);
            yield return this.Comparison(TokenKind.GreaterEqual, this.SystemChar, this.SystemChar, this.CodegenGreaterEqual);
            yield return this.Comparison(TokenKind.LessEqual, this.SystemChar, this.SystemChar, this.CodegenLessEqual);
        }

        // Add additional symbols for some metadata builtins
        foreach (var metadataType in new[] { this.WellKnownTypes.SystemString })
        {
            foreach (var additionalSym in metadataType.AdditionalSymbols) yield return additionalSym;
        }

        // String addition
        yield return this.Binary(TokenKind.Plus, this.String, this.String, this.String, (codegen, target, operands) =>
            codegen.Write(Call(target, this.WellKnownTypes.SystemString_Concat, operands)));
    }

    private static TypeAliasSymbol Alias(string name, TypeSymbol type) =>
        new SynthetizedTypeAliasSymbol(name, type);
    #endregion

    #region Singletons
    public static TypeSymbol Never => NeverTypeSymbol.Instance;
    public static TypeSymbol ErrorType { get; } = new ErrorTypeSymbol("<error>");
    public static TypeSymbol UninferredType { get; } = new ErrorTypeSymbol("?");
    public static TypeSymbol Unit { get; } = new PrimitiveTypeSymbol("unit", isValueType: true);
    #endregion

    #region Methods
    /// <summary>
    /// object.ToString().
    /// </summary>
    public MetadataMethodSymbol SystemObject_ToString => InterlockedUtils.InitializeNull(
        ref this.object_ToString,
        () => this.SystemObject
            .Members
            .OfType<MetadataMethodSymbol>()
            .Single(m => m.Name == "ToString"));
    private MetadataMethodSymbol? object_ToString;

    /// <summary>
    /// string.Format(string formatString, object[] args).
    /// </summary>
    public MetadataMethodSymbol SystemString_Format => InterlockedUtils.InitializeNull(
        ref this.systemString_Format,
        () => this.SystemString
            .Members
            .OfType<MetadataMethodSymbol>()
            .First(m =>
                m.Name == "Format"
             && m.Parameters is [_, { Type: TypeInstanceSymbol { GenericDefinition: ArrayTypeSymbol } }]));
    private MetadataMethodSymbol? systemString_Format;

    /// <summary>
    /// string.Concat(string str1, string str2).
    /// </summary>
    public MetadataMethodSymbol SystemString_Concat => InterlockedUtils.InitializeNull(
        ref this.systemString_Concat,
        () => this.SystemString
            .Members
            .OfType<MetadataMethodSymbol>()
            .First(m =>
                m.Name == "Concat"
             && m.Parameters.Length == 2
             && m.Parameters.All(p => SymbolEqualityComparer.Default.Equals(p.Type, this.SystemString))));
    private MetadataMethodSymbol? systemString_Concat;

    public TypeSymbol InstantiateArray(TypeSymbol elementType, int rank = 1) => rank switch
    {
        1 => this.ArrayType.GenericInstantiate(elementType),
        int n => new ArrayTypeSymbol(n, this.SystemInt32).GenericInstantiate(elementType),
    };
    #endregion

    private readonly Compilation compilation;

    public WellKnownTypes(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public ArrayTypeSymbol ArrayType => InterlockedUtils.InitializeNull(ref this.array, () => new(1, this.SystemInt32));
    private ArrayTypeSymbol? array;

    public ArrayConstructorSymbol ArrayCtor => InterlockedUtils.InitializeNull(ref this.arrayCtor, () => new(this.ArrayType));
    private ArrayConstructorSymbol? arrayCtor;


    #region Loader Methods
    public MetadataTypeSymbol GetTypeFromAssembly(AssemblyName name, ImmutableArray<string> path)
    {
        var assembly = this.GetAssemblyWithAssemblyName(name);
        return this.GetTypeFromAssembly(assembly, path);
    }

    public MetadataTypeSymbol GetTypeFromAssembly(MetadataAssemblySymbol assembly, ImmutableArray<string> path) =>
        assembly.Lookup(path).OfType<MetadataTypeSymbol>().Single();

    private MetadataAssemblySymbol GetAssemblyWithAssemblyName(AssemblyName name) =>
        this.compilation.MetadataAssemblies.Values.Single(asm => AssemblyNameComparer.Full.Equals(asm.AssemblyName, name));

    private MetadataAssemblySymbol GetAssemblyWithNameAndToken(string name, byte[] token)
    {
        var assemblyName = new AssemblyName() { Name = name };
        assemblyName.SetPublicKeyToken(token);
        return this.compilation.MetadataAssemblies.Values
            .SingleOrDefault(asm => AssemblyNameComparer.NameAndToken.Equals(asm.AssemblyName, assemblyName))
            ?? throw new InvalidOperationException($"Failed to locate assembly with name '{name}' and public key token '{BitConverter.ToString(token)}'.");
    }

    #endregion

    #region Operators

    private FunctionSymbol Unary(
        TokenKind token,
        TypeSymbol operandType,
        TypeSymbol returnType,
        FunctionSymbol.CodegenDelegate codegen) =>
        DelegateIrFunctionSymbol.UnaryOperator(token, operandType, returnType, codegen);
    private FunctionSymbol Binary(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        TypeSymbol returnType,
        FunctionSymbol.CodegenDelegate codegen) =>
        DelegateIrFunctionSymbol.BinaryOperator(token, leftType, rightType, returnType, codegen);
    private FunctionSymbol Comparison(
        TokenKind token,
        TypeSymbol leftType,
        TypeSymbol rightType,
        FunctionSymbol.CodegenDelegate codegen) =>
        DelegateIrFunctionSymbol.ComparisonOperator(token, leftType, rightType, this.SystemBoolean, codegen);

    public FunctionSymbol Bool_Not => InterlockedUtils.InitializeNull(
       ref this.bool_not,
       () => this.Unary(TokenKind.KeywordNot, this.SystemBoolean, this.SystemBoolean, this.CodegenNot));
    private FunctionSymbol? bool_not;
    #endregion

    #region Codegen Methods

    private void CodegenPlus(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // No-op
    }

    private void CodegenMinus(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Mul(target, operands[0], new Constant(-1, this.SystemInt32)));

    private void CodegenNot(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Equal(target, operands[0], new Constant(false, this.SystemBoolean)));

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
        var tmp = codegen.DefineRegister(this.SystemBoolean);
        codegen.Write(Less(tmp, operands[1], operands[0]));
        codegen.Write(Equal(target, tmp, new Constant(false, this.SystemBoolean)));
    }

    private void CodegenGreaterEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a >= b
        //  <=>
        // (a < b) == false
        var tmp = codegen.DefineRegister(this.SystemBoolean);
        codegen.Write(Less(tmp, operands[0], operands[1]));
        codegen.Write(Equal(target, tmp, new Constant(false, this.SystemBoolean)));
    }

    private void CodegenEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands) =>
        codegen.Write(Equal(target, operands[0], operands[1]));

    private void CodegenNotEqual(FunctionBodyCodegen codegen, Register target, ImmutableArray<IOperand> operands)
    {
        // a != b
        //  <=>
        // (a == b) == false
        var tmp = codegen.DefineRegister(this.SystemBoolean);
        codegen.Write(Equal(tmp, operands[0], operands[1]));
        codegen.Write(Equal(target, tmp, new Constant(false, this.SystemBoolean)));
    }
    #endregion
}
