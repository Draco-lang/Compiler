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
}
