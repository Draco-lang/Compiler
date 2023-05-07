namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a LSP notification.
/// </summary>
internal sealed class Notification
{
    /// <summary>
    /// The notification's method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    // NOTE: The metamodel specifies Type | Type[], but an array is never used here.
    /// <summary>
    /// The parameter type(s) if any.
    /// </summary>
    public Type? Params { get; set; }

    /// <summary>
    /// Optional a dynamic registration method if it
    /// different from the notification's method.
    /// </summary>
    public string? RegistrationMethod { get; set; }

    /// <summary>
    /// Optional registration options if the notification
    /// supports dynamic registration.
    /// </summary>
    public Type? RegistrationOptions { get; set; }

    /// <summary>
    /// The direction in which this request is sent
    /// in the protocol.
    /// </summary>
    public MessageDirection MessageDirection { get; set; }

    /// <summary>
    /// An optional documentation.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Since when (release number) this notification is
    /// available.Is undefined if not known.
    /// </summary>
    public string? Since { get; set; }

    /// <summary>
    /// Whether this is a proposed notification. If omitted
    /// the notification is final.
    /// </summary>
    public bool? Proposed { get; set; }

    /// <summary>
    /// Whether the notification is deprecated or not. If deprecated
    /// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }
}
