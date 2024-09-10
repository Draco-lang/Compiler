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
    /// Checks, if the property is required.
    /// </summary>
    /// <param name="settable">True if the property is settable.</param>
    /// <returns>True if the property is required.</returns>
    public bool IsRequired(bool settable) => !this.OmitIfNull && this.Value is null && settable;
}
