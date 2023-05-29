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
    /// The parent of this class in terms of containment, not inheritance.
    /// </summary>
    public Class? Parent { get; set; }

    /// <summary>
    /// The classes this class has nested within it.
    /// </summary>
    public IList<Class> NestedClasses { get; set; } = new List<Class>();

    /// <summary>
    /// The properties within this class.
    /// </summary>
    public IList<Property> Properties { get; set; } = new List<Property>();
}
