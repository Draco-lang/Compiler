using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Draco.SourceGeneration.Dap.Metamodel;

/// <summary>
/// The toplevel schema object.
/// </summary>
internal sealed class JsonSchema
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public required Dictionary<string, MetaType> Definitions { get; set; }
}
