using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Dap.Metamodel;

/// <summary>
/// An anonymous object type.
/// </summary>
internal sealed class ObjectType : MetaType
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}
