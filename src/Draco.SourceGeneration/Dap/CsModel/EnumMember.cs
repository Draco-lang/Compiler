using System;

namespace Draco.SourceGeneration.Dap.CsModel;

/// <summary>
/// A single enum member.
/// </summary>
public sealed class EnumMember : Declaration
{
    /// <summary>
    /// The value of this enum member.
    /// </summary>
    public object? Value { get; set; }

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
