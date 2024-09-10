namespace Draco.SourceGeneration.Dap.CsModel;

/// <summary>
/// The base of all declarations.
/// </summary>
public abstract class Declaration
{
    /// <summary>
    /// The docs of this declaration.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// The name of this declaration.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
