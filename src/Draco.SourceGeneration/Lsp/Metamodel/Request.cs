namespace Draco.SourceGeneration.Lsp.Metamodel;

/// <summary>
/// Represents a LSP request.
/// </summary>
internal sealed class Request
{
    /// <summary>
    /// The request's method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    // NOTE: The metamodel specifies Type | Type[], but an array is never used here.
    /// <summary>
    /// The parameter type(s) if any.
    /// </summary>
    public Type? Params { get; set; }

    /// <summary>
    /// The result type.
    /// </summary>
    public Type Result { get; set; } = null!;

    /// <summary>
    /// Optional partial result type if the request
	/// supports partial result reporting.
    /// </summary>
    public Type? PartialResult { get; set; }

    /// <summary>
    /// An optional error data type.
    /// </summary>
    public Type? ErrorData { get; set; }

    /// <summary>
    /// Optional a dynamic registration method if it
	/// different from the request's method.
    /// </summary>
    public string? RegistrationMethod { get; set; }

    /// <summary>
    /// Optional registration options if the request
	/// supports dynamic registration.
    /// </summary>
    public Type? RegistrationOptions { get; set; }

    /// <summary>
    /// The direction in which this notification is sent
	/// in the protocol.
    /// </summary>
    public MessageDirection MessageDirection { get; set; }

    /// <summary>
    /// An optional documentation.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Since when (release number) this request is
	/// available.Is undefined if not known.
    /// </summary>
    public string? Since { get; set; }

    /// <summary>
    /// Whether this is a proposed feature. If omitted
	/// the feature is final.
    /// </summary>
    public bool? Proposed { get; set; }

    /// <summary>
    /// Whether the request is deprecated or not. If deprecated
	/// the property contains the deprecation message.
    /// </summary>
    public string? Deprecated { get; set; }
}
