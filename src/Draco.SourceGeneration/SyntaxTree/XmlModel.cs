using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draco.SourceGeneration.SyntaxTree;

[XmlRoot(ElementName = "Tree")]
public sealed class XmlTree
{
    [XmlAttribute]
    public string Root { get; set; } = string.Empty;

    [XmlElement("PredefinedNode")]
    public List<XmlPredefinedNode> PredefinedNodes { get; set; } = null!;

    [XmlElement("AbstractNode")]
    public List<XmlAbstractNode> AbstractNodes { get; set; } = null!;

    [XmlElement("Node")]
    public List<XmlNode> Nodes { get; set; } = null!;
}

[XmlRoot(ElementName = "PredefinedNode")]
public sealed class XmlPredefinedNode
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string? Base { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "AbstractNode")]
public sealed class XmlAbstractNode
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string? Base { get; set; } = string.Empty;

    public string Documentation { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "Node")]
public sealed class XmlNode
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string? Base { get; set; } = string.Empty;

    public string Documentation { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "Field")]
public sealed class XmlField
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Type { get; set; } = string.Empty;

    public string Documentation { get; set; } = string.Empty;
}
