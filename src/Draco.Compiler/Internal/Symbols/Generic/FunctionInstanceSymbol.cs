using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated function.
/// It does not necessarily mean that the function itself was generic, it might have been within another generic
/// context (like a generic type definition).
/// </summary>
internal sealed class FunctionInstanceSymbol : FunctionSymbol, IGenericInstanceSymbol
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

    public override string Name => this.GenericDefinition.Name;
    public override bool IsMember => this.GenericDefinition.IsMember;
    public override bool IsVirtual => this.GenericDefinition.IsVirtual;

    public override Symbol? ContainingSymbol { get; }
    public override FunctionSymbol GenericDefinition { get; }

    private bool NeedsGenericsBuild => this.genericParameters is null;

    public GenericContext Context { get; }

    public FunctionInstanceSymbol(Symbol? containingSymbol, FunctionSymbol genericDefinition, GenericContext context)
    {
        this.ContainingSymbol = containingSymbol;
        this.GenericDefinition = genericDefinition;
        this.Context = context;
    }

    public override FunctionSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context) =>
        throw new NotImplementedException();

    public override string ToString()
    {
        // We have generic args, add those
        if (this.GenericArguments.Length > 0)
        {
            var result = new StringBuilder();
            result.Append($"{this.Name}<");
            result.AppendJoin(", ", this.GenericArguments);
            result.Append(">(");
            result.AppendJoin(", ", this.Parameters);
            result.Append($"): {this.ReturnType}");
            return result.ToString();
        }
        // Either way:
        //  - We have generic parameters, this is still a generic definition
        //  - Non-generic
        // 
        return base.ToString();
    }

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
        var hasParametersSpecified = this.GenericDefinition.GenericParameters.Any(this.Context.ContainsKey);

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
            .Select(param => this.Context[param])
            .ToImmutableArray();
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.GenericDefinition.Parameters
        .Select(p => p.GenericInstantiate(this, this.Context))
        .ToImmutableArray();

    private TypeSymbol BuildReturnType() =>
        this.GenericDefinition.ReturnType.GenericInstantiate(this.GenericDefinition.ReturnType.ContainingSymbol, this.Context);
}
