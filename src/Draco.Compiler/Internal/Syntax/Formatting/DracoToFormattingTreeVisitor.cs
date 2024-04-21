using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;
internal class DracoToFormattingTreeVisitor : Api.Syntax.SyntaxVisitor<FormattingNode>
{
    private GroupsNode root = null!;
    private FormattingNode current = null!;

    public DracoToFormattingTreeVisitor()
    {
    }

    public override FormattingNode VisitCompilationUnit(Api.Syntax.CompilationUnitSyntax node)
    {
        this.root = new GroupsNode() { Parent = null };
        this.current = this.root;
        using var _ = this.Scope(this.root);
        foreach (var declaration in node.Declarations)
        {
            // TODO: separate group type by declaration kind
            this.root.Items.Add(new GroupsNode.GroupInfo("TODO", declaration.Accept(this)));
        }
        return this.root;
    }

    public override FormattingNode VisitSeparatedSyntaxList<TNode>(Api.Syntax.SeparatedSyntaxList<TNode> node)
    {
        if (!node.Values.Any()) return default!;
        if (!node.Values.Skip(1).Any())
        {
            return node.Values.First().Accept(this);
        }
        var list = new SeparatedListNode()
        {
            Separator = node.Separators.First().Text,
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        foreach (var item in node.Values)
        {
            list.Items.Add(item.Accept(this));
        }
        return list;
    }

    public override FormattingNode VisitBlockExpression(Api.Syntax.BlockExpressionSyntax node)
    {
        var block = new CodeBlockNode()
        {
            CodeBlockKind = "BlockExpression",
            Parent = this.current,
            Opening = (TokenNode)node.OpenBrace.Accept(this),
            ClosingString = (TokenNode)node.CloseBrace.Accept(this),
        };
        var group = new GroupsNode() { Parent = block }; ;
        block.Content = group;
        using var _ = this.Scope(block);
        foreach (var statement in node.Statements)
        {
            // TODO: separate group type by statement/declaration
            group.Items.Add(new GroupsNode.GroupInfo("TODO", statement.Accept(this)));
        }
        if (node.Value != null)
        {
            group.Items.Add(new GroupsNode.GroupInfo("Value", node.Value.Accept(this)));
        }
        return block;
    }

    public override FormattingNode VisitFunctionDeclaration(Api.Syntax.FunctionDeclarationSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "FunctionDeclaration",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        if (node.VisibilityModifier != null)
        {
            list.Items.Add(node.VisibilityModifier.Accept(this));
        }
        list.Items.Add(node.FunctionKeyword.Accept(this));
        list.Items.Add(node.Name.Accept(this));
        list.Items.Add(node.OpenParen.Accept(this));
        if (node.ParameterList.Values.Any())
        {
            list.Items.Add(node.ParameterList.Accept(this));
        }
        list.Items.Add(node.CloseParen.Accept(this));
        if (node.ReturnType != null)
        {
            list.Items.Add(node.ReturnType.Accept(this));
        }
        list.Items.Add(node.Body.Accept(this));
        return list;
    }

    public override FormattingNode VisitParameter(Api.Syntax.ParameterSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "Parameter",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        if (node.Variadic != null)
        {
            list.Items.Add(node.Variadic.Accept(this));
        }
        list.Items.Add(node.Name.Accept(this));
        list.Items.Add(node.Colon.Accept(this));
        list.Items.Add(node.Type.Accept(this));
        return list;
    }

    public override FormattingNode VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        var block = new CodeBlockNode()
        {
            CodeBlockKind = "BlockFunctionBody",
            Parent = this.current,
            Opening = (TokenNode)node.OpenBrace.Accept(this),
            ClosingString = (TokenNode)node.CloseBrace.Accept(this),
        };
        var group = new GroupsNode() { Parent = block }; ;
        block.Content = group;
        using var _ = this.Scope(block);
        foreach (var statement in node.Statements)
        {
            // TODO: separate group type by statement/declaration
            group.Items.Add(new GroupsNode.GroupInfo("TODO", statement.Accept(this)));
        }
        return block;
    }

