using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// A single enum member.
/// </summary>
/// <param name="Documentation">The documentation of this member.</param>
/// <param name="Name">The name of the enum member.</param>
/// <param name="Value">The serialized enum member value.</param>
public sealed record class EnumMember(
    string? Documentation,
    string Name,
    object? Value)
{
    /// <summary>
    /// A discriminator string for the value.
    /// </summary>
    public string ValueDiscriminator => this.Value switch
    {
        int => "Int",
        string => "String",
        _ => throw new NotImplementedException(),
    };
}
