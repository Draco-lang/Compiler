namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Any symbol that is in a generic instantiated context.
/// </summary>
internal interface IGenericInstanceSymbol
{
    /// <summary>
    /// The generic context introduced.
    /// </summary>
    public GenericContext Context { get; }
}
