namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined, in-source label reference.
/// </summary>
internal sealed class UndefinedLabelSymbol(string name) : LabelSymbol
{
    public override bool IsError => true;

    public override string Name { get; } = name;
}
