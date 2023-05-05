using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a JSON object map
/// (e.g. `interface Map&lt;K extends string | integer, V&gt; { [key: K] =&gt; V; }`).
/// </summary>
internal sealed class MapType : Type
{
    public string Kind => "map";

    public NamedType Key { get; set; } = null!;

    public Type Value { get; set; } = null!;
}
