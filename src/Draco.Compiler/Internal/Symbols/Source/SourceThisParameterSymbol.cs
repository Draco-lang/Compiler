using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// The 'this' parameter of a function defined in-source.
/// </summary>
internal sealed class SourceThisParameterSymbol : ParameterSymbol, ISourceSymbol
{
    public override string Name => "this";
    public override FunctionSymbol ContainingSymbol { get; }
    public override bool IsVariadic => false;
    public override bool IsThis => true;
    public override ThisParameterSyntax DeclaringSyntax { get; }

    public override ImmutableArray<AttributeInstance> Attributes => [];

    public override TypeSymbol Type => LazyInitializer.EnsureInitialized(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public SourceThisParameterSymbol(FunctionSymbol containingSymbol, ThisParameterSyntax syntax)
    {
        var containingType = containingSymbol.ContainingSymbol?.AncestorChain
            .OfType<TypeSymbol>()
            .FirstOrDefault()
            ?? throw new ArgumentException("the containing symbol of a source this parameter must be a function within a type");

        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    public void Bind(IBinderProvider binderProvider) { }

    private TypeSymbol BuildType()
    {
        var containingType = this.ContainingSymbol.AncestorChain
            .OfType<TypeSymbol>()
            .First();

        if (!containingType.IsGenericDefinition) return containingType;

        var genericArgs = containingType.GenericParameters.Cast<TypeSymbol>().ToImmutableArray();
        return containingType.GenericInstantiate(containingType.ContainingSymbol, genericArgs);
    }
}
