using System;
using System.Collections.Immutable;
using System.Linq;
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
    public override TypeSymbol Type { get; }

    public SourceThisParameterSymbol(FunctionSymbol containingSymbol, ThisParameterSyntax syntax)
    {
        var containingType = containingSymbol.ContainingSymbol?.AncestorChain
            .OfType<TypeSymbol>()
            .FirstOrDefault()
            ?? throw new ArgumentException("the containing symbol of a source this parameter must be a function within a type");

        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
        this.Type = containingType;
    }

    public void Bind(IBinderProvider binderProvider) { }
}
