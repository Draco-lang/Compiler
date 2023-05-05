using System;
using System.Collections.Generic;
using System.Text;

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
    public string? Documentation { get; set; } = null;

    /// <summary>
    /// The name of this declaration.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
