using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation;

/// <summary>
/// Utilities for extracting info from the LSP specs Markdown.
/// </summary>
internal static class MarkdownReader
{
    private static readonly Regex regex = new(@"{% *include_relative +(\S+) *%}", RegexOptions.Compiled);

    /// <summary>
    /// Reads out all relevant code snippets from a Markdown.
    /// </summary>
    /// <param name="markdown">The Markdown text.</param>
    /// <param name="languageIds">The language IDs that we want to extract.</param>
    /// <returns>The sequence of extracted snippets.</returns>
    public static IEnumerable<string> ExtractCodeSnippets(string markdown, params string[] languageIds)
    {
        var reader = new StringReader(markdown);
        var line = null as string;
        while ((line = reader.ReadLine()) is not null)
        {
            // Check, if snippet block
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("```")) continue;
            // Check, if relevant ID
            var languageId = trimmedLine[3..];
            if (!languageIds.Contains(languageId)) continue;
            // We start a block
            var snippet = new StringBuilder();
            while ((line = reader.ReadLine()) is not null)
            {
                // End of block
                if (line.Trim() == "```") break;
                // Content
                snippet.AppendLine(line);
            }
            // Done
            yield return snippet.ToString();
        }
    }

    /// <summary>
    /// Resolves relative includes in a Markdown text.
    /// </summary>
    /// <param name="markdown">The Markdown text.</param>
    /// <param name="rootPath">The path that should be considered the root for relative importing.</param>
    /// <returns>The expanded form of <paramref name="markdown"/>, inlining all includes.</returns>
    public static string ResolveRelativeIncludes(string markdown, string rootPath) =>
        regex.Replace(markdown, match =>
        {
            var fileToRead = Path.Combine(rootPath, match.Groups[1].Value);
            var nextRoot = Path.GetDirectoryName(fileToRead);
            var includedMd = File.ReadAllText(fileToRead);
            return ResolveRelativeIncludes(includedMd, nextRoot ?? string.Empty);
        });
}
