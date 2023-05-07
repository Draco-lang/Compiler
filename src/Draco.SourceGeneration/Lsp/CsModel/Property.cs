using System;

namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// A property definition.
/// </summary>
public sealed class Property : Declaration
{
    /// <summary>
    /// The property type.
    /// </summary>
    public Type Type { get; set; } = null!;

    /// <summary>
    /// The property name when serialized.
    /// </summary>
    public string SerializedName { get; set; } = string.Empty;

    /// <summary>
    /// True if the property should be omitted, if it's null.
    /// </summary>
    public bool OmitIfNull { get; set; }

    /// <summary>
    /// The value of the enumeration member.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// A discriminator string for the value.
    /// </summary>
    public string ValueDiscriminator => this.Value switch
    {
        string => "String",
        _ => throw new ArgumentOutOfRangeException(),
    };
}
