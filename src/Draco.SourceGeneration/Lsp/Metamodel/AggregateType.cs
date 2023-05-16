namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Includes AndType, OrType, TupleType.
/// </summary>
internal sealed record class AggregateType : Type
{
    public override required string Kind { get; set; }

    public required EquatableArray<Type> Items { get; set; }
}
