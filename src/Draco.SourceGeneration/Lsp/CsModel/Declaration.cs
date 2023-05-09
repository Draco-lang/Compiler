namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// The base of all declarations.
/// </summary>
public abstract class Declaration
{
    /// <summary>
    /// A discriminator string for Scriban.
    /// </summary>
    public string Discriminator => this.GetType().Name;

    /// <summary>
    /// The docs of this declaration.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// The deprecation message, if any.
    /// </summary>
    public string? Deprecated { get; set; }

    /// <summary>
    /// The version since this declaration was introduced.
    /// </summary>
    public string? SinceVersion { get; set; }

    /// <summary>
    /// True, if this declaration is only proposed, not necessarily final.
    /// </summary>
    public bool IsProposed { get; set; }

    /// <summary>
    /// The name of this declaration.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
