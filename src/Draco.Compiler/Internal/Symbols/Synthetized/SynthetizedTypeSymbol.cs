using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A type symbol constructed by the compiler.
/// </summary>
internal sealed class SynthetizedTypeSymbol : TypeSymbol
{
    public override string Name { get; }
    public override Type Type { get; }

    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public SynthetizedTypeSymbol(string name, Type type)
    {
        this.Name = name;
        this.Type = type;
    }

    public SynthetizedTypeSymbol(BuiltinType type)
        : this(type.Name, type)
    {
    }

    public override ISymbol ToApiSymbol() => throw new System.NotImplementedException();
}
