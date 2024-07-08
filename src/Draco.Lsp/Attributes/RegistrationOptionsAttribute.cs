using System;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates registration options within the capability interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RegistrationOptionsAttribute(string method) : Attribute
{
    /// <summary>
    /// The method to register onto.
    /// </summary>
    public string Method { get; set; } = method;
}