    public override FormattingNode VisitInlineFunctionBody(Api.Syntax.InlineFunctionBodySyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "InlineFunctionBody",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.Assign.Accept(this));
        list.Items.Add(node.Value.Accept(this));
        list.Items.Add(node.Semicolon.Accept(this));
        return list;
    }

    public override FormattingNode VisitBinaryExpression(Api.Syntax.BinaryExpressionSyntax node)
    {
        var operatorNode = new OperatorNode()
        {
            Operator = node.Operator.Text,
            Parent = this.current,
        };
        using var _ = this.Scope(operatorNode);
        operatorNode.Left = node.Left.Accept(this);
        operatorNode.Right = node.Right.Accept(this);
        return operatorNode;
    }

    public override FormattingNode VisitCallExpression(Api.Syntax.CallExpressionSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "CallExpression",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.Function.Accept(this));
        var argBlock = new CodeBlockNode()
        {
            CodeBlockKind = "Arguments",
            Parent = list,
            Opening = (TokenNode)node.OpenParen.Accept(this),
            ClosingString = (TokenNode)node.CloseParen.Accept(this)
        };
        list.Items.Add(argBlock);
        using var __ = this.Scope(argBlock);
        argBlock.Content = node.ArgumentList.Accept(this);
        return list;
    }

    public override FormattingNode VisitRelationalExpression(Api.Syntax.RelationalExpressionSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "RelationalExpression",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.Left.Accept(this));
        foreach (var comparison in node.Comparisons)
        {
            list.Items.Add(comparison.Operator.Accept(this));
            list.Items.Add(comparison.Right.Accept(this));
        }
        return list;
    }

    public override FormattingNode VisitComparisonElement(Api.Syntax.ComparisonElementSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "ComparisonElement",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.Operator.Accept(this));
        list.Items.Add(node.Right.Accept(this));
        return list;
    }

    public override FormattingNode VisitIfExpression(Api.Syntax.IfExpressionSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "IfExpression",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.IfKeyword.Accept(this));
        list.Items.Add(node.OpenParen.Accept(this));
        list.Items.Add(node.Condition.Accept(this));
        list.Items.Add(node.CloseParen.Accept(this));
        list.Items.Add(node.Then.Accept(this));
        if (node.Else != null)
        {
            list.Items.Add(node.Else.Accept(this));
        }
        return list;
    }

    public override FormattingNode VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "ElseClause",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.ElseKeyword.Accept(this));
        list.Items.Add(node.Expression.Accept(this));
        return list;
    }


    public override FormattingNode VisitExpressionStatement(Api.Syntax.ExpressionStatementSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "ExpressionStatement",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.Expression.Accept(this));
        if (node.Semicolon != null)
        {
            list.Items.Add(node.Semicolon.Accept(this));
        }
        return list;
    }

    public override FormattingNode VisitVariableDeclaration(Api.Syntax.VariableDeclarationSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "VariableDeclaration",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        if (node.VisibilityModifier != null)
        {
            list.Items.Add(node.VisibilityModifier.Accept(this));
        }
        list.Items.Add(node.Keyword.Accept(this));
        list.Items.Add(node.Name.Accept(this));
        if (node.Type != null)
        {
            list.Items.Add(node.Type.Accept(this));
        }

        if (node.Value != null)
        {
            list.Items.Add(node.Value.Accept(this));
        }
        if (node.Semicolon != null)
        {
            list.Items.Add(node.Semicolon.Accept(this));
        }
        return list;
    }

    public override FormattingNode VisitValueSpecifier(Api.Syntax.ValueSpecifierSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "ValueSpecifier",
            Parent = this.current,
        };

        using var _ = this.Scope(list);
        list.Items.Add(node.Assign.Accept(this));
        list.Items.Add(node.Value.Accept(this));
        return list;
    }

    public override FormattingNode VisitStringExpression(Api.Syntax.StringExpressionSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "StringExpression",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.OpenQuotes.Accept(this));
        list.Items.Add(node.Parts.Accept(this));
        list.Items.Add(node.CloseQuotes.Accept(this));
        return list;
    }

    public override FormattingNode VisitSyntaxList<TNode>(Api.Syntax.SyntaxList<TNode> node)
    {
        var list = new ListNode()
        {
            ListKind = "SyntaxList",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        foreach (var item in node)
        {
            list.Items.Add(item.Accept(this));
        }
        return list;
    }

    public override FormattingNode VisitInterpolationStringPart(Api.Syntax.InterpolationStringPartSyntax node)
    {
        var codeBlock = new CodeBlockNode()
        {
            CodeBlockKind = "InterpolationStringPart",
            Parent = this.current,
            Opening = (TokenNode)node.Open.Accept(this),
            ClosingString = (TokenNode)node.Close.Accept(this),
        };
        using var _ = this.Scope(codeBlock);
        codeBlock.Content = node.Expression.Accept(this);
        return codeBlock;
    }


    public override FormattingNode VisitImportDeclaration(Api.Syntax.ImportDeclarationSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "ImportDeclaration",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.ImportKeyword.Accept(this));
        list.Items.Add(node.Path.Accept(this));
        list.Items.Add(node.Semicolon.Accept(this));
        return list;
    }

    public override FormattingNode VisitMemberImportPath(Api.Syntax.MemberImportPathSyntax node)
    {
        // unrecursive the tree into a list
        var list = new SeparatedListNode()
        {
            Parent = this.current,
            Separator = "."
        };
        using var _ = this.Scope(list);
        var toReverse = new List<FormattingNode>();
        var current = node;
        while (true)
        {
            toReverse.Add(current.Member.Accept(this));
            if (current.Parent is not Api.Syntax.MemberImportPathSyntax parent)
            {
                var asRoot = (Api.Syntax.RootImportPathSyntax)current.Accessed!;
                toReverse.Add(asRoot.Name.Accept(this));
                break;
            }
            current = parent;
        }
        list.Items.AddRange(toReverse.Reverse<FormattingNode>());
        return list;
    }

    public override FormattingNode VisitLabelDeclaration(Api.Syntax.LabelDeclarationSyntax node)
    {
        var list = new ListNode()
        {
            ListKind = "LabelDeclaration",
            Parent = this.current,
        };
        using var _ = this.Scope(list);
        list.Items.Add(node.Name.Accept(this));
        list.Items.Add(node.Colon.Accept(this));
        return list;
    }


    public override FormattingNode VisitSyntaxToken(Api.Syntax.SyntaxToken node) => new TokenNode()
    {
        Parent = this.current,
        Text = node.Text
    };

    public override FormattingNode VisitNameExpression(Api.Syntax.NameExpressionSyntax node) => new TokenNode()
    {
        Parent = this.current,
        Text = node.Name.Text
    };
    public override FormattingNode VisitTextStringPart(Api.Syntax.TextStringPartSyntax node) => new TokenNode()
    {
        //TODO: remove indentation of previous code here.
        Parent = this.current,
        Text = node.Content.Text
    };

    public override FormattingNode VisitLiteralExpression(Api.Syntax.LiteralExpressionSyntax node) => new TokenNode()
    {
        Parent = this.current,
        Text = node.Literal.Text
    };

    public override FormattingNode VisitStatementExpression(Api.Syntax.StatementExpressionSyntax node) => node.Statement.Accept(this);
    public override FormattingNode VisitDeclarationStatement(Api.Syntax.DeclarationStatementSyntax node) => node.Declaration.Accept(this);
    public override FormattingNode VisitNameType(Api.Syntax.NameTypeSyntax node) => node.Name.Accept(this);
    

    private DisposeAction Scope(FormattingNode node)
    {
        var prevCurrent = this.current;
        this.current = node;
        return new DisposeAction(() =>
        {
            this.current = prevCurrent;
        });
    }
}

