using System;
using System.IO;
using System.Xml.Serialization;
using Draco.SourceGeneration.SyntaxTree;
using Scriban;

namespace Draco.SourceGeneration;

internal class Program
{
    const string syntax = """
    <?xml version="1.0" encoding="utf-8"?>
    <Tree Root="SyntaxNode" Namespace="Draco.Syntax">
        <AbstractNode Name="StatementSyntax" Base="SyntaxNode">
            <Documentation>
                Syntax node of any statement.
            </Documentation>
        </AbstractNode>
    </Tree>
    """;

    internal static void Main(string[] args)
    {
        var str = new StringWriter();
        var serializer = new XmlSerializer(typeof(XmlTree));
        var xmlModel = (XmlTree)serializer.Deserialize(new StringReader(syntax))!;
        var domainModel = Tree.FromXml(xmlModel);

        var template = Template.Parse(File.ReadAllText("../../../SyntaxTree/GreenTree.sbncs"));
        var output = template.Render(model: domainModel, memberRenamer: n => n.Name);
        Console.WriteLine(output);
    }
}
