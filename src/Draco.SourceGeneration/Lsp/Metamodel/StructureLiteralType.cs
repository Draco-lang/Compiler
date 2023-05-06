using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a literal structure
/// (e.g. `property: { start: uinteger; end: uinteger; }`).
/// </summary>
internal sealed class StructureLiteralType : Type
{
    public override string Kind { get; set; } = string.Empty;

    public StructureLiteral Value { get; set; } = null!;
}
