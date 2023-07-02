using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated type.
/// It does not necessarily mean that the type itself was generic, it might have been within another generic
/// context.
/// </summary>
internal sealed class TypeInstanceSymbol : TypeSymbol, IGenericInstanceSymbol
{
    // TODO: One-to-one copy from FunctionInstanceSymbol...
    public override ImmutableArray<TypeParameterSymbol> GenericParameters
    {
        get
        {
            if (!this.genericsNeedsBuild) return this.genericParameters;
            lock (this.genericsBuildLock)
            {
                if (this.genericsNeedsBuild) this.BuildGenerics();
                return this.genericParameters;
            }
        }
    }
    public override ImmutableArray<TypeSymbol> GenericArguments
    {
        get
        {
            if (!this.genericsNeedsBuild) return this.genericArguments;
            lock (this.genericsBuildLock)
            {
                if (this.genericsNeedsBuild) this.BuildGenerics();
                return this.genericArguments;
            }
        }
    }

    private ImmutableArray<TypeSymbol> genericArguments;
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override IEnumerable<Symbol> Members =>
        InterlockedUtils.InitializeDefault(ref this.members, this.BuildMembers);
    private ImmutableArray<Symbol> members;

    public override ImmutableArray<TypeSymbol> BaseTypes => this.GenericDefinition.BaseTypes;
    public override bool IsTypeVariable => this.GenericDefinition.IsTypeVariable;
    public override bool IsValueType => this.GenericDefinition.IsValueType;
    public override string Name => this.GenericDefinition.Name;

    public override Symbol? ContainingSymbol { get; }

    public override TypeSymbol GenericDefinition { get; }

    // IMPORTANT: Flag is a bool and not computed because we can't atomically write structs
    private volatile bool genericsNeedsBuild = true;
    private readonly object genericsBuildLock = new();

    public GenericContext Context { get; }

    public TypeInstanceSymbol(Symbol? containingSymbol, TypeSymbol genericDefinition, GenericContext context)
    {
        this.ContainingSymbol = containingSymbol;
        this.GenericDefinition = genericDefinition;
        this.Context = context;
    }

    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, ImmutableArray<TypeSymbol> arguments) =>
        (TypeSymbol)base.GenericInstantiate(containingSymbol, arguments);
    public override TypeSymbol GenericInstantiate(Symbol? containingSymbol, GenericContext context)
    {
        // We need to merge contexts
        var substitutions = ImmutableDictionary.CreateBuilder<TypeParameterSymbol, TypeSymbol>();
        substitutions.AddRange(this.Context);
        // Go through existing substitutions and where we have X -> Y in the old, Y -> Z in the new,
        // replace with X -> Z
        foreach (var (typeParam, typeSubst) in this.Context)
        {
            if (typeSubst is not TypeParameterSymbol paramSubst) continue;
            if (context.TryGetValue(paramSubst, out var prunedSubst))
            {
                substitutions[typeParam] = prunedSubst;
            }
        }
        // Add the rest
        substitutions.AddRange(context);
        // Done merging
        var newContext = new GenericContext(substitutions.ToImmutable());
        return new TypeInstanceSymbol(containingSymbol, this.GenericDefinition, newContext);
    }

    // TODO: Almost one-to-one copy from FunctionInstanceSymbol...
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
            // IMPORTANT: Flag is written last
            this.genericsNeedsBuild = false;
            return;
        }

        // Check, if we have parameters in there
        var hasParametersSpecified = this.GenericDefinition.GenericParameters.Any(this.Context.ContainsKey);

        // If the parameters are not specified, we have the same old generic params
        if (!hasParametersSpecified)
        {
            this.genericParameters = this.GenericDefinition.GenericParameters;
            this.genericArguments = ImmutableArray<TypeSymbol>.Empty;
            // IMPORTANT: Flag is written last
            this.genericsNeedsBuild = false;
            return;
        }

        // Otherwise, this must have been substituted
        this.genericParameters = ImmutableArray<TypeParameterSymbol>.Empty;
        this.genericArguments = this.GenericDefinition.GenericParameters
            .Select(param => this.Context[param])
            .ToImmutableArray();
        // IMPORTANT: Flag is written last
        this.genericsNeedsBuild = false;
    }

    private ImmutableArray<Symbol> BuildMembers() => this.GenericDefinition.Members
        .Select(m => m.GenericInstantiate(this, this.Context))
        .ToImmutableArray();
}
