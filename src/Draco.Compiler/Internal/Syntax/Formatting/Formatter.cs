using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class Formatter : Api.Syntax.SyntaxVisitor
{
    private TokenMetadata[] tokensMetadata = [];
    private int currentIdx;
    private Scope scope;
    private ref TokenMetadata PreviousToken => ref this.tokensMetadata[this.currentIdx - 1];
    private ref TokenMetadata CurrentToken => ref this.tokensMetadata[this.currentIdx];
    private ref TokenMetadata NextToken => ref this.tokensMetadata[this.currentIdx + 1];

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

        var metadatas = formatter.tokensMetadata;
        var tokens = tree.Root.Tokens.ToArray();
        FoldTooLongLine(formatter, settings, tokens);

        var builder = new StringBuilder();
        var stateMachine = new LineStateMachine(string.Concat(metadatas[0].ScopeInfo.CurrentTotalIndent));

        stateMachine.AddToken(metadatas[0], settings, tokens[0].Kind == TokenKind.EndOfInput);

        for (var x = 1; x < metadatas.Length; x++)
        {
            var metadata = metadatas[x];
            // we ignore multiline string newline tokens because we handle them in the string expression visitor.

            if (metadata.DoesReturnLine?.Value ?? false)
            {
                builder.Append(stateMachine);
                builder.Append(settings.Newline);
                stateMachine = new LineStateMachine(string.Concat(metadata.ScopeInfo.CurrentTotalIndent));
            }
            if (metadata.Kind.HasFlag(WhitespaceBehavior.ExtraNewline))
            {
                builder.Append(settings.Newline);
            }

            stateMachine.AddToken(metadata, settings, tokens[x].Kind == TokenKind.EndOfInput);
        }
        builder.Append(stateMachine);
        builder.Append(settings.Newline);
        return builder.ToString();
    }

    private static void FoldTooLongLine(Formatter formatter, FormatterSettings settings, IReadOnlyList<Api.Syntax.SyntaxToken> tokens)
    {
        var metadatas = formatter.tokensMetadata;
        var stateMachine = new LineStateMachine(string.Concat(metadatas[0].ScopeInfo.CurrentTotalIndent));
        var currentLineStart = 0;
        List<Scope> foldedScopes = [];
        for (var x = 0; x < metadatas.Length; x++)
        {
            var curr = metadatas[x];
            if (curr.DoesReturnLine?.Value ?? false) // if it's a new line
            {
                // we recreate a state machine for the new line.
                stateMachine = new LineStateMachine(string.Concat(curr.ScopeInfo.CurrentTotalIndent));
                currentLineStart = x;
                foldedScopes.Clear();
            }

            stateMachine.AddToken(curr, settings, tokens[x].Kind == TokenKind.EndOfInput);

            if (stateMachine.LineWidth <= settings.LineWidth) continue;

            // the line is too long...

            var folded = curr.ScopeInfo.Fold(); // folding can fail if there is nothing else to fold.
            if (folded != null)
            {
                x = currentLineStart - 1;
                foldedScopes.Add(folded);
                stateMachine.Reset();
                continue;
            }

            // we can't fold the current scope anymore, so we revert our folding, and we fold the previous scopes on the line.
            // there can be other strategy taken in the future, parametrable through settings.

            // first rewind and fold any "as soon as possible" scopes.
            for (var i = x - 1; i >= currentLineStart; i--)
            {
                var scope = metadatas[i].ScopeInfo;
                if (scope.IsMaterialized?.Value ?? false) continue;
                if (scope.FoldPriority != FoldPriority.AsSoonAsPossible) continue;
                var prevFolded = scope.Fold();
                if (prevFolded != null)
                {
                    ResetBacktracking();
                    continue;
                }
            }
            // there was no high priority scope to fold, we try to get the low priority then.
            for (var i = x - 1; i >= currentLineStart; i--)
            {
                var scope = metadatas[i].ScopeInfo;
                if (scope.IsMaterialized?.Value ?? false) continue;
                var prevFolded = scope.Fold();
                if (prevFolded != null)
                {
                    ResetBacktracking();
                    continue;
                }
            }

            // we couldn't fold any scope, we just give up.

            void ResetBacktracking()
            {
                foreach (var scope in foldedScopes)
                {
                    scope.IsMaterialized.Value = null;
                }
                foldedScopes.Clear();
                x = currentLineStart - 1;
            }
        }
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
        this.tokensMetadata = new TokenMetadata[node.Tokens.Count()];
        base.VisitCompilationUnit(node);
    }

    public override void VisitSyntaxToken(Api.Syntax.SyntaxToken node)
    {
        static WhitespaceBehavior GetFormattingTokenKind(Api.Syntax.SyntaxToken token) => token.Kind switch
        {
            TokenKind.KeywordAnd => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordElse => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordFor => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordGoto => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordImport => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordIn => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordInternal => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordModule => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordOr => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordReturn => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordPublic => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordVar => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordVal => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordIf => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,
            TokenKind.KeywordWhile => WhitespaceBehavior.PadLeft | WhitespaceBehavior.ForceRightPad,

            TokenKind.KeywordTrue => WhitespaceBehavior.PadAround,
            TokenKind.KeywordFalse => WhitespaceBehavior.PadAround,
            TokenKind.KeywordMod => WhitespaceBehavior.PadAround,
            TokenKind.KeywordRem => WhitespaceBehavior.PadAround,

            TokenKind.KeywordFunc => WhitespaceBehavior.ExtraNewline,


            TokenKind.Semicolon => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
            TokenKind.CurlyOpen => WhitespaceBehavior.PadLeft | WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken,
            TokenKind.ParenOpen => WhitespaceBehavior.Whitespace,
            TokenKind.ParenClose => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
            TokenKind.InterpolationStart => WhitespaceBehavior.Whitespace,
            TokenKind.Dot => WhitespaceBehavior.Whitespace,
            TokenKind.Colon => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,

            TokenKind.Assign => WhitespaceBehavior.PadAround,
            TokenKind.LineStringStart => WhitespaceBehavior.PadLeft,
            TokenKind.MultiLineStringStart => WhitespaceBehavior.PadLeft,
            TokenKind.Plus => WhitespaceBehavior.PadLeft,
            TokenKind.Minus => WhitespaceBehavior.PadLeft,
            TokenKind.Star => WhitespaceBehavior.PadLeft,
            TokenKind.Slash => WhitespaceBehavior.PadLeft,
            TokenKind.PlusAssign => WhitespaceBehavior.PadLeft,
            TokenKind.MinusAssign => WhitespaceBehavior.PadLeft,
            TokenKind.StarAssign => WhitespaceBehavior.PadLeft,
            TokenKind.SlashAssign => WhitespaceBehavior.PadLeft,
            TokenKind.GreaterEqual => WhitespaceBehavior.PadLeft,
            TokenKind.GreaterThan => WhitespaceBehavior.PadLeft,
            TokenKind.LessEqual => WhitespaceBehavior.PadLeft,
            TokenKind.LessThan => WhitespaceBehavior.PadLeft,
            TokenKind.Equal => WhitespaceBehavior.PadLeft,
            TokenKind.LiteralFloat => WhitespaceBehavior.PadLeft,
            TokenKind.LiteralInteger => WhitespaceBehavior.PadLeft,

            TokenKind.Identifier => WhitespaceBehavior.PadLeft,

            _ => WhitespaceBehavior.NoFormatting
        };

        this.CurrentToken.ScopeInfo = this.scope;
        this.CurrentToken.Kind |= GetFormattingTokenKind(node);
        //this.CurrentToken.Token = node;
        this.CurrentToken.Text = node.Text;
        this.HandleTokenComments(node);

        base.VisitSyntaxToken(node);
        this.currentIdx++;
    }

    private void HandleTokenComments(Api.Syntax.SyntaxToken node)
    {
        var trivia = node.TrailingTrivia;
        if (trivia.Count > 0)
        {
            var comment = trivia
                .Where(x => x.Kind == TriviaKind.LineComment || x.Kind == TriviaKind.DocumentationComment)
                .Select(x => x.Text)
                .SingleOrDefault();
            if (comment != null)
            {
                this.CurrentToken.TokenOverride = node.Text + " " + comment;
                this.NextToken.DoesReturnLine = true;
            }
        }
        var leadingComments = node.LeadingTrivia
            .Where(x => x.Kind == TriviaKind.LineComment || x.Kind == TriviaKind.DocumentationComment)
            .Select(x => x.Text)
            .ToArray();
        this.CurrentToken.LeadingComments = leadingComments;
        if (leadingComments.Length > 0)
        {
            this.CurrentToken.DoesReturnLine = true;
        }
    }

    public override void VisitSeparatedSyntaxList<TNode>(Api.Syntax.SeparatedSyntaxList<TNode> node)
    {
        if (node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ParameterSyntax>
            || node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ExpressionSyntax>)
        {
            this.CreateMaterializableScope(this.Settings.Indentation,
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
            this.CurrentToken.DoesReturnLine = this.currentIdx > 0; // don't create empty line on first line in file.
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
        using var _ = this.CreateScope(this.Settings.Indentation);
        var blockCurrentIndentCount = SyntaxFacts.ComputeCutoff(node).Length;

        var i = 0;
        var shouldIndent = true;
        for (; i < node.Parts.Count; i++)
        {
            var curr = node.Parts[i];

            if (curr.IsNewLine)
            {
                shouldIndent = true;
                curr.Accept(this);
                continue;
            }

            if (shouldIndent)
            {
                shouldIndent = false;

                var tokenText = curr.Tokens.First().ValueText!;
                if (!tokenText.Take(blockCurrentIndentCount).All(char.IsWhiteSpace)) throw new InvalidOperationException();
                this.CurrentToken.TokenOverride = tokenText[blockCurrentIndentCount..];
                this.CurrentToken.DoesReturnLine = true;

                if (i > 0 && node.Parts[i - 1].IsNewLine)
                {
                    this.PreviousToken.TokenOverride = ""; // PreviousToken is a newline, CurrentToken.DoesReturnLine will produce the newline.
                }
            }

            var startIdx = this.currentIdx; // capture position before visiting the tokens of this parts (this will move forward the position)

            // for parts that contains expressions and have return lines.
            foreach (var token in curr.Tokens)
            {
                var newLines = token.TrailingTrivia.Where(t => t.Kind == TriviaKind.Newline).ToArray();
                if (newLines.Length > 0)
                {
                    this.NextToken.DoesReturnLine = true;
                    this.CurrentToken.TokenOverride = string.Concat(Enumerable.Repeat(this.Settings.Newline, newLines.Length - 1).Prepend(token.Text));
                }
                token.Accept(this);
            }

            // default all tokens to never return.
            var tokenCount = curr.Tokens.Count();

            for (var j = 0; j < tokenCount; j++)
            {
                this.tokensMetadata[startIdx + j].DoesReturnLine ??= false;
            }
        }
        this.CurrentToken.DoesReturnLine = true;
        node.CloseQuotes.Accept(this);
    }

    public override void VisitBinaryExpression(Api.Syntax.BinaryExpressionSyntax node)
    {
        DisposeAction? closeScope = null;
        var kind = node.Operator.Kind;
        if (!(this.scope.Data?.Equals(kind) ?? false))
        {
            closeScope = this.CreateMaterializableScope("", FoldPriority.AsLateAsPossible);
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
        this.CreateScope(this.Settings.Indentation, () => node.Statements.Accept(this));
        this.CurrentToken.DoesReturnLine = true;
        node.CloseBrace.Accept(this);
    }

    public override void VisitInlineFunctionBody(Api.Syntax.InlineFunctionBodySyntax node)
    {
        var curr = this.currentIdx;
        node.Assign.Accept(this);

        using var _ = this.CreateMaterializableScope(curr, FoldPriority.AsSoonAsPossible);
        node.Value.Accept(this);
        node.Semicolon.Accept(this);
    }

    public override void VisitFunctionDeclaration(Api.Syntax.FunctionDeclarationSyntax node)
    {
        this.VisitDeclaration(node);
        var disposable = this.OpenScopeAtFirstToken(node.VisibilityModifier, node.FunctionKeyword);
        node.Name.Accept(this);
        if (node.Generics is not null)
        {
            this.CreateMaterializableScope(this.Settings.Indentation, FoldPriority.AsLateAsPossible, () => node.Generics?.Accept(this));
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
            this.CurrentToken.Kind = WhitespaceBehavior.RemoveOneIndentation;
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
        this.CreateMaterializableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Condition.Accept(this));
        node.CloseParen.Accept(this);
        this.CreateMaterializableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node) =>
        this.CreateMaterializableScope("", FoldPriority.AsSoonAsPossible, () =>
        {
            node.IfKeyword.Accept(this);
            DisposeAction? disposable = null;
            node.OpenParen.Accept(this);
            if (this.PreviousToken.DoesReturnLine?.Value ?? false)
            {
                // there is no reason for an OpenParen to return line except if there is a comment.
                disposable = this.CreateScope(this.Settings.Indentation);
                this.PreviousToken.ScopeInfo = this.scope; // it's easier to change our mind that compute ahead of time.
            }
            this.CreateMaterializableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () =>
            {
                var firstTokenIdx = this.currentIdx;
                node.Condition.Accept(this);
                var firstToken = this.tokensMetadata[firstTokenIdx];
                if (firstToken.DoesReturnLine?.Value ?? false)
                {
                    firstToken.ScopeInfo.IsMaterialized.Value = true;
                }
            });
            node.CloseParen.Accept(this);
            disposable?.Dispose();

            this.CreateMaterializableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));

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
        this.CreateMaterializableScope(this.Settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Expression.Accept(this));
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

        this.CreateScope(this.Settings.Indentation, () =>
        {
            node.Statements.Accept(this);
            if (node.Value != null)
            {
                this.CurrentToken.DoesReturnLine = true;
                node.Value.Accept(this);
            }
        });
        node.CloseBrace.Accept(this);
        this.PreviousToken.DoesReturnLine = true;
    }

    public override void VisitVariableDeclaration(Api.Syntax.VariableDeclarationSyntax node)
    {
        var disposable = this.OpenScopeAtFirstToken(node.VisibilityModifier, node.Keyword);
        node.Name.Accept(this);
        disposable.Dispose();
        node.Type?.Accept(this);
        node.Value?.Accept(this);
        node.Semicolon.Accept(this);
    }

    private DisposeAction OpenScopeAtFirstToken(Api.Syntax.SyntaxToken? optionalToken, Api.Syntax.SyntaxToken token)
    {
        var disposable = null as DisposeAction;
        if (optionalToken != null)
        {
            optionalToken.Accept(this);
            disposable = this.CreateScope(this.Settings.Indentation);
        }
        token.Accept(this);
        return disposable ?? this.CreateScope(this.Settings.Indentation);
    }

    private DisposeAction CreateScope(string indentation)
    {
        this.scope = new Scope(this.scope, this.Settings, FoldPriority.Never, indentation);
        this.scope.IsMaterialized.Value = true;
        return new DisposeAction(() => this.scope = this.scope.Parent!);
    }

    private void CreateScope(string indentation, Action action)
    {
        using (this.CreateScope(indentation)) action();
    }

    private DisposeAction CreateMaterializableScope(string indentation, FoldPriority foldBehavior)
    {
        this.scope = new Scope(this.scope, this.Settings, foldBehavior, indentation);
        return new DisposeAction(() => this.scope = this.scope.Parent!);
    }

    private DisposeAction CreateMaterializableScope(int indexOfLevelingToken, FoldPriority foldBehavior)
    {
        this.scope = new Scope(this.scope, this.Settings, foldBehavior, (this.tokensMetadata, indexOfLevelingToken));
        return new DisposeAction(() => this.scope = this.scope.Parent!);
    }

    private void CreateMaterializableScope(string indentation, FoldPriority foldBehavior, Action action)
    {
        using (this.CreateMaterializableScope(indentation, foldBehavior)) action();
    }
}
