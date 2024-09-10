using System;
using System.Collections.Generic;
using System.Linq;

namespace Draco.SourceGeneration.SyntaxTree;

public sealed class Tree(Node root, IList<Node> nodes, IList<Token> tokens)
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

        Token MakeToken(XmlToken token) =>
            new(token.Kind, token.Text, token.Value, token.Documentation.Trim());

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
            nodes: tree.AbstractNodes
                .Select(n => n.Name)
                .Concat(tree.Nodes.Select(n => n.Name))
                .Select(GetNodeByName)
                .ToList(),
            tokens: tree.Tokens
                .Select(MakeToken)
                .ToList());
    }

    private static void ValidateXml(XmlTree tree)
    {
        // Unique node name validation
        var nodeNames = new HashSet<string>();
        void AddNodeName(string name)
        {
            if (!nodeNames!.Add(name)) throw new InvalidOperationException($"duplicate node named {name} in ther tree");
        }

        var tokenKinds = new HashSet<string>();
        void AddTokenKind(string kind)
        {
            if (!tokenKinds!.Add(kind)) throw new InvalidOperationException($"duplicate token kind {kind} in the tree");
        }

        foreach (var token in tree.Tokens) AddTokenKind(token.Kind);
        foreach (var predefined in tree.PredefinedNodes) AddNodeName(predefined.Name);
        foreach (var @abstract in tree.AbstractNodes) AddNodeName(@abstract.Name);
        foreach (var node in tree.Nodes) AddNodeName(node.Name);

        foreach (var node in tree.Nodes)
        {
            foreach (var field in node.Fields)
            {
                foreach (var kind in field.Tokens)
                {
                    if (tokenKinds.Contains(kind.Kind)) continue;
                    throw new InvalidOperationException($"token kind {kind.Kind} in field {field.Name} of node {node.Name} is not defined in the tree");
                }
            }
        }
    }

    public Node Root { get; } = root;
    public IList<Node> Nodes { get; } = nodes;
    public IList<Token> Tokens { get; } = tokens;

    public bool HasTokenKind(string kind) => this.Tokens.Any(t => t.Name == kind);
    public Token GetTokenFromKind(string kind) => this.Tokens.First(t => t.Name == kind);
}

public sealed class Token(string name, string? text, string? value, string documentation)
{
    public string Name { get; } = name;
    public string? Text { get; } = text;
    public string? Value { get; } = value;
    public string Documentation { get; } = documentation;
}

public sealed class Node
{
    public string Name { get; }
    public Node? Base { get; }
    public bool IsAbstract { get; }
    public string Documentation { get; }
    public IList<Node> Derived { get; } = [];
    public IList<Field> Fields { get; } = [];

    public Node(string name, Node? @base, bool isAbstract, string documentation)
    {
        this.Name = name;
        this.Base = @base;
        this.IsAbstract = isAbstract;
        this.Documentation = documentation;

        @base?.Derived.Add(this);
    }
}

public sealed class Field(
    string name,
    string type,
    bool @override,
    bool @abstract,
    string? documentation,
    IList<string> tokenKinds)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
    public bool Override { get; } = @override;
    public bool Abstract { get; } = @abstract;
    public string? Documentation { get; } = documentation;
    public IList<string> TokenKinds { get; } = tokenKinds;
    public bool IsNullable => this.Type.EndsWith("?");
    public string NonNullableType => this.IsNullable ? this.Type[..^1] : this.Type;
    public bool IsToken => this.NonNullableType == "SyntaxToken";
    public bool IsSyntaxList => this.NonNullableType.StartsWith("SyntaxList");
    public string ElementType => this.NonNullableType.Split('<', '>')[1];
}
