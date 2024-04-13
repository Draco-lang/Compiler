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

        foreach (var scope in scopes)
        {
            var isMaterialized = await scope.IsMaterialized.Collapsed;
            if (isMaterialized)
            {
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

            builder.Append(decoration.TokenOverride ?? token.Text);
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
        node.OpenQuotes.Accept(this);
        using var _ = this.CreateFoldedScope(this.Settings.Indentation);
        var blockCurrentIndentCount = node.CloseQuotes.LeadingTrivia.Aggregate(0, (value, right) =>
        {
            if (right.Kind == TriviaKind.Newline) return 0;
            return value + right.Span.Length;
        });
        var i = 0;
        var newLineCount = 1;
        var shouldIndent = true;

        for (; i < node.Parts.Count; i++)
        {
            var curr = node.Parts[i];

            var isNewLine = curr.Children.Count() == 1 && curr.Children.SingleOrDefault() is Api.Syntax.SyntaxToken and { Kind: TokenKind.StringNewline };
            if (shouldIndent)
            {
                var tokenText = curr.Tokens.First().ValueText!;
                if (!tokenText.Take(blockCurrentIndentCount).All(char.IsWhiteSpace)) throw new InvalidOperationException();
                this.tokenDecorations[this.currentIdx].TokenOverride = tokenText.Substring(blockCurrentIndentCount);
                MultiIndent(newLineCount);
                shouldIndent = false;
            }

            if (isNewLine)
            {
                newLineCount++;
                shouldIndent = true;
            }
            else
            {
                newLineCount = 0;
            }

            var tokenCount = curr.Tokens.Count();
            for (var j = 0; j < tokenCount; j++)
            {
                ref var decoration = ref this.tokenDecorations[this.currentIdx + j];
                if (decoration.Indentation is null)
                {
                    decoration.SetIndentation(SolverTask.FromResult<string?>(null));
                }
            }
            curr.Accept(this);
        }
        MultiIndent(newLineCount);
        this.tokenDecorations[this.currentIdx].SetIndentation(GetIndentation(this.scope.ThisAndParents));
        node.CloseQuotes.Accept(this);


        void MultiIndent(int newLineCount)
        {
            if (newLineCount > 0)
            {
                ref var currentToken = ref this.tokenDecorations[this.currentIdx];
                currentToken.SetIndentation(GetIndentation(this.scope.ThisAndParents));
                if (newLineCount > 1)
                {
                    currentToken.LeftPadding = string.Concat(Enumerable.Repeat(this.Settings.Newline, newLineCount - 1));
                }
            }
        }
    }

    public override void VisitBinaryExpression(Api.Syntax.BinaryExpressionSyntax node)
    {
        IDisposable? closeScope = null;
        var kind = node.Operator.Kind;
        if (!(this.scope.Data?.Equals(kind) ?? false))
        {
            closeScope = this.CreateFoldableScope("", SolverTask.FromResult(FoldPriority.AsLateAsPossible));
            this.scope.Data = kind;
        }
        node.Left.Accept(this);

        static async SolverTask<string?> Indentation(ScopeInfo scope)
        {
            var isCollapsed = await scope.IsMaterialized.Collapsed;
            if (!isCollapsed) return null;
            return await GetIndentation(scope.ThisAndParents);
        }
        if (this.CurrentToken.Indentation is null)
        {
            this.CurrentToken.SetIndentation(Indentation(this.scope));
        }
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
        this.CreateFoldedScope(this.Settings.Indentation, () => base.VisitBlockFunctionBody(node));
        this.tokenDecorations[this.currentIdx - 1].SetIndentation(GetIndentation(this.scope.ThisAndParents));
    }

    public override void VisitInlineFunctionBody(Api.Syntax.InlineFunctionBodySyntax node)
    {
        var parent = (Api.Syntax.FunctionDeclarationSyntax)node.Parent!;
        using var _ = this.CreateFoldableScope(new string(' ', 7 + parent.Name.Span.Length + parent.ParameterList.Span.Length),
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
        this.CreateFoldableScope(this.Settings.Indentation, SolverTask.FromResult(FoldPriority.AsSoonAsPossible), () => base.VisitWhileExpression(node));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node)
    {
        this.tokenDecorations[this.currentIdx + 2 + node.Condition.Tokens.Count()].RightPadding = " ";
        void Visit()
        {
            this.VisitExpression(node);
            node.IfKeyword.Accept(this);
            node.OpenParen.Accept(this);
            node.Condition.Accept(this);
            node.CloseParen.Accept(this);
            node.Then.Accept(this);
        }

        if (this.scope.ItemsCount.MinimumCurrentValue > 1)
        {
            Visit();
        }
        else
        {
            this.CreateFoldableScope(this.Settings.Indentation, SolverTask.FromResult(FoldPriority.AsSoonAsPossible), Visit);
        }
        node.Else?.Accept(this);
    }

    public override void VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        var isElseIf = node.Expression is Api.Syntax.StatementExpressionSyntax a && a.Statement is Api.Syntax.ExpressionStatementSyntax s && s.Expression is Api.Syntax.IfExpressionSyntax;
        this.CurrentToken.RightPadding = " ";
        if (isElseIf || node.Parent!.Parent is Api.Syntax.ExpressionStatementSyntax)
        {
            this.CurrentToken.SetIndentation(GetIndentation(this.scope.ThisAndParents));
        }
        else
        {
            this.CurrentToken.LeftPadding = " ";
            this.CurrentToken.SetIndentation(VariableIndentation(this.scope));
        }
        this.CreateFoldableScope(this.Settings.Indentation, SolverTask.FromResult(FoldPriority.AsSoonAsPossible), () => base.VisitElseClause(node));
    }

    private static async SolverTask<string?> VariableIndentation(ScopeInfo scope)
    {
        return await scope.IsMaterialized.Collapsed ? scope.Indentation[..^1] : null;
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

        this.CreateFoldedScope(this.Settings.Indentation, () =>
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

    private IDisposable CreateFoldedScope(string indentation)
    {
        this.scope = new ScopeInfo(this.scope, indentation);
        this.scope.IsMaterialized.Collapse(true);
        return new DisposeAction(() =>
        {
            this.scope.Dispose();
            this.scope = this.scope.Parent!;
        });
    }

    private void CreateFoldedScope(string indentation, Action action)
    {
        using (this.CreateFoldedScope(indentation)) action();
    }

    private IDisposable CreateFoldableScope(string indentation, SolverTask<FoldPriority> foldBehavior)
    {
        this.scope = new ScopeInfo(this.scope, indentation, foldBehavior);
        return new DisposeAction(() =>
        {
            this.scope.Dispose();
            this.scope = this.scope.Parent!;
        });
    }

    private void CreateFoldableScope(string indentation, SolverTask<FoldPriority> foldBehavior, Action action)
    {
        using (this.CreateFoldableScope(indentation, foldBehavior)) action();
    }
}
