using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Documentation;

internal class XmlDocumentationExtractor
{
    public string Xml { get; }
    public Symbol ContainingSymbol { get; }
    public MetadataAssemblySymbol Assembly => this.ContainingSymbol.AncestorChain.OfType<MetadataAssemblySymbol>().First();

    public XmlDocumentationExtractor(string xml, Symbol containingSymbol)
    {
        this.Xml = xml;
        this.ContainingSymbol = containingSymbol;
    }

    public SymbolDocumentation Extract()
    {
        var xml = $"""
            <documentation>
              {this.Xml}
            </documentation>
            """;
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var node = doc.DocumentElement!.FirstChild;
        var sections = ImmutableArray.CreateBuilder<DocumentationSection>();
        var nextNode = null as XmlNode;
        for (; node is not null; node = nextNode)
        {
            sections.Add(this.ExtractSection(node, out nextNode));
        }
        return new SymbolDocumentation(sections.ToImmutable());
    }

    private DocumentationSection ExtractSection(XmlNode node, out XmlNode? nextNode)
    {
        if (node.Name == "param")
        {
            nextNode = node;
            var parameters = ImmutableArray.CreateBuilder<ParameterDocumentationElement>();
            for (; nextNode?.Name == "param"; nextNode = nextNode.NextSibling)
            {
                parameters.Add(this.ExtractParameter(nextNode));
            }
            return new ParametersDocumentationSection(parameters.ToImmutable());
        }

        if (node.Name == "code")
        {
            nextNode = node.NextSibling;
            return new CodeDocumentationSection((CodeDocumentationElement)this.ExtractElement(node));
        }

        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode element in node.ChildNodes)
        {
            elements.Add(this.ExtractElement(element));
        }

        nextNode = node.NextSibling;
        return new DocumentationSection(node.Name, elements.ToImmutable());
    }

    private DocumentationElement ExtractElement(XmlNode node) => node.LocalName switch
    {
        "#text" => new RawTextDocumentationElement(node.InnerText),
        "see" => new SeeDocumentationElement(this.GetSymbolFromDocumentationName(node.Attributes?["cref"]?.Value ?? string.Empty)),
        "paramref" => new ParamrefDocumentationElement(this.GetParameter(node.Attributes?["name"]?.Value ?? string.Empty)),
        "code" => new CodeDocumentationElement(node.InnerXml, "cs"),
        _ => new RawTextDocumentationElement(node.InnerText),
    };

    private ParameterDocumentationElement ExtractParameter(XmlNode node)
    {
        Debug.Assert(node.Name == "param");
        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode child in node.ChildNodes)
        {
            elements.Add(this.ExtractElement(child));
        }
        return new ParameterDocumentationElement(new ParamrefDocumentationElement(this.GetParameter(node.Attributes?["name"]?.Value ?? string.Empty)), elements.ToImmutable());
    }

    private Symbol? GetSymbolFromDocumentationName(string documentationName) =>
        this.Assembly.Compilation.MetadataAssemblies.Values
            .Select(x => x.RootNamespace
            .LookupByDocumentationName(documentationName))
            .OfType<Symbol>()
            .FirstOrDefault();

    private ParameterSymbol? GetParameter(string paramName) =>
        (this.ContainingSymbol as FunctionSymbol)?.Parameters.FirstOrDefault(x => x.Name == paramName);
}
