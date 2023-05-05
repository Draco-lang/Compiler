using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// A C# type.
/// </summary>
public abstract record class Type
{
    /// <summary>
    /// A discriminator string for Scriban.
    /// </summary>
    public string Discriminator
    {
        get
        {
            var name = this.GetType().Name;
            if (name.EndsWith("Type")) name = name.Substring(0, name.Length - 4);
            return name;
        }
    }
}
