using System;
using System.IO;
using System.Xml.Serialization;
using Draco.SourceGeneration.SyntaxTree;
using Scriban;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.SourceGeneration;

internal class Program
{
    internal static void Main(string[] args)
    {
        var syntaxXml = File.ReadAllText("../../../../Draco.Compiler/Internal/Syntax/Syntax.xml");
        var str = new StringWriter();
        var serializer = new XmlSerializer(typeof(XmlTree));
        var xmlModel = (XmlTree)serializer.Deserialize(new StringReader(syntaxXml))!;
        var domainModel = Tree.FromXml(xmlModel);

        var template = Template.Parse(File.ReadAllText("../../../SyntaxTree/GreenTree.sbncs"));
        var output = template.Render(model: domainModel, memberRenamer: n => n.Name);
        output = SyntaxFactory
            .ParseCompilationUnit(output)
            .NormalizeWhitespace()
            .GetText()
            .ToString();
        Console.WriteLine(output);
    }
}
