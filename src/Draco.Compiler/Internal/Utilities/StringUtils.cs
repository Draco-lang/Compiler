using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// <returns>The Excep column-name of <paramref name="index"/>.</returns>
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
}
