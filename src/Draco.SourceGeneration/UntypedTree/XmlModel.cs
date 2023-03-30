using System.Collections.Generic;
using System.Xml.Serialization;

namespace Draco.SourceGeneration.UntypedTree;

[XmlRoot(ElementName = "Tree", Namespace = "http://draco-lang.com/untyped-tree/untyped-nodes")]
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
    public string? Base { get; set; }
}

[XmlRoot(ElementName = "AbstractNode")]
public sealed class XmlAbstractNode
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string? Base { get; set; }

    [XmlElement("Field")]
    public List<XmlField> Fields { get; set; } = null!;
}

[XmlRoot(ElementName = "Node")]
public sealed class XmlNode
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string? Base { get; set; }

    [XmlElement("Field")]
    public List<XmlField> Fields { get; set; } = null!;
}

[XmlRoot(ElementName = "Field")]
public sealed class XmlField
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Type { get; set; } = string.Empty;

    [XmlAttribute]
    public bool Override { get; set; }
}
