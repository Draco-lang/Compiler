using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draco.SourceGeneration.SyntaxTree;

public sealed class Tree
{
    [XmlAttribute]
    public string Root { get; set; } = string.Empty;

    [XmlAttribute]
    public string Namespace { get; set; } = string.Empty;

    [XmlElement("PredefinedNode")]
    public List<PredefinedNode> PredefinedNodes { get; set; } = null!;

    [XmlElement("AbstractNode")]
    public List<AbstractNode> AbstractNodes { get; set; } = null!;

    [XmlElement("Node")]
    public List<Node> Nodes { get; set; } = null!;
}

public sealed class PredefinedNode
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Base { get; set; } = string.Empty;
}

public sealed class AbstractNode
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Base { get; set; } = string.Empty;

    public string Documentation { get; set; } = string.Empty;
}

public sealed class Node
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Base { get; set; } = string.Empty;

    public string Documentation { get; set; } = string.Empty;
}

public sealed class Field
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Type { get; set; } = string.Empty;

    public string Documentation { get; set; } = string.Empty;
}
