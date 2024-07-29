namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// An error indexer property symbol.
/// </summary>
/// <param name="indicesCount">The number of indices the indexer property takes.</param>
internal sealed class ErrorIndexerPropertySymbol(int indicesCount) : ErrorPropertySymbol
{
    public override bool IsIndexer => true;
    protected override int ParameterCount => indicesCount;
}
