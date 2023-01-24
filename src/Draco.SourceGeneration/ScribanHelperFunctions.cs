using System;
using System.Collections.Generic;
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
        var result = $"{char.ToLower(str[0])}{str[1..]}";
        return EscapeKeyword(result);
    }

    public static string RemoveSuffix(string str, string suffix) => str.EndsWith(suffix)
        ? str[..^suffix.Length]
        : str;

    public static ScribanHelperFunctions Instance { get; } = new();

    private ScribanHelperFunctions()
    {
    }
}
