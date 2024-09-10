using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.SourceGeneration;

/// <summary>
/// Templating utilities.
/// </summary>
internal static class TemplateUtils
{
    private static readonly string[] keywords =
    [
        "if",
        "else",
        "while",
        "for",
        "foreach",
        "params",
        "ref",
        "out",
        "operator",
        "object",
        "bool",
        "string"
    ];

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
    /// Removes a prefix from a string, if it starts with the prefix.
    /// </summary>
    /// <param name="str">The string to remove the prefix from.</param>
    /// <param name="prefix">The prefix to remove.</param>
    /// <returns>The string without the prefix.</returns>
    public static string RemovePrefix(string str, string prefix) => str.StartsWith(prefix)
        ? str[prefix.Length..]
        : str;

    /// <summary>
    /// Removes a suffix from a string, if it ends with the suffix.
    /// </summary>
    /// <param name="str">The string to remove the suffix from.</param>
    /// <param name="suffix">The suffix to remove.</param>
    /// <returns>The string without the suffix.</returns>
    public static string RemoveSuffix(string str, string suffix) => str.EndsWith(suffix)
        ? str[..^suffix.Length]
        : str;

    /// <summary>
    /// Converts a name to a valid C# identifier in camel case.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The converted name in camel-case and escaped if necessary.</returns>
    public static string CamelCase(string name)
    {
        if (name.Length == 0) return name;
        var result = $"{char.ToLower(name[0])}{name[1..]}";
        return EscapeKeyword(result);
    }

    /// <summary>
    /// Escapes a C# keyword, if necessary.
    /// </summary>
    /// <param name="name">The name to escape.</param>
    /// <returns>The escaped name.</returns>
    public static string EscapeKeyword(string name) => keywords.Contains(name)
        ? $"@{name}"
        : name;

    /// <summary>
    /// Wraps a string between quotes and escapes it to be a valid C# line-string literal.
    /// </summary>
    /// <param name="text">The text to wrap.</param>
    /// <returns>The wrapped and escaped text.</returns>
    public static string StringLiteral(string text) => $"\"{Unescape(text)}\"";

    /// <summary>
    /// Unescapes a given text to be a valid C# line-string literal.
    /// </summary>
    /// <param name="text">The text to unescape.</param>
    /// <returns>The unescaped version of <paramref name="text"/>.</returns>
    public static string Unescape(string text)
    {
        var result = new StringBuilder();
        foreach (var ch in text)
        {
            result.Append(ch switch
            {
                '\"' => @"\""",
                '\'' => @"\'",
                '\\' => @"\\",
                '\a' => @"\a",
                '\b' => @"\b",
                '\f' => @"\f",
                '\n' => @"\n",
                '\r' => @"\r",
                '\t' => @"\t",
                '\v' => @"\v",
                '\0' => @"\0",
                _ => ch,
            });
        }
        return result.ToString();
    }

    /// <summary>
    /// Encodes a text to be a valid XML text.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>The encoded version of <paramref name="text"/>.</returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? EscapeXml(string? text) => text is null
        ? null
        : HttpUtility.HtmlEncode(text);

    /// <summary>
    /// Transforms a nullable value to a string, using the provided function.
    /// </summary>
    /// <typeparam name="T">The type of the nullable value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="func">The function to apply to the value.</param>
    /// <returns>The result of the function or an empty string if the value is null.</returns>
    public static string NotNull<T>(T? value, Func<T, string> func) where T : class =>
        value is null ? string.Empty : func(value);

    /// <summary>
    /// Emits the value if the condition is true.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="value">The value to emit.</param>
    /// <returns>The value if the condition is true, otherwise an empty string.</returns>
    public static string When(bool condition, string value) => When(condition, value, string.Empty);

    /// <summary>
    /// Emits the value if the condition is true, otherwise the alternative value.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="whenTrue">The value to emit if the condition is true.</param>
    /// <param name="whenFalse">The value to emit if the condition is false.</param>
    /// <returns>The value if the condition is true, otherwise the alternative value.</returns>
    public static string When(bool condition, string whenTrue, string whenFalse) =>
        condition ? whenTrue : whenFalse;

    /// <summary>
    /// Selects the first truthy case result.
    /// </summary>
    /// <param name="cases">The tuples of conditions and results.</param>
    /// <returns>The first truthy result or an empty string if none are truthy.</returns>
    public static string Case(params (bool Condition, string Result)[] cases) =>
        cases.FirstOrDefault(c => c.Condition).Result ?? string.Empty;

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

    /// <summary>
    /// Loops over the lines of a string and applies the function, concatenating the results.
    /// </summary>
    /// <param name="str">The string to loop over.</param>
    /// <param name="func">The function to apply to each line.</param>
    /// <returns>The concatenated results of the functions line by line.</returns>
    public static string ForEachLine(string str, Func<string, string> func)
    {
        var result = new StringBuilder();
        var reader = new StringReader(str);
        var first = true;
        while (reader.ReadLine() is { } line)
        {
            if (!first) result.AppendLine();
            result.Append(func(line));
            first = false;
        }
        return result.ToString();
    }
}
