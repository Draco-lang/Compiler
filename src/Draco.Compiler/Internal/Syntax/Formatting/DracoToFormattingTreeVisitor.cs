using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;
internal class DracoToFormattingTreeVisitor
{
    // default converter
    public static NodeOrToken Convert(FormattingNode parent, Api.Syntax.SyntaxNode node)
    {
        if (node is Api.Syntax.SyntaxToken token) return new NodeOrToken(new FormattingToken() { Text = token.Text });

        var list = new List<NodeOrToken>();
        var converted = new FormattingNode()
        {
            NodeKind = node.GetType().Name,
            Parent = parent,
            Childrens = list
        };
        foreach (var child in node.Children)
        {
            list.Add(Convert(converted, child));
        }
        return new NodeOrToken(converted);
    }


    // flatify converter

    public static NodeOrToken FlatConvert(FormattingNode parent, Api.Syntax.SyntaxNode node)
    {
        var list = new List<NodeOrToken>();
        var converted = new FormattingNode()
        {
            NodeKind = node.GetType().Name,
            Parent = parent,
            Childrens = list
        };
        foreach (var child in node.Tokens)
        {
            list.Add(new NodeOrToken(new FormattingToken() { Text = child.Text }));
        }
        return new NodeOrToken(converted);
    }

    public static NodeOrToken Convert(FormattingNode parent, Api.Syntax.ImportDeclarationSyntax node) => FlatConvert(parent, node);

    public static NodeOrToken Convert(FormattingNode parent, Api.Syntax.FunctionDeclarationSyntax node)
    {
        var list = new List<NodeOrToken>();
        var converted = new FormattingNode()
        {
            NodeKind = node.GetType().Name,
            Parent = parent,
            Childrens = list
        };

        foreach (var child in node.Body.Children)
        {
            list.Add(Convert(converted, child));
        }
        return new NodeOrToken(converted);
    }
}

internal class NodeOrToken
{
    public NodeOrToken(FormattingNode node)
    {
        this.Node = node;
    }

    public NodeOrToken(FormattingToken token)
    {
        this.Token = token;
    }
    [MemberNotNullWhen(true, nameof(Node))]
    [MemberNotNullWhen(false, nameof(Token))]
    public bool IsNode => this.Node != null;

    [MemberNotNullWhen(true, nameof(Token))]
    [MemberNotNullWhen(false, nameof(Node))]
    public bool IsToken => this.Token != null;

    public FormattingNode? Node { get; }
    public FormattingToken? Token { get; }

    public override string ToString() => this.IsNode ? this.Node.ToString() : this.Token.ToString();
}

internal class FormattingNode
{
    public required string NodeKind { get; init; }
    public required FormattingNode? Parent { get; init; }
    public required IReadOnlyCollection<NodeOrToken> Childrens { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var child in this.Childrens)
        {
            sb.Append(child.ToString());
        }
        return sb.ToString();
    }
}

internal class FormattingToken
{
    public required string Text { get; init; }

    public override string ToString() => this.Text;
}
