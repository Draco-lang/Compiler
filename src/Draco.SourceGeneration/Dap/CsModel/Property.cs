using System;

namespace Draco.SourceGeneration.Dap.CsModel;

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
    /// True, if the property is required.
    /// </summary>
    public bool IsRequired => !this.OmitIfNull && this.Value is null;
}
