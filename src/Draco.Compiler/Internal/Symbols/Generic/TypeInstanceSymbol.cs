using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated type.
/// It does not necessarily mean that the type itself was generic, it might have been within another generic
/// context.
/// </summary>
internal sealed class TypeInstanceSymbol : TypeSymbol
{
    // TODO: One-to-one copy from FunctionInstanceSymbol...
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

    public override IEnumerable<Symbol> Members => this.members ??= this.BuildMembers();
    private ImmutableArray<Symbol>? members;

    public override bool IsTypeVariable => this.GenericDefinition.IsTypeVariable;
    public override bool IsValueType => this.GenericDefinition.IsValueType;
    public override string Name => this.GenericDefinition.Name;

    public override Symbol? ContainingSymbol => this.GenericDefinition.ContainingSymbol;

    public override TypeSymbol GenericDefinition { get; }

    private bool NeedsGenericsBuild => this.genericParameters is null;

    private readonly GenericContext context;

    public TypeInstanceSymbol(TypeSymbol genericDefinition, GenericContext context)
    {
        this.GenericDefinition = genericDefinition;
        this.context = context;
    }

    public override string ToString()
    {
        // We have generic args, add those
        if (this.GenericArguments.Length > 0)
        {
            var result = new StringBuilder();
            result.Append($"{this.Name}<");
            result.AppendJoin(", ", this.GenericArguments);
            result.Append('>');
            return result.ToString();
        }
        // Either way:
        //  - We have generic parameters, this is still a generic definition
        //  - Non-generic
        // 
        return this.GenericDefinition.ToString();
    }

    // TODO: One-to-one copy from FunctionInstanceSymbol...
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

    private ImmutableArray<Symbol> BuildMembers() => this.GenericDefinition.Members
        .Select(m => m.GenericInstantiate(this.context))
        .ToImmutableArray();
}
