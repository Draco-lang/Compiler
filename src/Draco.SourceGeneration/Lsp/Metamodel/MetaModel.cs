using System;
using System.Collections.Generic;
using System.Text;

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
    public IList<Request> Requests { get; set; } = Array.Empty<Request>();

    /// <summary>
    /// The notifications.
    /// </summary>
    public IList<Notification> Notifications { get; set; } = Array.Empty<Notification>();

    /// <summary>
    /// The structures.
    /// </summary>
    public IList<Structure> Structures { get; set; } = Array.Empty<Structure>();

    /// <summary>
    /// The enumerations.
    /// </summary>
    public IList<Enumeration> Enumerations { get; set; } = Array.Empty<Enumeration>();

    /// <summary>
    /// The type aliases.
    /// </summary>
    public IList<TypeAlias> TypeAliases { get; set; } = Array.Empty<TypeAlias>();
}
