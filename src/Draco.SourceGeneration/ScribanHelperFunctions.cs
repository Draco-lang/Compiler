using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Scriban.Runtime;

namespace Draco.SourceGeneration;

public sealed class ScribanHelperFunctions : ScriptObject
{
    private static readonly string[] keywords = new[]
    {
        "if", "else", "while", "for", "foreach",
        "params", "ref", "out", "operator",
        "object",
    };

    public static string EscapeKeyword(string name) => keywords.Contains(name)
        ? $"@{name}"
        : name;

    public static string CamelCase(string str)
    {
        if (str.Length == 0) return str;
        var result = $"{char.ToLower(str[0])}{str.Substring(1)}";
        return EscapeKeyword(result);
    }

    public static string RemovePrefix(string str, string suffix) => str.StartsWith(suffix)
        ? str.Substring(suffix.Length)
        : str;

    public static string RemoveSuffix(string str, string suffix) => str.EndsWith(suffix)
        ? str.Substring(0, str.Length - suffix.Length)
        : str;

    public static IList<string> SplitLines(string str)
    {
        var result = new List<string>();
        var reader = new StringReader(str);
        var line = null as string;
        while ((line = reader.ReadLine()) is not null) result.Add(line);
        return result;
    }

    public static ScribanHelperFunctions Instance { get; } = new();

    private ScribanHelperFunctions()
    {
    }
}
