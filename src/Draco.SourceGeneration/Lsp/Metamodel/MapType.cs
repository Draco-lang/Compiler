namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a JSON object map
/// (e.g. `interface Map&lt;K extends string | integer, V&gt; { [key: K] =&gt; V; }`).
/// </summary>
internal sealed record class MapType : Type
{
    public override required string Kind { get; set; }

    public required NamedType Key { get; set; }

    public required Type Value { get; set; }
}
