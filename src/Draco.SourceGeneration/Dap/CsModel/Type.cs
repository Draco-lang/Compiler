using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Draco.SourceGeneration.Dap.CsModel;

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
            if (name.EndsWith("Type")) name = name[..^4];
            return name;
        }
    }
}

/// <summary>
/// A type backed by a C# declaration.
/// </summary>
/// <param name="Declaration">The referenced C# declaration.</param>
public sealed record class DeclarationType(Declaration Declaration) : Type;

/// <summary>
/// A builtin C# type.
/// </summary>
/// <param name="FullName">The full name of the reflected type.</param>
public sealed record class BuiltinType(string FullName) : Type;
