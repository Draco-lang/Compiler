using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class Formatter : Api.Syntax.SyntaxVisitor
{
    private TokenDecoration[] tokenDecorations = [];
    private int currentIdx;
    private ScopeInfo scope;
    private readonly SyntaxTree tree; // debugging helper, to remove
    private ref TokenDecoration CurrentToken => ref this.tokenDecorations[this.currentIdx];

    private bool firstDeclaration = true;

    public static async SolverTask<string?> GetIndentation(IEnumerable<ScopeInfo> scopes)
    {
        var indentation = "";
        var prevKind = MaterialisationKind.Normal;
        foreach (var scope in scopes)
        {
            var isMaterialized = await scope.IsMaterialized.Collapsed;
            if (isMaterialized)
            {
                var previousIsStrong = prevKind == MaterialisationKind.Strong;
                prevKind = scope.MaterialisationKind;
                if (scope.MaterialisationKind == MaterialisationKind.Weak && previousIsStrong)
                {
                    continue;
                }
                indentation += scope.Indentation;
            }
        }
        return indentation;
    }

    /// <summary>
    /// Formats the given syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to format.</param>
    /// <param name="settings">The formatter settings to use.</param>
    /// <returns>The formatted tree.</returns>
    public static string Format(SyntaxTree tree, FormatterSettings? settings = null)
    {
        settings ??= FormatterSettings.Default;

        var formatter = new Formatter(settings, tree);

        tree.Root.Accept(formatter);
        var decorations = formatter.tokenDecorations;
        var currentLineLength = 0;
        var currentLineStart = 0;
        for (var x = 0; x < decorations.Length; x++)
        {
            var curr = decorations[x];
            if (curr.DoesReturnLineCollapsible?.Collapsed.Result ?? false)
            {
                currentLineLength = 0;
                currentLineStart = x;
            }

            currentLineLength += curr.CurrentTotalSize;
            if (currentLineLength > settings.LineWidth)
            {
                if (curr.ScopeInfo.Fold())
                {
                    x = currentLineStart;
                    continue;
                }
            }
        }

        var builder = new StringBuilder();
        var i = 0;
        foreach (var token in tree.Root.Tokens)
        {
            if (token.Kind == TokenKind.StringNewline)
            {
                i++;
                continue;
            }
            var decoration = formatter.tokenDecorations[i];

            if (decoration.DoesReturnLineCollapsible is not null)
            {
                builder.Append(decoration.DoesReturnLineCollapsible.Collapsed.Result ? settings.Newline : ""); // will default to false if not collapsed, that what we want.
            }
            if (decoration.Indentation is not null) builder.Append(decoration.Indentation.Result);
            builder.Append(decoration.LeftPadding);
            builder.Append(token.Text);
            builder.Append(decoration.RightPadding);
            i++;
        }
        builder.AppendLine();
        return builder.ToString();
    }

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private Formatter(FormatterSettings settings, SyntaxTree tree)
    {
        this.Settings = settings;
        this.tree = tree;
        this.scope = new(null, "");
        this.scope.IsMaterialized.Collapse(true);
    }

    public override void VisitCompilationUnit(Api.Syntax.CompilationUnitSyntax node)
    {
        this.tokenDecorations = new TokenDecoration[node.Tokens.Count()];
        base.VisitCompilationUnit(node);
    }

    public override void VisitSyntaxToken(Api.Syntax.SyntaxToken node)
    {
        this.CurrentToken.ScopeInfo = this.scope;
        switch (node.Kind)
        {
        case TokenKind.Minus:
        case TokenKind.Plus:
        case TokenKind.Star:
        case TokenKind.Slash:
        case TokenKind.GreaterThan:
        case TokenKind.LessThan:
        case TokenKind.GreaterEqual:
        case TokenKind.LessEqual:
        case TokenKind.Equal:
        case TokenKind.Assign:
        case TokenKind.KeywordMod:
            this.CurrentToken.RightPadding = " ";
            this.CurrentToken.LeftPadding = " ";
            break;
        case TokenKind.KeywordVar:
        case TokenKind.KeywordVal:
        case TokenKind.KeywordFunc:
        case TokenKind.KeywordReturn:
        case TokenKind.KeywordGoto:
        case TokenKind.KeywordWhile:
        case TokenKind.KeywordIf:
        case TokenKind.KeywordImport:
            this.CurrentToken.RightPadding = " ";
            break;
        }
        base.VisitSyntaxToken(node);
        this.CurrentToken.TokenSize = node.Green.Width;
        this.currentIdx++;
    }

    public override void VisitSeparatedSyntaxList<TNode>(Api.Syntax.SeparatedSyntaxList<TNode> node)
    {
        if (node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ParameterSyntax>
            || node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ExpressionSyntax>)
        {
            this.CreateFoldableScope(this.Settings.Indentation,
                MaterialisationKind.Normal,
                SolverTask.FromResult(FoldPriority.AsSoonAsPossible),
                () => base.VisitSeparatedSyntaxList(node)
            );
        }
        else
        {
            base.VisitSeparatedSyntaxList(node);
        }
    }

    public override void VisitParameter(Api.Syntax.ParameterSyntax node)
    {
        static async SolverTask<string?> VariableIndentation(ScopeInfo scope)
        {
            return await scope.IsMaterialized.Collapsed ? scope.Indentation[..^1] : null;
        }
        if (node.Index > 0)
        {
            this.CurrentToken.LeftPadding = " ";
        }
        this.CurrentToken.SetIndentation(VariableIndentation(this.scope));
        base.VisitParameter(node);
    }

    public override void VisitExpression(Api.Syntax.ExpressionSyntax node)
    {
        if (node.ArgumentIndex > 0)
        {
            this.CurrentToken.LeftPadding = " ";
        }
        base.VisitExpression(node);
    }

    public override void VisitDeclaration(Api.Syntax.DeclarationSyntax node)
    {
        if (node.Parent is not Api.Syntax.DeclarationStatementSyntax)
        {

            if (!this.firstDeclaration)
            {
                async SolverTask<string?> DoubleNewLine() => this.Settings.Newline + await GetIndentation(this.scope.ThisAndParents);
                this.CurrentToken.SetIndentation(DoubleNewLine());
            }
            else
            {
                this.CurrentToken.SetIndentation(SolverTask.FromResult(null as string));
            }
            this.firstDeclaration = false;
        }
        base.VisitDeclaration(node);
    }

    public override void VisitStringExpression(Api.Syntax.StringExpressionSyntax node)
    {
        if (node.OpenQuotes.Kind != TokenKind.MultiLineStringStart)
        {
            base.VisitStringExpression(node);
            return;
        }
        var i = 0;
        for (; i < node.Parts.Count; i++)
        {
            if (node.Parts[i].Children.SingleOrDefault() is Api.Syntax.SyntaxToken and { Kind: TokenKind.StringNewline })
            {
                continue;
            }
            ref var currentToken = ref this.tokenDecorations[this.currentIdx + i + 1];
            currentToken.SetIndentation(GetIndentation(this.scope.ThisAndParents));
        }
        using var _ = this.CreateFoldedScope(this.Settings.Indentation, MaterialisationKind.Normal);
        this.tokenDecorations[this.currentIdx + i + 1].SetIndentation(GetIndentation(this.scope.ThisAndParents));
        base.VisitStringExpression(node);
    }

    public override void VisitBinaryExpression(Api.Syntax.BinaryExpressionSyntax node)
    {
        IDisposable? closeScope = null;
        var kind = node.Operator.Kind;
        if (!(this.scope.Data?.Equals(kind) ?? false))
        {
            closeScope = this.CreateFoldableScope("", MaterialisationKind.Normal, SolverTask.FromResult(FoldPriority.AsLateAsPossible));
            this.scope.Data = kind;
        }
        node.Left.Accept(this);

        static async SolverTask<string?> Indentation(ScopeInfo scope)
        {
            var isCollapsed = await scope.IsMaterialized.Collapsed;
            if (!isCollapsed) return null;
            return await GetIndentation(scope.ThisAndParents);
        }

        this.CurrentToken.SetIndentation(Indentation(this.scope));
        node.Operator.Accept(this);
        node.Right.Accept(this);
        closeScope?.Dispose();
    }

    public override void VisitMemberExpression(Api.Syntax.MemberExpressionSyntax node)
    {
        base.VisitMemberExpression(node);
        this.scope.ItemsCount.Add(1);
    }

    public override void VisitFunctionDeclaration(Api.Syntax.FunctionDeclarationSyntax node)
    {
        base.VisitFunctionDeclaration(node);
    }

    public override void VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        this.CurrentToken.LeftPadding = " ";
        this.CreateFoldedScope(this.Settings.Indentation, MaterialisationKind.Strong, () => base.VisitBlockFunctionBody(node));
        this.tokenDecorations[this.currentIdx - 1].SetIndentation(GetIndentation(this.scope.ThisAndParents));
    }

    public override void VisitInlineFunctionBody(Api.Syntax.InlineFunctionBodySyntax node)
    {
        var parent = (Api.Syntax.FunctionDeclarationSyntax)node.Parent!;
        using var _ = this.CreateFoldableScope(new string(' ', 7 + parent.Name.Span.Length + parent.ParameterList.Span.Length),
            MaterialisationKind.Weak,
            SolverTask.FromResult(FoldPriority.AsSoonAsPossible)
        );
        base.VisitInlineFunctionBody(node);
    }

    public override void VisitStatement(Api.Syntax.StatementSyntax node)
    {
        this.scope.ItemsCount.Add(1);

        if (node is Api.Syntax.DeclarationStatementSyntax { Declaration: Api.Syntax.LabelDeclarationSyntax })
        {
            this.CurrentToken.SetIndentation(GetIndentation(this.scope.Parents));
        }
        else if (node.Parent is Api.Syntax.BlockExpressionSyntax || node.Parent is Api.Syntax.BlockFunctionBodySyntax)
        {
            this.CurrentToken.SetIndentation(GetIndentation(this.scope.ThisAndParents));
        }
        else
        {
            async SolverTask<string?> Indentation()
            {
                var haveMoreThanOneStatement = await this.scope.ItemsCount.WhenGreaterOrEqual(2);
                if (haveMoreThanOneStatement) return await GetIndentation(this.scope.ThisAndParents);
                return null;
            }
            this.CurrentToken.SetIndentation(Indentation());

        }
        base.VisitStatement(node);
    }

    public override void VisitWhileExpression(Api.Syntax.WhileExpressionSyntax node)
    {
        this.tokenDecorations[this.currentIdx + 2 + node.Condition.Tokens.Count()].RightPadding = " ";
        this.CreateFoldableScope(this.Settings.Indentation, MaterialisationKind.Normal, SolverTask.FromResult(FoldPriority.AsSoonAsPossible), () => base.VisitWhileExpression(node));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node)
    {
        this.tokenDecorations[this.currentIdx + 2 + node.Condition.Tokens.Count()].RightPadding = " ";
        this.CreateFoldableScope(this.Settings.Indentation, MaterialisationKind.Normal, SolverTask.FromResult(FoldPriority.AsSoonAsPossible), () =>
        {
            this.VisitExpression(node);
            node.IfKeyword.Accept(this);
            node.OpenParen.Accept(this);
            node.Condition.Accept(this);
            node.CloseParen.Accept(this);
            node.Then.Accept(this);
        });
        node.Else?.Accept(this);
    }

    public override void VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        this.CurrentToken.RightPadding = " ";
        if (node.Parent!.Parent is Api.Syntax.ExpressionStatementSyntax)
        {
            this.CurrentToken.SetIndentation(GetIndentation(this.scope.ThisAndParents));
        }
        else
        {
            this.CurrentToken.LeftPadding = " ";
        }
        this.CreateFoldableScope(this.Settings.Indentation, MaterialisationKind.Normal, SolverTask.FromResult(FoldPriority.AsSoonAsPossible), () => base.VisitElseClause(node));
    }

    public override void VisitBlockExpression(Api.Syntax.BlockExpressionSyntax node)
    {
        // this means we are in a if/while/else, and *can* create an indentation with a regular expression folding:
        // if (blabla) an expression;
        // it can fold:
        // if (blabla)
        //     an expression;
        // but since we are in a block we create our own scope and the if/while/else will never create it's own scope.
        this.scope.IsMaterialized.TryCollapse(false);

        this.CreateFoldedScope(this.Settings.Indentation, MaterialisationKind.Strong, () =>
        {
            this.VisitExpression(node);
            node.OpenBrace.Accept(this);
            node.Statements.Accept(this);
            if (node.Value != null)
            {
                this.CurrentToken.SetIndentation(GetIndentation(this.scope.ThisAndParents));
                node.Value.Accept(this);
            }
            node.CloseBrace.Accept(this);
        });
        this.tokenDecorations[this.currentIdx - 1].SetIndentation(GetIndentation(this.scope.ThisAndParents));
    }

    public override void VisitTypeSpecifier(Api.Syntax.TypeSpecifierSyntax node)
    {
        this.CurrentToken.RightPadding = " ";
        base.VisitTypeSpecifier(node);
    }

    private IDisposable CreateFoldedScope(string indentation, MaterialisationKind materialisationKind)
    {
        this.scope = new ScopeInfo(this.scope, indentation);
        this.scope.IsMaterialized.Collapse(true);
        this.scope.MaterialisationKind = materialisationKind;
        return new DisposeAction(() =>
        {
            this.scope.Dispose();
            this.scope = this.scope.Parent!;
        });
    }

    private void CreateFoldedScope(string indentation, MaterialisationKind materialisationKind, Action action)
    {
        using (this.CreateFoldedScope(indentation, materialisationKind)) action();
    }

    private IDisposable CreateFoldableScope(string indentation, MaterialisationKind materialisationKind, SolverTask<FoldPriority> foldBehavior)
    {
        this.scope = new ScopeInfo(this.scope, indentation, foldBehavior)
        {
            MaterialisationKind = materialisationKind
        };
        return new DisposeAction(() =>
        {
            this.scope.Dispose();
            this.scope = this.scope.Parent!;
        });
    }

    private void CreateFoldableScope(string indentation, MaterialisationKind materialisationKind, SolverTask<FoldPriority> foldBehavior, Action action)
    {
        using (this.CreateFoldableScope(indentation, materialisationKind, foldBehavior)) action();
    }
}
