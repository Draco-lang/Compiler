using System;
using System.Collections.Generic;
using System.Text;
using Scriban.Runtime;

namespace Draco.SourceGeneration;

public sealed class ScribanHelperFunctions : ScriptObject
{
    public static string CamelCase(string str)
    {
        if (str.Length == 0) return str;
        return $"{char.ToLower(str[0])}{str[1..]}";
    }

    public static ScribanHelperFunctions Instance { get; } = new();

    private ScribanHelperFunctions()
    {
    }
}
