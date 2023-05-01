using System;
using System.Collections.Immutable;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A synthetized function that loads in lazily.
/// </summary>
internal sealed class LazySynthetizedFunctionSymbol : SynthetizedFunctionSymbol
{
    // TODO
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => throw new NotImplementedException();

    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.parametersBuilder(this);
    private ImmutableArray<ParameterSymbol>? parameters;

    public override TypeSymbol ReturnType => this.returnType ??= this.returnTypeBuilder(this);
    private TypeSymbol? returnType;

    public override BoundStatement Body => this.body ??= this.bodyBuilder(this);
    private BoundStatement? body;

    private readonly Func<FunctionSymbol, ImmutableArray<ParameterSymbol>> parametersBuilder;
    private readonly Func<FunctionSymbol, TypeSymbol> returnTypeBuilder;
    private readonly Func<FunctionSymbol, BoundStatement> bodyBuilder;

    public LazySynthetizedFunctionSymbol(
        string name,
        Func<FunctionSymbol, ImmutableArray<ParameterSymbol>> parametersBuilder,
        Func<FunctionSymbol, TypeSymbol> returnTypeBuilder,
        Func<FunctionSymbol, BoundStatement> bodyBuilder)
        : base(name)
    {
        this.parametersBuilder = parametersBuilder;
        this.returnTypeBuilder = returnTypeBuilder;
        this.bodyBuilder = bodyBuilder;
    }
}
