using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Syntax;

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
        var stateMachine = new LineStateMachine(string.Concat(decorations[0].ScopeInfo.CurrentTotalIndent));
        var currentLineStart = 0;
        List<ScopeInfo> foldedScopes = new();
        for (var x = 0; x < decorations.Length; x++)
        {
            var curr = decorations[x];
            if (curr.DoesReturnLine?.Value ?? false)
            {
                stateMachine = new LineStateMachine(string.Concat(curr.ScopeInfo.CurrentTotalIndent));
                currentLineStart = x;
                foldedScopes.Clear();
            }
            stateMachine.AddToken(curr, settings);
            if (stateMachine.LineWidth > settings.LineWidth)
            {
                var folded = curr.ScopeInfo.Fold();
                if (folded != null)
                {
                    x = currentLineStart - 1;
                    foldedScopes.Add(folded);
                    stateMachine.Reset();
                    continue;
                }
                else if (curr.ScopeInfo.Parent != null)
                {
                    // we can't fold the current scope anymore, so we revert our folding, and we fold the previous scopes on the line.
                    // there can be other strategy taken in the future, parametrable through settings.

                    // first rewind and fold any "as soon as possible" scopes.
                    for (var i = x - 1; i >= currentLineStart; i--)
                    {
                        var scope = decorations[i].ScopeInfo;
                        if (scope.IsMaterialized?.Value ?? false) continue;
                        if (scope.FoldPriority != FoldPriority.AsSoonAsPossible) continue;
                        var prevFolded = scope.Fold();
                        if (prevFolded != null) goto folded;
                    }
                    // there was no high priority scope to fold, we try to get the low priority then.
                    for (var i = x - 1; i >= currentLineStart; i--)
                    {
                        var scope = decorations[i].ScopeInfo;
                        if (scope.IsMaterialized?.Value ?? false) continue;
                        var prevFolded = scope.Fold();
                        if (prevFolded != null) goto folded;
                    }

                    // we couldn't fold any scope, we just give up.
                    continue;

                folded:
                    foreach (var scope in foldedScopes)
                    {
                        scope.IsMaterialized.Value = null;
                    }
                    foldedScopes.Clear();
                    x = currentLineStart - 1;
                }
            }
        }

        var builder = new StringBuilder();
        stateMachine = new LineStateMachine(string.Concat(decorations[0].ScopeInfo.CurrentTotalIndent));
        for (var x = 0; x < decorations.Length; x++)
        {

            var decoration = decorations[x];
            if (decoration.Token.Kind == TokenKind.StringNewline) continue;

            if (x > 0 && (decoration.DoesReturnLine?.Value ?? false))
            {
                builder.Append(stateMachine);
                builder.Append(settings.Newline);
                stateMachine = new LineStateMachine(string.Concat(decoration.ScopeInfo.CurrentTotalIndent));
            }
            if (decoration.Kind.HasFlag(FormattingTokenKind.ExtraNewline) && x > 0)
            {
                builder.Append(settings.Newline);
            }

            stateMachine.AddToken(decoration, settings);
        }
        builder.Append(stateMachine);
        builder.Append(settings.Newline);
        return builder.ToString();
    }

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
        this.scope = new(null, settings, FoldPriority.Never, "");
        this.scope.IsMaterialized.Value = true;
    }

    public override void VisitCompilationUnit(Api.Syntax.CompilationUnitSyntax node)
    {
        this.tokenDecorations = new TokenDecoration[node.Tokens.Count()];
        base.VisitCompilationUnit(node);
    }

    public override void VisitSyntaxToken(Api.Syntax.SyntaxToken node)
    {
        FormattingTokenKind GetFormattingTokenKind(Api.Syntax.SyntaxToken token) => token.Kind switch
        {
            TokenKind.KeywordAnd => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordElse => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordFor => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordGoto => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordImport => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordIn => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordInternal => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordModule => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordOr => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordReturn => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordPublic => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordVar => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordVal => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordIf => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,
            TokenKind.KeywordWhile => FormattingTokenKind.PadLeft | FormattingTokenKind.ForceRightPad,

            TokenKind.KeywordTrue => FormattingTokenKind.PadAround,
            TokenKind.KeywordFalse => FormattingTokenKind.PadAround,
            TokenKind.KeywordMod => FormattingTokenKind.PadAround,
            TokenKind.KeywordRem => FormattingTokenKind.PadAround,

            TokenKind.KeywordFunc => this.currentIdx == 0 ? FormattingTokenKind.PadAround : FormattingTokenKind.ExtraNewline,


            TokenKind.Semicolon => FormattingTokenKind.BehaveAsWhiteSpaceForPreviousToken,
            TokenKind.CurlyOpen => FormattingTokenKind.PadLeft | FormattingTokenKind.BehaveAsWhiteSpaceForNextToken,
            TokenKind.ParenOpen => FormattingTokenKind.Whitespace,
            TokenKind.ParenClose => FormattingTokenKind.BehaveAsWhiteSpaceForPreviousToken,
            TokenKind.InterpolationStart => FormattingTokenKind.Whitespace,
            TokenKind.Dot => FormattingTokenKind.Whitespace,

            TokenKind.Assign => FormattingTokenKind.PadAround,
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
            TokenKind.Equal => FormattingTokenKind.PadLeft,
            TokenKind.LiteralFloat => FormattingTokenKind.PadLeft,
            TokenKind.LiteralInteger => FormattingTokenKind.PadLeft,

            TokenKind.Identifier => FormattingTokenKind.PadLeft,

            _ => FormattingTokenKind.NoFormatting
        };

        this.CurrentToken.ScopeInfo = this.scope;
        this.CurrentToken.Kind |= GetFormattingTokenKind(node);
        this.CurrentToken.Token = node;
        var trivia = this.CurrentToken.Token.TrailingTrivia;
        if (trivia.Count > 0)
        {
            var comment = trivia
                .Where(x => x.Kind == TriviaKind.LineComment || x.Kind == TriviaKind.DocumentationComment)
                .Select(x => x.Text)
                .SingleOrDefault();
            if (comment != null)
            {
                this.CurrentToken.TokenOverride = this.CurrentToken.Token.Text + " " + comment;
                this.tokenDecorations[this.currentIdx + 1].DoesReturnLine = true;
            }
        }
        var leadingComments = this.CurrentToken.Token.LeadingTrivia
            .Where(x => x.Kind == TriviaKind.LineComment || x.Kind == TriviaKind.DocumentationComment)
            .Select(x => x.Text)
            .ToArray();
        this.CurrentToken.LeadingComments = leadingComments;
        if (leadingComments.Length > 0)
        {
            this.CurrentToken.DoesReturnLine = true;
        }

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
        this.CurrentToken.DoesReturnLine = this.scope.IsMaterialized;
        base.VisitParameter(node);
    }

    public override void VisitDeclaration(Api.Syntax.DeclarationSyntax node)
    {
        if (node.Parent is not Api.Syntax.DeclarationStatementSyntax)
        {
            this.CurrentToken.DoesReturnLine = !this.firstDeclaration;
            this.firstDeclaration = false;
        }
        base.VisitDeclaration(node);
    }

    public override void VisitStringExpression(Api.Syntax.StringExpressionSyntax node)
    {
        if (node.OpenQuotes.Kind != TokenKind.MultiLineStringStart)
        {
            node.OpenQuotes.Accept(this);
            foreach (var item in node.Parts.Tokens)
            {
                this.CurrentToken.DoesReturnLine = false;
                item.Accept(this);
            }
            this.CurrentToken.DoesReturnLine = false;
            node.CloseQuotes.Accept(this);
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
            var startIdx = this.currentIdx;
            var tokenCount = curr.Tokens.Count();
            foreach (var token in curr.Tokens)
            {
                var newLines = token.TrailingTrivia.Where(t => t.Kind == TriviaKind.Newline).ToArray();
                if (newLines.Length > 0)
                {
                    this.tokenDecorations[this.currentIdx + 1].DoesReturnLine = true;

                    this.CurrentToken.TokenOverride = string.Concat(Enumerable.Repeat(this.Settings.Newline, newLines.Length - 1).Prepend(token.Text));
                }
                token.Accept(this);
            }

            for (var j = 0; j < tokenCount; j++)
            {
                ref var decoration = ref this.tokenDecorations[startIdx + j];
                decoration.DoesReturnLine ??= false;
            }
        }
        MultiIndent(newLineCount);
        this.tokenDecorations[this.currentIdx].DoesReturnLine = true;
        node.CloseQuotes.Accept(this);


        void MultiIndent(int newLineCount)
        {
            if (newLineCount > 0)
            {
                ref var currentToken = ref this.tokenDecorations[this.currentIdx];
                currentToken.DoesReturnLine = true;
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
        DisposeAction? closeScope = null;
        var kind = node.Operator.Kind;
        if (!(this.scope.Data?.Equals(kind) ?? false))
        {
            closeScope = this.CreateFoldableScope("", FoldPriority.AsLateAsPossible);
            this.scope.Data = kind;
        }
        node.Left.Accept(this);

        if (this.CurrentToken.DoesReturnLine is null)
        {
            this.CurrentToken.DoesReturnLine = this.scope.IsMaterialized;
        }
        node.Operator.Accept(this);
        node.Right.Accept(this);
        closeScope?.Dispose();
    }

    public override void VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        node.OpenBrace.Accept(this);
        this.CreateFoldedScope(this.Settings.Indentation, () => node.Statements.Accept(this));
        this.tokenDecorations[this.currentIdx].DoesReturnLine = true;
        node.CloseBrace.Accept(this);
    }

    public override void VisitInlineFunctionBody(Api.Syntax.InlineFunctionBodySyntax node)
    {
        var curr = this.currentIdx;
        node.Assign.Accept(this);

        using var _ = this.CreateFoldableScope(curr, FoldPriority.AsSoonAsPossible);
        node.Value.Accept(this);
        node.Semicolon.Accept(this);
    }

    public override void VisitFunctionDeclaration(Api.Syntax.FunctionDeclarationSyntax node)
    {
        this.VisitDeclaration(node);
        DisposeAction disposable;
        if (node.VisibilityModifier != null)
        {
            node.VisibilityModifier?.Accept(this);
            disposable = this.CreateFoldedScope(this.Settings.Indentation);
            node.FunctionKeyword.Accept(this);
        }
        else
        {
            node.FunctionKeyword.Accept(this);
            disposable = this.CreateFoldedScope(this.Settings.Indentation);
        }
        node.Name.Accept(this);
        if (node.Generics is not null)
        {
            this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsLateAsPossible, () => node.Generics?.Accept(this));
        }
        node.OpenParen.Accept(this);
        disposable.Dispose();
        node.ParameterList.Accept(this);
        node.CloseParen.Accept(this);
        node.ReturnType?.Accept(this);
        node.Body.Accept(this);
    }

    public override void VisitStatement(Api.Syntax.StatementSyntax node)
    {
        if (node is Api.Syntax.DeclarationStatementSyntax { Declaration: Api.Syntax.LabelDeclarationSyntax })
        {
            this.CurrentToken.DoesReturnLine = true;
            this.CurrentToken.Kind = FormattingTokenKind.RemoveOneIndentation;
        }
        else
        {
            var shouldBeMultiLine = node.Parent is Api.Syntax.BlockExpressionSyntax || node.Parent is Api.Syntax.BlockFunctionBodySyntax;
            this.CurrentToken.DoesReturnLine = shouldBeMultiLine;
        }
        base.VisitStatement(node);
    }

    public override void VisitWhileExpression(Api.Syntax.WhileExpressionSyntax node)
    {
        node.WhileKeyword.Accept(this);
        node.OpenParen.Accept(this);
        this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Condition.Accept(this));
        node.CloseParen.Accept(this);
        this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node) =>
        this.CreateFoldableScope("", FoldPriority.AsSoonAsPossible, () =>
        {
            node.IfKeyword.Accept(this);
            node.OpenParen.Accept(this);
            this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Condition.Accept(this));
            node.CloseParen.Accept(this);

            this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));

            node.Else?.Accept(this);
        });

    public override void VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        if (node.IsElseIf || node.Parent!.Parent is Api.Syntax.ExpressionStatementSyntax)
        {
            this.CurrentToken.DoesReturnLine = true;
        }
        else
        {
            this.CurrentToken.DoesReturnLine = this.scope.IsMaterialized;
        }
        node.ElseKeyword.Accept(this);
        this.CreateFoldableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Expression.Accept(this));
    }

    public override void VisitBlockExpression(Api.Syntax.BlockExpressionSyntax node)
    {
        // this means we are in a if/while/else, and *can* create an indentation with a regular expression folding:
        // if (blabla) an expression;
        // it can fold:
        // if (blabla)
        //     an expression;
        // but since we are in a block we create our own scope and the if/while/else will never create it's own scope.
        this.scope.IsMaterialized.Value ??= false;

        node.OpenBrace.Accept(this);

        this.CreateFoldedScope(this.Settings.Indentation, () =>
        {
            node.Statements.Accept(this);
            if (node.Value != null)
            {
                this.CurrentToken.DoesReturnLine = true;
                node.Value.Accept(this);
            }
        });
        node.CloseBrace.Accept(this);
        this.tokenDecorations[this.currentIdx - 1].DoesReturnLine = true;
    }

    private DisposeAction CreateFoldedScope(string indentation)
    {
        this.scope = new ScopeInfo(this.scope, this.Settings, FoldPriority.Never, indentation);
        this.scope.IsMaterialized.Value = true;
        return new DisposeAction(() => this.scope = this.scope.Parent!);
    }

    private void CreateFoldedScope(string indentation, Action action)
    {
        using (this.CreateFoldedScope(indentation)) action();
    }

    private DisposeAction CreateFoldableScope(string indentation, FoldPriority foldBehavior)
    {
        this.scope = new ScopeInfo(this.scope, this.Settings, foldBehavior, indentation);
        return new DisposeAction(() => this.scope = this.scope.Parent!);
    }

    private DisposeAction CreateFoldableScope(int indexOfLevelingToken, FoldPriority foldBehavior)
    {
        this.scope = new ScopeInfo(this.scope, this.Settings, foldBehavior, (this.tokenDecorations, indexOfLevelingToken));
        return new DisposeAction(() => this.scope = this.scope.Parent!);
    }

    private void CreateFoldableScope(string indentation, FoldPriority foldBehavior, Action action)
    {
        using (this.CreateFoldableScope(indentation, foldBehavior)) action();
    }
}
