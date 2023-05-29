using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Dap.Metamodel;

/// <summary>
/// Represents the "allOf" <see cref="MetaType"/>.
/// </summary>
internal sealed class AllOf : MetaType
{
    public required List<MetaType> Elements { get; set; }
}
