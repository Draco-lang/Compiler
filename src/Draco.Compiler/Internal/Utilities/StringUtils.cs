using System.Collections.Generic;
using System.Text;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// General string utilities.
/// </summary>
internal static class StringUtils
{
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
    /// Converts a 0-based numeric index into an Excel-like column name.
    /// </summary>
    /// <param name="index">The index to convert.</param>
    /// <returns>The Excel column-name of <paramref name="index"/>.</returns>
    public static string IndexToExcelColumnName(int index)
    {
        var result = new StringBuilder();
        ++index;
        while (index > 0)
        {
            var mod = (index - 1) % 26;
            result.Insert(0, (char)('A' + mod));
            index = (index - mod) / 26;
        }
        return result.ToString();
    }

    /// <summary>
    /// Converts a PascalCase text to snake_case.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The <paramref name="text"/> in snake case.</returns>
    public static string ToSnakeCase(string text)
    {
        var result = new StringBuilder();
        var first = true;
        foreach (var ch in text)
        {
            if (char.IsUpper(ch))
            {
                if (!first) result.Append('_');
                result.Append(char.ToLower(ch));
            }
            else
            {
                result.Append(ch);
            }
            first = false;
        }
        return result.ToString();
    }

    /// <summary>
    /// Retrieves the length of the newline sequence at the given offset.
    /// </summary>
    /// <param name="str">The string to check the newline for.</param>
    /// <param name="offset">The offset to check the newline at.</param>
    /// <returns>The length of the newline sequence, which is 0, if there is no newline.</returns>
    public static int NewlineLength(string str, int offset)
    {
        if (offset < 0 || offset >= str.Length) return 0;
        if (str[offset] == '\r')
        {
            // Windows or OS-X 9
            if (offset + 1 < str.Length && str[offset + 1] == '\n') return 2;
            else return 1;
        }
        if (str[offset] == '\n') return 1;
        return 0;
    }

    /// <summary>
    /// Splits a string into lines and newline sequences.
    /// </summary>
    /// <param name="str">The string to split.</param>
    /// <returns>The pairs of string content and optional newline sequence after.</returns>
    public static IEnumerable<(string Line, string? Newline)> SplitIntoLines(string str)
    {
        var prevStart = 0;
        for (var i = 0; i < str.Length;)
        {
            var newlineLength = NewlineLength(str, i);
            if (newlineLength == 0)
            {
                // Not a newline
                ++i;
                continue;
            }

            // It is a newline
            var line = str[prevStart..i];
            var newline = str[i..(i + newlineLength)];
            yield return (line, newline);

            i += newlineLength;
            prevStart = i;
        }
        // Possible trailing line
        if (prevStart != str.Length) yield return (str[prevStart..], null);
    }
}
