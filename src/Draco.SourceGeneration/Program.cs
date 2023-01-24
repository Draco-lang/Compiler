using System;
using System.IO;
using System.Xml.Serialization;
using Draco.SourceGeneration.SyntaxTree;
using Scriban;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Scriban.Runtime;
using Scriban.Parsing;
using System.Threading.Tasks;

namespace Draco.SourceGeneration;

internal class Program
{
    internal static void Main(string[] args)
    {
        var syntaxXml = File.ReadAllText("../../../../Draco.Compiler/Internal/Syntax/Syntax.xml");
        var serializer = new XmlSerializer(typeof(XmlTree));
        var xmlModel = (XmlTree)serializer.Deserialize(new StringReader(syntaxXml))!;
        var domainModel = Tree.FromXml(xmlModel);

        Console.WriteLine(CodeGenerator.GenerateGreenTree(domainModel));
        Console.WriteLine("===========================");
        Console.WriteLine(CodeGenerator.GenerateRedTree(domainModel));
    }
}
