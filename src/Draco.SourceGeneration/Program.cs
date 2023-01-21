using System;
using System.IO;
using System.Xml.Serialization;
using Draco.SourceGeneration.SyntaxTree;

namespace Draco.SourceGeneration;

internal class Program
{
    internal static void Main(string[] args)
    {
        var str = new StringWriter();
        var serializer = new XmlSerializer(typeof(Tree), defaultNamespace: null);
        serializer.Serialize(str, new Tree()
        {
            Root = "Reee",
            PredefinedNodes = new()
            {
                new(),
                new(),
            },
            AbstractNodes = new()
            {
                new(),
                new(),
            },
            Nodes = new()
            {
                new(),
                new(),
            },
        });
        Console.WriteLine(str.ToString());
    }
}
