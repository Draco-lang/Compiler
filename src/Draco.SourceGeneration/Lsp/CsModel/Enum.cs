using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// An enum declaration.
/// </summary>
public sealed class Enum : Declaration
{
    /// <summary>
    /// True, if this is a string enum.
    /// </summary>
    public bool IsStringEnum => this.Members.OfType<string>().Any();

    /// <summary>
    /// The members within this enum.
    /// </summary>
    public IList<EnumMember> Members { get; set; } = new List<EnumMember>();
}
