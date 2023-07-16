using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Attributes;

/// <summary>
/// Annotates a server capability interface with the corresponding client capability.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class ClientCapabilityAttribute : Attribute
{
    /// <summary>
    /// The capability property path.
    /// </summary>
    public string Path { get; set; }

    public ClientCapabilityAttribute(string path)
    {
        this.Path = path;
    }
}
