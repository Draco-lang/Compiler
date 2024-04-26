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
    private ref TokenDecoration CurrentToken => ref this.tokenDecorations[this.currentIdx];

    private bool firstDeclaration = true;

    /// <summary>
    /// Formats the given syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to format.</param>
    /// <param name="settings">The formatter settings to use.</param>
    /// <returns>The formatted tree.</returns>
    public static string Format(SyntaxTree tree, FormatterSettings? settings = null)
    {
        settings ??= FormatterSettings.Default;

        var formatter = new Formatter(settings);

        tree.Root.Accept(formatter);

        var decorations = formatter.tokenDecorations;
        var tokens = tree.Root.Tokens.ToArray();
        var stateMachine = new LineStateMachine(string.Concat(decorations[0].ScopeInfo.CurrentTotalIndent));
        var currentLineStart = 0;
        for (var x = 0; x < decorations.Length; x++)
        {
            var curr = decorations[x];
            var token = tokens[x];
            if (curr.DoesReturnLineCollapsible?.Collapsed.Result ?? false)
            {
                stateMachine = new LineStateMachine(string.Concat(curr.ScopeInfo.CurrentTotalIndent));
                currentLineStart = x;
            }
            stateMachine.AddToken(curr, token);
            if (stateMachine.LineWidth > settings.LineWidth)
            {
                if (curr.ScopeInfo.Fold())
                {
                    x = currentLineStart - 1;
                    stateMachine.Reset();
                    continue;
                }
            }
        }

        var builder = new StringBuilder();
        stateMachine = new LineStateMachine(string.Concat(decorations[0].ScopeInfo.CurrentTotalIndent));
        for (var x = 0; x < decorations.Length; x++)
        {
            var token = tokens[x];
            if (token.Kind == TokenKind.StringNewline) continue;

            var decoration = decorations[x];

            if (decoration.DoesReturnLineCollapsible?.Collapsed.Result ?? false)
            {
                builder.Append(stateMachine);
                builder.Append(settings.Newline);
                stateMachine = new LineStateMachine(string.Concat(decoration.ScopeInfo.CurrentTotalIndent));
            }
            if (decoration.Kind.HasFlag(FormattingTokenKind.ExtraNewline) && x > 0)
            {
                builder.Append(settings.Newline);
            }

            stateMachine.AddToken(decoration, token);
        }
        builder.Append(stateMachine);
        builder.AppendLine();
        return builder.ToString();
    }

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
        this.scope = new(null, FoldPriority.Never, "");
        this.scope.IsMaterialized.Collapse(true);
    }

    public override void VisitCompilationUnit(Api.Syntax.CompilationUnitSyntax node)
    {
        this.tokenDecorations = new TokenDecoration[node.Tokens.Count()];
        base.VisitCompilationUnit(node);
    }

    public override void VisitSyntaxToken(Api.Syntax.SyntaxToken node)
    {
        static FormattingTokenKind GetFormattingTokenKind(Api.Syntax.SyntaxToken token) => token.Kind switch
        {
            TokenKind.KeywordAnd => FormattingTokenKind.PadLeft,
            TokenKind.KeywordElse => FormattingTokenKind.PadLeft,
            TokenKind.KeywordFalse => FormattingTokenKind.PadLeft,
            TokenKind.KeywordFor => FormattingTokenKind.PadLeft,
            TokenKind.KeywordGoto => FormattingTokenKind.PadLeft,
            TokenKind.KeywordImport => FormattingTokenKind.PadLeft,
            TokenKind.KeywordIn => FormattingTokenKind.PadLeft,
            TokenKind.KeywordInternal => FormattingTokenKind.PadLeft,
            TokenKind.KeywordMod => FormattingTokenKind.PadLeft,
            TokenKind.KeywordModule => FormattingTokenKind.PadLeft,
            TokenKind.KeywordOr => FormattingTokenKind.PadLeft,
            TokenKind.KeywordRem => FormattingTokenKind.PadLeft,
            TokenKind.KeywordReturn => FormattingTokenKind.PadLeft,
            TokenKind.KeywordPublic => FormattingTokenKind.PadLeft,
            TokenKind.KeywordTrue => FormattingTokenKind.PadLeft,
            TokenKind.KeywordVar => FormattingTokenKind.PadLeft,
            TokenKind.KeywordVal => FormattingTokenKind.PadLeft,


            TokenKind.KeywordFunc => FormattingTokenKind.ExtraNewline,

            TokenKind.KeywordIf => FormattingTokenKind.PadAround,
            TokenKind.KeywordWhile => FormattingTokenKind.PadAround,

            TokenKind.Semicolon => FormattingTokenKind.Semicolon,
            TokenKind.CurlyOpen => FormattingTokenKind.PadLeft | FormattingTokenKind.TreatAsWhitespace,
            TokenKind.ParenOpen => FormattingTokenKind.TreatAsWhitespace,
            TokenKind.InterpolationStart => FormattingTokenKind.TreatAsWhitespace,
            TokenKind.Dot => FormattingTokenKind.TreatAsWhitespace,

            TokenKind.Assign => FormattingTokenKind.PadLeft,
            TokenKind.LineStringStart => FormattingTokenKind.PadLeft,
            TokenKind.MultiLineStringStart => FormattingTokenKind.PadLeft,
            TokenKind.Plus => FormattingTokenKind.PadLeft,
            TokenKind.Minus => FormattingTokenKind.PadLeft,
            TokenKind.Star => FormattingTokenKind.PadLeft,
            TokenKind.Slash => FormattingTokenKind.PadLeft,
            TokenKind.PlusAssign => FormattingTokenKind.PadLeft,
            TokenKind.MinusAssign => FormattingTokenKind.PadLeft,
            TokenKind.StarAssign => FormattingTokenKind.PadLeft,
            TokenKind.SlashAssign => FormattingTokenKind.PadLeft,
            TokenKind.GreaterEqual => FormattingTokenKind.PadLeft,
            TokenKind.GreaterThan => FormattingTokenKind.PadLeft,
            TokenKind.LessEqual => FormattingTokenKind.PadLeft,
            TokenKind.LessThan => FormattingTokenKind.PadLeft,

            TokenKind.LiteralFloat => FormattingTokenKind.PadLeft,
            TokenKind.LiteralInteger => FormattingTokenKind.PadLeft,

            TokenKind.Identifier => FormattingTokenKind.PadLeft,

            _ => FormattingTokenKind.NoFormatting
        };

        this.CurrentToken.ScopeInfo = this.scope;
        this.CurrentToken.Kind = GetFormattingTokenKind(node);
        this.CurrentToken.Token = node;
        base.VisitSyntaxToken(node);
        this.currentIdx++;
    }

    public override void VisitSeparatedSyntaxList<TNode>(Api.Syntax.SeparatedSyntaxList<TNode> node)
    {
        if (node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ParameterSyntax>
            || node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ExpressionSyntax>)
        {
            this.CreateFoldableScope(this.Settings.Indentation,
                FoldPriority.AsSoonAsPossible,
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
        this.CurrentToken.DoesReturnLineCollapsible = this.scope.IsMaterialized;
        base.VisitParameter(node);
    }

    public override void VisitDeclaration(Api.Syntax.DeclarationSyntax node)
    {
        if (node.Parent is not Api.Syntax.DeclarationStatementSyntax)
        {
            if (!this.firstDeclaration)
            {
                this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.True;
            }
            else
            {
                this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.False;
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
                this.tokenDecorations[this.currentIdx].TokenOverride = tokenText[blockCurrentIndentCount..];
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
                if (decoration.DoesReturnLineCollapsible is null)
                {
                    decoration.DoesReturnLineCollapsible = CollapsibleBool.False;
                }
            }
            curr.Accept(this);
        }
        MultiIndent(newLineCount);
        this.tokenDecorations[this.currentIdx].DoesReturnLineCollapsible = CollapsibleBool.True;
        node.CloseQuotes.Accept(this);


        void MultiIndent(int newLineCount)
        {
            if (newLineCount > 0)
            {
                ref var currentToken = ref this.tokenDecorations[this.currentIdx];
                currentToken.DoesReturnLineCollapsible = CollapsibleBool.True;
                if (newLineCount > 1)
                {
                    // TODO
                    //currentToken.LeftPadding = string.Concat(Enumerable.Repeat(this.Settings.Newline, newLineCount - 1));
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
            closeScope = this.CreateFoldableScope("", FoldPriority.AsLateAsPossible);
            this.scope.Data = kind;
        }
        node.Left.Accept(this);

        if (this.CurrentToken.DoesReturnLineCollapsible is null)
        {
            this.CurrentToken.DoesReturnLineCollapsible = this.scope.IsMaterialized;
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

    public override void VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        node.OpenBrace.Accept(this);
        this.CreateFoldedScope(this.Settings.Indentation, () => node.Statements.Accept(this));
        this.tokenDecorations[this.currentIdx].DoesReturnLineCollapsible = CollapsibleBool.True;
        node.CloseBrace.Accept(this);
    }

    public override void VisitInlineFunctionBody(Api.Syntax.InlineFunctionBodySyntax node)
    {
        var parent = (Api.Syntax.FunctionDeclarationSyntax)node.Parent!;
        using var _ = this.CreateFoldableScope(new string(' ', 7 + parent.Name.Span.Length + parent.ParameterList.Span.Length),
            FoldPriority.AsSoonAsPossible
        );
        base.VisitInlineFunctionBody(node);
    }

    public override void VisitFunctionDeclaration(Api.Syntax.FunctionDeclarationSyntax node)
    {
        node.VisibilityModifier?.Accept(this);
        node.FunctionKeyword.Accept(this);
        node.Name.Accept(this);
        if (node.Generics is not null)
        {
            this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsLateAsPossible, () => node.Generics?.Accept(this));
        }
        node.OpenParen.Accept(this);
        this.CreateFoldableScope()
        node.ParameterList.Accept(this);
        node.CloseParen.Accept(this);
        node.ReturnType?.Accept(this);
        node.Body.Accept(this);
    }

    public override void VisitStatement(Api.Syntax.StatementSyntax node)
    {
        this.scope.ItemsCount.Add(1);

        if (node is Api.Syntax.DeclarationStatementSyntax { Declaration: Api.Syntax.LabelDeclarationSyntax })
        {
            // TODO: special case where we un-nest a level.
            this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.True;
        }
        else if (node.Parent is Api.Syntax.BlockExpressionSyntax || node.Parent is Api.Syntax.BlockFunctionBodySyntax)
        {
            this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.True;
        }
        else
        {
            this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.Create(this.scope.ItemsCount.WhenGreaterOrEqual(2));
        }
        base.VisitStatement(node);
    }

    public override void VisitWhileExpression(Api.Syntax.WhileExpressionSyntax node)
    {
        this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => base.VisitWhileExpression(node));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node)
    {
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
            this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, Visit);
        }
        node.Else?.Accept(this);
    }

    public override void VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        if (node.IsElseIf || node.Parent!.Parent is Api.Syntax.ExpressionStatementSyntax)
        {
            this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.True;
        }
        else
        {
            this.CurrentToken.DoesReturnLineCollapsible = this.scope.IsMaterialized;
        }
        this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => base.VisitElseClause(node));
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
                this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.True;
                node.Value.Accept(this);
            }
            node.CloseBrace.Accept(this);
        });
        this.tokenDecorations[this.currentIdx - 1].DoesReturnLineCollapsible = CollapsibleBool.True;
    }

    public override void VisitTypeSpecifier(Api.Syntax.TypeSpecifierSyntax node)
    {
        base.VisitTypeSpecifier(node);
    }

    private IDisposable CreateFoldedScope(string indentation)
    {
        this.scope = new ScopeInfo(this.scope, FoldPriority.Never, indentation);
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

    private IDisposable CreateFoldableScope(string indentation, FoldPriority foldBehavior)
    {
        this.scope = new ScopeInfo(this.scope, foldBehavior, indentation);
        return new DisposeAction(() =>
        {
            this.scope.Dispose();
            this.scope = this.scope.Parent!;
        });
    }

    private void CreateFoldableScope(string indentation, FoldPriority foldBehavior, Action action)
    {
        using (this.CreateFoldableScope(indentation, foldBehavior)) action();
    }
}
