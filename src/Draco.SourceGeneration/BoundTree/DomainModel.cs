using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Draco.SourceGeneration.BoundTree;

public sealed class Tree(Node root, IList<Node> nodes)
{
    public static Tree FromXml(XmlTree tree)
    {
        ValidateXml(tree);

        Field MakeField(XmlField field) =>
            new(field.Name, field.Type, field.Override);

        Node MakePredefinedNode(XmlPredefinedNode node) =>
            new(node.Name, GetBaseByName(node.Base), false);

        Node MakeAbstractNode(XmlAbstractNode node)
        {
            var result = new Node(node.Name, GetBaseByName(node.Base), true);
            foreach (var field in node.Fields) result.Fields.Add(MakeField(field));
            return result;
        }

        Node MakeNode(XmlNode node)
        {
            var result = new Node(node.Name, GetBaseByName(node.Base), false);
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

    public Node Root { get; } = root;
    public IList<Node> Nodes { get; } = nodes;

    public bool HasNodeWithName(string name) => this.Nodes.Any(n => n.Name == name);
}

public sealed class Node
{
    public string Name { get; }
    public Node? Base { get; }
    public bool IsAbstract { get; }
    public IList<Node> Derived { get; } = [];
    public IList<Field> Fields { get; } = [];

    public Node(string name, Node? @base, bool isAbstract)
    {
        this.Name = name;
        this.Base = @base;
        this.IsAbstract = isAbstract;

        @base?.Derived.Add(this);
    }
}

public sealed class Field(string name, string type, bool @override)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
    public bool Override { get; } = @override;
    public bool IsNullable => this.Type.EndsWith("?");
    public string NonNullableType => this.IsNullable
        ? this.Type[..^1]
        : this.Type;
    public bool IsArray => this.Type.StartsWith("ImmutableArray");
    public string ElementType
    {
        get
        {
            Debug.Assert(this.IsArray);
            var start = this.Type.IndexOf('<') + 1;
            var end = this.Type.IndexOf('>');
            return this.Type[start..end];
        }
    }
}
