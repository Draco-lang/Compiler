using System;
using System.Collections.Generic;
using System.Linq;

namespace Draco.SourceGeneration.SyntaxTree;

public sealed class Tree
{
    public static Tree FromXml(XmlTree tree)
    {
        ValidateXml(tree);

        Field MakeField(XmlField field) =>
            new(field.Name, field.Type, field.Override, field.Abstract, field.Documentation?.Trim(), field.Tokens.Select(t => t.Kind).ToList());

        Node MakePredefinedNode(XmlPredefinedNode node) =>
            new(node.Name, GetBaseByName(node.Base), false, string.Empty);

        Node MakeAbstractNode(XmlAbstractNode node)
        {
            var result = new Node(node.Name, GetBaseByName(node.Base), true, node.Documentation.Trim());
            foreach (var field in node.Fields) result.Fields.Add(MakeField(field));
            return result;
        }

        Node MakeNode(XmlNode node)
        {
            var result = new Node(node.Name, GetBaseByName(node.Base), false, node.Documentation.Trim());
            foreach (var field in node.Fields) result.Fields.Add(MakeField(field));
            return result;
        }

        Node MakeNodeByName(string name)
        {
            var predefined = tree.PredefinedNodes.FirstOrDefault(n => n.Name == name);
            if (predefined is not null) return MakePredefinedNode(predefined);

            var @abstract = tree.AbstractNodes.FirstOrDefault(n => n.Name == name);
            if (@abstract is not null) return MakeAbstractNode(@abstract);

            var node = tree.Nodes.FirstOrDefault(n => n.Name == name);
            if (node is not null) return MakeNode(node);

            throw new KeyNotFoundException($"no node called {name} was found in the tree");
        }

        var nodes = new Dictionary<string, Node>();
        Node GetNodeByName(string name)
        {
            if (!nodes!.TryGetValue(name, out var node))
            {
                node = MakeNodeByName(name);
                nodes.Add(name, node);
            }
            return node;
        }
        Node? GetBaseByName(string? name) => name is null ? null : GetNodeByName(name);

        return new Tree(
            root: GetNodeByName(tree.Root),
            nodes: tree.AbstractNodes.Select(n => n.Name)
                .Concat(tree.Nodes.Select(n => n.Name))
                .Select(GetNodeByName)
                .ToList());
    }

    private static void ValidateXml(XmlTree tree)
    {
        // Unique node name validation
        var names = new HashSet<string>();
        void AddNodeName(string name)
        {
            if (!names!.Add(name)) throw new InvalidOperationException($"duplicate node named {name} in ther tree");
        }

        foreach (var predefined in tree.PredefinedNodes) AddNodeName(predefined.Name);
        foreach (var @abstract in tree.AbstractNodes) AddNodeName(@abstract.Name);
        foreach (var node in tree.Nodes) AddNodeName(node.Name);
    }

    public Node Root { get; }
    public IList<Node> Nodes { get; }

    public Tree(Node root, IList<Node> nodes)
    {
        this.Root = root;
        this.Nodes = nodes;
    }
}

public sealed class Node
{
    public string Name { get; }
    public Node? Base { get; }
    public bool IsAbstract { get; }
    public string Documentation { get; }
    public IList<Node> Derived { get; } = new List<Node>();
    public IList<Field> Fields { get; } = new List<Field>();

    public Node(string name, Node? @base, bool isAbstract, string documentation)
    {
        this.Name = name;
        this.Base = @base;
        this.IsAbstract = isAbstract;
        this.Documentation = documentation;

        @base?.Derived.Add(this);
    }
}

public sealed class Field
{
    public string Name { get; }
    public string Type { get; }
    public bool Override { get; }
    public bool Abstract { get; }
    public string? Documentation { get; }
    public IList<string> TokenKinds { get; }
    public bool IsNullable => this.Type.EndsWith("?");
    public string NonNullableType => this.IsNullable
        ? this.Type.Substring(0, this.Type.Length - 1)
        : this.Type;
    public bool IsToken => this.NonNullableType == "SyntaxToken";

    public Field(string name, string type, bool @override, bool @abstract, string? documentation, IList<string> tokenKinds)
    {
        this.Name = name;
        this.Type = type;
        this.Override = @override;
        this.Abstract = @abstract;
        this.Documentation = documentation;
        this.TokenKinds = tokenKinds;
    }
}
