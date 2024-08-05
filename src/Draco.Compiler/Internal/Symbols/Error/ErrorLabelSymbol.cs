namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents label reference that has an error - for example, it does not exist.
/// </summary>
internal sealed class ErrorLabelSymbol(string name) : LabelSymbol
{
    public override bool IsError => true;

    public override string Name { get; } = name;
}
