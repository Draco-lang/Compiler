using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Dap.CsModel;

/// <summary>
/// A class declaration.
/// </summary>
public sealed class Class : Declaration
{
    /// <summary>
    /// The base of this class.
    /// </summary>
    public Class? Base { get; set; }

    /// <summary>
    /// The properties within this class.
    /// </summary>
    public IList<Property> Properties { get; set; } = new List<Property>();
}
