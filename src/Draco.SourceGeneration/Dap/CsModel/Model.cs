using System.Collections.Generic;

namespace Draco.SourceGeneration.Dap.CsModel;

/// <summary>
/// The C# model of the DAP code.
/// </summary>
public sealed class Model
{
    /// <summary>
    /// The declarations of the model.
    /// </summary>
    public IList<Declaration> Declarations { get; set; } = new List<Declaration>();
}
