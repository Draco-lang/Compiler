using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Includes AndType, OrType, TupleType.
/// </summary>
internal sealed class AggregateType : Type
{
    public override string Kind { get; set; } = string.Empty;

    public IList<Type> Items { get; set; } = Array.Empty<Type>();
}
