using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Draco.SourceGeneration.Dap.Metamodel;

/// <summary>
/// Represents a $ref.
/// </summary>
internal sealed class RefType : MetaType
{
    [JsonPropertyName("$ref")]
    public required string Path { get; set; }
}
