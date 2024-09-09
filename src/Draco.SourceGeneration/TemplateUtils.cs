using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.SourceGeneration;

/// <summary>
/// Templating utilities.
/// </summary>
internal static class TemplateUtils
{
    /// <summary>
    /// Formats C# code.
    /// </summary>
    /// <param name="code">The C# code to format.</param>
    /// <returns>The formatted C# code.</returns>
    public static string FormatCSharp(string code) => SyntaxFactory
        .ParseCompilationUnit(code)
        .NormalizeWhitespace()
        .GetText()
        .ToString();

    /// <summary>
    /// Loops over a range and applies the iteration function, concatenating the results.
    /// </summary>
    /// <param name="range">The range to loop over.</param>
    /// <param name="iteration">The iteration function.</param>
    /// <returns>The concatenated results of the iteration function.</returns>
    public static string For(Range range, Func<int, string> iteration) =>
        For(range, string.Empty, iteration);

    /// <summary>
    /// Loops over a range and applies the iteration function, concatenating the results.
    /// </summary>
    /// <param name="range">The range to loop over.</param>
    /// <param name="separator">The separator to use between iterations.</param>
    /// <param name="iteration">The iteration function.</param>
    /// <returns>The concatenated results of the iteration function.</returns>
    public static string For(Range range, string separator, Func<int, string> iteration)
    {
        var result = new StringBuilder();
        // NOTE: Inclusive range, inherit behavior from Scriban
        for (var i = range.Start.Value; i <= range.End.Value; i++)
        {
            if (i > range.Start.Value) result.Append(separator);
            result.Append(iteration(i));
        }
        return result.ToString();
    }

    /// <summary>
    /// Loops over a collection and applies the iteration function, concatenating the results.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    /// <param name="items">The collection to loop over.</param>
    /// <param name="iteration">The iteration function.</param>
    /// <returns>The concatenated results of the iteration function.</returns>
    public static string ForEach<T>(IEnumerable<T> items, Func<T, string> iteration) =>
        ForEach(items, string.Empty, iteration);

    /// <summary>
    /// Loops over a collection and applies the iteration function, concatenating the results.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    /// <param name="items">The collection to loop over.</param>
    /// <param name="separator">The separator to use between iterations.</param>
    /// <param name="iteration">The iteration function.</param>
    /// <returns>The concatenated results of the iteration function.</returns>
    public static string ForEach<T>(IEnumerable<T> items, string separator, Func<T, string> iteration)
    {
        var result = new StringBuilder();
        var first = true;
        foreach (var item in items)
        {
            if (!first) result.Append(separator);
            result.Append(iteration(item));
            first = false;
        }
        return result.ToString();
    }
}
