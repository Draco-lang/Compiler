using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml;

namespace Draco.Compiler.Internal.Documentation;

internal static class XmlDocumentationExtractor
{
    public static SymbolDocumentation Extract(string xml)
    {
        xml = $"""
            <documentation>
              {xml}
            </documentation>
            """;
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var node = doc.DocumentElement!.FirstChild;
        var sections = ImmutableArray.CreateBuilder<DocumentationSection>();
        var nextNode = null as XmlNode;
        for (; node is not null; node = nextNode)
        {
            sections.Add(ExtractSection(node, out nextNode));
        }
        return new SymbolDocumentation(sections.ToImmutable());
    }

    private static DocumentationSection ExtractSection(XmlNode node, out XmlNode? nextNode)
    {
        if (node.Name == "param")
        {
            nextNode = node;
            var parameters = ImmutableArray.CreateBuilder<ParameterDocumentationElement>();
            for (; nextNode?.Name == "param"; nextNode = nextNode.NextSibling)
            {
                parameters.Add(ExtractParameter(nextNode));
            }
            return new ParametersDocumentationSection(parameters.ToImmutable());
        }

        if (node.Name == "code")
        {
            nextNode = node.NextSibling;
            return new CodeDocumentationSection((CodeDocumentationElement)ExtractElement(node));
        }

        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode element in node.ChildNodes)
        {
            elements.Add(ExtractElement(element));
        }

        nextNode = node.NextSibling;
        return new DocumentationSection(node.Name, elements.ToImmutable());
    }

    private static DocumentationElement ExtractElement(XmlNode node) => node.LocalName switch
    {
        "#text" => new RawTextDocumentationElement(node.InnerText),
        "see" => new SeeDocumentationElement(node.Attributes?["cref"]?.Value ?? string.Empty),
        "paramref" => new ParamrefDocumentationElement(node.Attributes?["name"]?.Value ?? string.Empty),
        "code" => new CodeDocumentationElement(node.InnerXml, "cs"),
        _ => new RawTextDocumentationElement(node.InnerText),
    };

    private static ParameterDocumentationElement ExtractParameter(XmlNode node)
    {
        Debug.Assert(node.Name == "param");
        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode child in node.ChildNodes)
        {
            elements.Add(ExtractElement(child));
        }
        return new ParameterDocumentationElement(node.Attributes?["name"]?.Value ?? string.Empty, elements.ToImmutable());
    }
}
