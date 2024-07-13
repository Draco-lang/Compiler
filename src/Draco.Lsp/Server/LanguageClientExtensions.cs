using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server;

/// <summary>
/// Utility methods for <see cref="ILanguageClient"/>.
/// </summary>
public static class LanguageClientExtensions
{
    // Language features

    public static Task PublishDiagnosticsAsync(
        this ILanguageClient client,
        DocumentUri uri,
        IEnumerable<Diagnostic> diagnostics,
        int? version = null) => client.PublishDiagnosticsAsync(new()
        {
            Uri = uri,
            Diagnostics = diagnostics.ToList(),
            Version = version,
        });

    // Workspace features

    public static Task<IList<JsonElement>> GetConfigurationAsync(
        this ILanguageClient client,
        params ConfigurationItem[] items) => client.GetConfigurationAsync(new()
        {
            Items = items.ToList(),
        });

    public static Task<IList<JsonElement>> GetConfigurationAsync(
        this ILanguageClient client,
        IEnumerable<ConfigurationItem> items) => client.GetConfigurationAsync(new()
        {
            Items = items.ToList(),
        });

    // Window features

    public static Task ShowMessageAsync(
        this ILanguageClient client,
        MessageType type,
        string message) => client.ShowMessageAsync(new()
        {
            Type = type,
            Message = message,
        });

    public static Task LogMessageAsync(
        this ILanguageClient client,
        MessageType type,
        string message) => client.LogMessageAsync(new()
        {
            Type = type,
            Message = message,
        });
}
