using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated function.
/// It does not necessarily mean that the function itself was generic, it might have been within another generic
/// context (like a generic type definition).
/// </summary>
internal sealed class FunctionInstanceSymbol : FunctionSymbol
{
    public override ImmutableArray<TypeParameterSymbol> GenericParameters
    {
        get
        {
            if (this.NeedsGenericsBuild) this.BuildGenerics();
            return this.genericParameters!.Value;
        }
    }
    public override ImmutableArray<TypeSymbol> GenericArguments
    {
        get
        {
            if (this.NeedsGenericsBuild) this.BuildGenerics();
            return this.genericArguments!.Value;
        }
    }

    private ImmutableArray<TypeSymbol>? genericArguments;
    private ImmutableArray<TypeParameterSymbol>? genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.BuildParameters();
    private ImmutableArray<ParameterSymbol>? parameters;

    public override TypeSymbol ReturnType => this.returnType ??= this.BuildReturnType();
    private TypeSymbol? returnType;

    public override bool IsMember => this.GenericDefinition.IsMember;
    public override bool IsVirtual => this.GenericDefinition.IsVirtual;

    public override Symbol? ContainingSymbol => this.GenericDefinition.ContainingSymbol;
    public override FunctionSymbol GenericDefinition { get; }

    private bool NeedsGenericsBuild => this.genericParameters is null;

    private readonly GenericContext context;

    public FunctionInstanceSymbol(FunctionSymbol genericDefinition, GenericContext context)
    {
        this.GenericDefinition = genericDefinition;
        this.context = context;
    }

    public override FunctionSymbol GenericInstantiate(GenericContext context) =>
        throw new NotImplementedException();

    private void BuildGenerics()
    {
        // If the definition wasn't generic, we just carry over the context
        if (!this.GenericDefinition.IsGenericDefinition)
        {
            this.genericParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            this.genericArguments = ImmutableArray<TypeSymbol>.Empty;
            return;
        }

        // Check, if we have parameters in there
        var hasParametersSpecified = this.GenericDefinition.GenericParameters.Any(this.context.ContainsKey);

        // If the parameters are not specified, we have the same old generic params
        if (!hasParametersSpecified)
        {
            this.genericParameters = this.GenericDefinition.GenericParameters;
            this.genericArguments = ImmutableArray<TypeSymbol>.Empty;
            return;
        }

        // Otherwise, this must have been substituted
        this.genericParameters = ImmutableArray<TypeParameterSymbol>.Empty;
        this.genericArguments = this.GenericDefinition.GenericParameters
            .Select(param => this.context[param])
            .ToImmutableArray();
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.GenericDefinition.Parameters
        .Select(p => p.GenericInstantiate(this.context))
        .ToImmutableArray();

    private TypeSymbol BuildReturnType() => this.GenericDefinition.ReturnType.GenericInstantiate(this.context);
}
