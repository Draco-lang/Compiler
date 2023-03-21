using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined, in-source value reference.
/// </summary>
internal sealed class UndefinedValueSymbol : Symbol, ITypedSymbol
{
    public override bool IsError => true;
    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name { get; }

    public Type Type => Intrinsics.Error;

    public UndefinedValueSymbol(string name)
    {
        this.Name = name;
    }

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.AnySymbol(this);
}
