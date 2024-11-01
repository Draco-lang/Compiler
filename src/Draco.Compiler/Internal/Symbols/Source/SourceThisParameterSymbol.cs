using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

internal sealed class SourceThisParameterSymbol(FunctionSymbol containingSymbol, ThisParameterSyntax syntax) : ParameterSymbol, ISourceSymbol
{
    public override string Name => "this";
    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;
    public override bool IsVariadic => false;
    public override bool IsThis => true;
    public override ThisParameterSyntax DeclaringSyntax { get; } = syntax;

    public override ImmutableArray<AttributeInstance> Attributes => [];
    public override TypeSymbol Type { get; } = (containingSymbol.ContainingSymbol as TypeSymbol)!;

    public void Bind(IBinderProvider binderProvider) { }
}
