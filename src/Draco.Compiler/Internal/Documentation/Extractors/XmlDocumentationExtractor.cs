using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Documentation.Extractors;

/// <summary>
/// Extracts XML into <see cref="SymbolDocumentation"/>.
/// </summary>
internal sealed class XmlDocumentationExtractor
{
    public string Xml { get; }
    public Symbol ContainingSymbol { get; }
    public MetadataAssemblySymbol Assembly => this.ContainingSymbol.AncestorChain.OfType<MetadataAssemblySymbol>().First();

    public XmlDocumentationExtractor(string xml, Symbol containingSymbol)
    {
        this.Xml = xml;
        this.ContainingSymbol = containingSymbol;
    }

    /// <summary>
    /// Extracts the <see cref="Xml"/>.
    /// </summary>
    /// <returns>The extracted XMl as <see cref="SymbolDocumentation"/>.</returns>
    public SymbolDocumentation Extract()
    {
        // TODO: exception
        //       para
        //       list
        //       c
        //       see - not cref links
        //       seealso
        //       b ?
        //       i ?

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
            sections.Add(this.ExtractSection(node, out nextNode));
        return new SymbolDocumentation(sections.ToImmutable());
    }

    private DocumentationSection ExtractSection(XmlNode node, out XmlNode? nextNode)
    {
        if (node.Name == "param")
        {
            nextNode = node;
            var parameters = ImmutableArray.CreateBuilder<DocumentationElement>();
            for (; nextNode?.Name == "param"; nextNode = nextNode.NextSibling)
                parameters.Add(this.ExtractParameter(nextNode));
            return new DocumentationSection(SectionKind.Parameters, parameters.ToImmutable());
        }

        if (node.Name == "typeparam")
        {
            nextNode = node;
            var typeParameters = ImmutableArray.CreateBuilder<DocumentationElement>();
            for (; nextNode?.Name == "typeparam"; nextNode = nextNode.NextSibling)
                typeParameters.Add(this.ExtractTypeParameter(nextNode));
            return new DocumentationSection(SectionKind.TypeParameters, typeParameters.ToImmutable());
        }

        if (node.Name == "code")
        {
            nextNode = node.NextSibling;
            return new DocumentationSection(SectionKind.Code, ImmutableArray.Create(this.ExtractElement(node)));
        }

        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode element in node.ChildNodes)
            elements.Add(this.ExtractElement(element));

        nextNode = node.NextSibling;
        return node.Name == "summary"
            ? new DocumentationSection(SectionKind.Summary, elements.ToImmutable())
            : new DocumentationSection(node.Name, elements.ToImmutable());
    }

    private DocumentationElement ExtractElement(XmlNode node) => node.LocalName switch
    {
        "#text" => new TextDocumentationElement(node.InnerText),
        "see" => new SeeDocumentationElement(this.GetSymbolFromDocumentationName(node.Attributes?["cref"]?.Value ?? string.Empty) ?? new PrimitiveTypeSymbol(node.Attributes?["cref"]?.Value[2..] ?? string.Empty, false)),
        "paramref" => new ParamrefDocumentationElement(this.GetParameter(node.Attributes?["name"]?.Value ?? string.Empty)),
        "typeparamref" => new TypeParamrefDocumentationElement(this.GetTypeParameter(node.Attributes?["name"]?.Value ?? string.Empty)),
        "code" => new CodeDocumentationElement(node.InnerXml, "cs"),
        _ => new TextDocumentationElement(node.InnerText),
    };

    private ParameterDocumentationElement ExtractParameter(XmlNode node)
    {
        Debug.Assert(node.Name == "param");
        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode child in node.ChildNodes)
            elements.Add(this.ExtractElement(child));
        return new ParameterDocumentationElement(new ParamrefDocumentationElement(this.GetParameter(node.Attributes?["name"]?.Value ?? string.Empty)), elements.ToImmutable());
    }

    private TypeParameterDocumentationElement ExtractTypeParameter(XmlNode node)
    {
        Debug.Assert(node.Name == "typeparam");
        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode child in node.ChildNodes)
            elements.Add(this.ExtractElement(child));
        return new TypeParameterDocumentationElement(new TypeParamrefDocumentationElement(this.GetTypeParameter(node.Attributes?["name"]?.Value ?? string.Empty)), elements.ToImmutable());
    }

    private Symbol? GetSymbolFromDocumentationName(string documentationName) =>
        this.Assembly.Compilation.MetadataAssemblies.Values
            .Select(x => x.RootNamespace
            .LookupByDocumentationName(documentationName))
            .OfType<Symbol>()
            .FirstOrDefault();

    private ParameterSymbol? GetParameter(string paramName) =>
        (this.ContainingSymbol as FunctionSymbol)?.Parameters.FirstOrDefault(x => x.Name == paramName);

    private TypeParameterSymbol? GetTypeParameter(string paramName) =>
       (this.ContainingSymbol as FunctionSymbol)?.GenericParameters.FirstOrDefault(x => x.Name == paramName);
}
