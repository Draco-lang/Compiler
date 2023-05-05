using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents an array type (e.g. `TextDocument[]`).
/// </summary>
internal sealed class ArrayType : Type
{
    public string Kind => "array";

    public Type Element { get; set; } = null!;
}
