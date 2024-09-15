namespace Draco.SourceGeneration.Lsp.CsModel;

/// <summary>
/// A single enum member.
/// </summary>
public sealed class EnumMember : Declaration
{
    /// <summary>
    /// The value of this enum member.
    /// </summary>
    public object? Value { get; set; }
}
