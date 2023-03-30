namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// Represents a type reference.
/// </summary>
public interface IType
{
}

internal sealed class Type : IType
{
    private readonly Internal.Types.Type internalType;

    public Type(Internal.Types.Type internalType)
    {
        this.internalType = internalType;
    }

    public override string ToString() => this.internalType.ToString();
}
