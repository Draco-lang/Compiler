namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents the bottom-type.
/// </summary>
internal sealed class NeverType : Type
{
    public static NeverType Instance { get; } = new();

    private NeverType()
    {
    }

    public override string ToString() => "<never>";
}