internal abstract class FormattingNode
{
    public required FormattingNode? Parent { get; init; }

    public string EscapedString => this.ToString().Replace("\n", "\\n").Replace("\r", "\\r");
}

/// <summary>
/// Groups separated by a new line.
/// For example, 
/// </summary>
internal class GroupsNode : FormattingNode
{
    public List<GroupInfo> Items { get; } = [];

    public class GroupInfo
    {
        public GroupInfo(string ItemGroupKind, FormattingNode Node)
        {
            this.ItemGroupKind = ItemGroupKind ?? throw new ArgumentNullException(nameof(ItemGroupKind));
            this.Node = Node ?? throw new ArgumentNullException(nameof(Node));
        }

        public string ItemGroupKind { get; }
        public FormattingNode Node { get; }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var item in this.Items)
        {
            sb.AppendLine(item.Node.ToString());
        }
        return sb.ToString();
    }
}

internal class TokenNode : FormattingNode
{
    public required string Text { get; init; }

    public override string ToString() => this.Text;
}

internal class CodeBlockNode : FormattingNode
{
    public required string CodeBlockKind { get; init; }
    public required TokenNode Opening { get; init; }
    public FormattingNode Content { get; set; }
    public required TokenNode ClosingString { get; init; }

    public override string ToString() => $"{this.Opening}\n{this.Content}\n{this.ClosingString}";
}

internal class ListNode : FormattingNode
{
    public required string ListKind { get; init; }
    public List<FormattingNode> Items { get; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < this.Items.Count; i++)
        {
            sb.Append(this.Items[i]);
            var isLast = i == this.Items.Count - 1;
            if (!isLast)
            {
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }
}

internal class SeparatedListNode : FormattingNode
{
    public required string Separator { get; init; }
    public List<FormattingNode> Items { get; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < this.Items.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(this.Separator);
                sb.Append(' ');
            }
            sb.Append(this.Items[i]);
        }
        return sb.ToString();
    }
}

internal class OperatorNode : FormattingNode
{
    public required string Operator { get; init; }
    public FormattingNode Left { get; set; }
    public FormattingNode Right { get; set; }

    public override string ToString() => $"{this.Left} {this.Operator} {this.Right}";
}
