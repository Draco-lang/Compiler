using System;
using System.Collections.Generic;
using System.Text;

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
    /// True, if this is an abstract property.
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// True, if this is an overriding property.
    /// </summary>
    public Property? Overrides { get; set; }

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
        EnumMember => "Enum",
        _ => throw new ArgumentOutOfRangeException(),
    };
}
