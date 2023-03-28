using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates registration options within the capability interfaces.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RegistrationOptionsAttribute : Attribute
{
    /// <summary>
    /// The method to register onto.
    /// </summary>
    public string Method { get; set; }

    public RegistrationOptionsAttribute(string method)
    {
        this.Method = method;
    }
}
