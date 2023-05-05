using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Includes StructureLiteralType, StringLiteralType, IntegerLiteralType, BooleanLiteralType.
/// </summary>
internal sealed class LiteralType : Type
{
    public override string Kind { get; set; } = string.Empty;

    public object Value { get; set; } = null!;
}
