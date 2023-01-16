using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer;

/// <summary>
/// Constants important for the language server.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// The language ID.
    /// </summary>
    public const string LanguageId = "draco";

    /// <summary>
    /// The language source file extension.
    /// </summary>
    public const string DracoSourceExtension = ".draco";

    /// <summary>
    /// The document filter for Draco source files.
    /// </summary>
    public static DocumentFilter DracoSourceDocumentFilter { get; } = new DocumentFilter
    {
        Pattern = $"**/*{DracoSourceExtension}",
    };

    /// <summary>
    /// The document selector for Draco source files.
    /// </summary>
    public static DocumentSelector DracoSourceDocumentSelector { get; } = new(DracoSourceDocumentFilter);
}
