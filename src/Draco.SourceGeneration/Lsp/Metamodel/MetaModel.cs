using System;
using System.Collections.Generic;

namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// The actual meta model.
/// </summary>
internal sealed class MetaModel
{
    /// <summary>
    /// Additional meta data.
    /// </summary>
    public MetaData MetaData { get; set; } = null!;

    /// <summary>
    /// The requests.
    /// </summary>
    public required IList<Request> Requests { get; set; }

    /// <summary>
    /// The notifications.
    /// </summary>
    public required IList<Notification> Notifications { get; set; }

    /// <summary>
    /// The structures.
    /// </summary>
    public required IList<Structure> Structures { get; set; }

    /// <summary>
    /// The enumerations.
    /// </summary>
    public required IList<Enumeration> Enumerations { get; set; }

    /// <summary>
    /// The type aliases.
    /// </summary>
    public required IList<TypeAlias> TypeAliases { get; set; }
}
