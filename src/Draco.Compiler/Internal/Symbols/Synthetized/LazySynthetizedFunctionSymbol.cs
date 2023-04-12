using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A synthetized function that loads in lazily.
/// </summary>
internal sealed class LazySynthetizedFunctionSymbol : SynthetizedFunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.parameters;
        }
    }
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.returnType!;
        }
    }
    private TypeSymbol? returnType;

    public override BoundStatement Body
    {
        get
        {
            if (this.NeedsBuild) this.Build();
            return this.body!;
        }
    }
    private BoundStatement? body;

    private bool NeedsBuild => this.returnType is null;

    private readonly Func<(
        ImmutableArray<ParameterSymbol> Parameters,
        TypeSymbol ReturnType,
        BoundStatement Body)> builder;

    public LazySynthetizedFunctionSymbol(
        string name,
        Func<(
            ImmutableArray<ParameterSymbol> Parameters,
            TypeSymbol ReturnType,
            BoundStatement Body)> builder)
        : base(name)
    {
        this.builder = builder;
    }

    private void Build()
    {
        var (parameters, returnType, body) = this.builder();
        this.parameters = parameters;
        this.returnType = returnType;
        this.body = body;
    }
}
