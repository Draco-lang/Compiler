namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// Represents no location.
/// </summary>
internal sealed class NullLocation : Location
{
    public override bool IsNone => true;

    public override string ToString() => "<no location>";
}
