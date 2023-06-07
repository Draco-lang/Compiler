using System;

namespace Draco.Dap.Attributes;

// NOTE: While currently almost all debug adapter capabilities are merely booleans,
// so annotating the type would be enough, but there are already two exceptions:
//  - CompletionTriggerCharacters
//  - AdditionalModuleColumns
// So for most cases, we'll just annotate a => true prop in the capability interfaces to avoid
// starting to hack in this extra data.
// If a capability potentially sets multiple properties, we just annotate all of the props with the
// corresponding capability name.

/// <summary>
/// Annotates a capability property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CapabilityAttribute : Attribute
{
    /// <summary>
    /// The capability's property name.
    /// </summary>
    public string Property { get; set; }

    public CapabilityAttribute(string property)
    {
        this.Property = property;
    }
}
