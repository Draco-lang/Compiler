namespace Draco.Compiler.Api.Semantics;

/// <summary>
/// The visibility of certain symbol.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// The symbol is visible only in the scope it was declared in.
    /// </summary>
    Private,

    /// <summary>
    /// The symbol is visible only in the assembly it was declared in.
    /// </summary>
    Internal,

    /// <summary>
    /// The symbol is visible both inside and outside of the assembly it was declared in.
    /// </summary>
    Public,
}
