using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Attributes;

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
