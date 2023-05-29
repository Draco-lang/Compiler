using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Dap.CsModel;

/// <summary>
/// The C# model of the DAP code.
/// </summary>
public sealed class Model
{
    /// <summary>
    /// The classes of the model.
    /// </summary>
    public IList<Class> Classes { get; set; } = new List<Class>();
}
