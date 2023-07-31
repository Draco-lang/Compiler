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

    public override IEnumerable<Symbol> DefinedMembers =>
        InterlockedUtils.InitializeDefault(ref this.definedMembers, this.BuildDefinedMembers);
    private ImmutableArray<Symbol> definedMembers;

    public override ImmutableArray<TypeSymbol> ImmediateBaseTypes => this.GenericDefinition.ImmediateBaseTypes
        .Select(x => x.GenericInstantiate(x.ContainingSymbol, this.Context))
        .ToImmutableArray();
    public override bool IsTypeVariable => this.GenericDefinition.IsTypeVariable;
    public override bool IsValueType => this.GenericDefinition.IsValueType;
    public override bool IsInterface => this.GenericDefinition.IsInterface;
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
        var newContext = this.Context.Merge(context);
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

    private ImmutableArray<Symbol> BuildDefinedMembers() => this.GenericDefinition.DefinedMembers
        .Select(m => m.GenericInstantiate(this, this.Context))
        .ToImmutableArray();
}
