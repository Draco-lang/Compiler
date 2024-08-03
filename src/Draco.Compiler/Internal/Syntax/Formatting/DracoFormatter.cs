using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class DracoFormatter : Api.Syntax.SyntaxVisitor
{
    private readonly FormatterSettings settings;
    private FormatterEngine formatter = null!;
    private DracoFormatter(FormatterSettings settings)
    {
        this.settings = settings;
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

        var formatter = new DracoFormatter(settings);
        tree.Root.Accept(formatter);
        return formatter.formatter.Format();
    }

    public override void VisitCompilationUnit(Api.Syntax.CompilationUnitSyntax node)
    {
        this.formatter = new FormatterEngine(node.Tokens.Count(), this.settings);
        base.VisitCompilationUnit(node);
    }

    private static WhitespaceBehavior GetFormattingTokenKind(Api.Syntax.SyntaxToken token) => token.Kind switch
    {
        TokenKind.KeywordAnd => WhitespaceBehavior.PadAround,
        TokenKind.KeywordElse => WhitespaceBehavior.PadAround,
        TokenKind.KeywordFor => WhitespaceBehavior.PadAround,
        TokenKind.KeywordGoto => WhitespaceBehavior.PadAround,
        TokenKind.KeywordImport => WhitespaceBehavior.PadAround,
        TokenKind.KeywordIn => WhitespaceBehavior.PadAround,
        TokenKind.KeywordInternal => WhitespaceBehavior.PadAround,
        TokenKind.KeywordModule => WhitespaceBehavior.PadAround,
        TokenKind.KeywordOr => WhitespaceBehavior.PadAround,
        TokenKind.KeywordReturn => WhitespaceBehavior.PadAround,
        TokenKind.KeywordPublic => WhitespaceBehavior.PadAround,
        TokenKind.KeywordVar => WhitespaceBehavior.PadAround,
        TokenKind.KeywordVal => WhitespaceBehavior.PadAround,
        TokenKind.KeywordIf => WhitespaceBehavior.PadAround,
        TokenKind.KeywordWhile => WhitespaceBehavior.PadAround,

        TokenKind.KeywordTrue => WhitespaceBehavior.PadAround,
        TokenKind.KeywordFalse => WhitespaceBehavior.PadAround,
        TokenKind.KeywordMod => WhitespaceBehavior.PadAround,
        TokenKind.KeywordRem => WhitespaceBehavior.PadAround,

        TokenKind.KeywordFunc => WhitespaceBehavior.PadAround,

        TokenKind.Semicolon => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
        TokenKind.CurlyOpen => WhitespaceBehavior.SpaceBefore | WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken,
        TokenKind.ParenOpen => WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken,
        TokenKind.ParenClose => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
        TokenKind.InterpolationStart => WhitespaceBehavior.Whitespace,
        TokenKind.Dot => WhitespaceBehavior.Whitespace,
        TokenKind.Colon => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,

        TokenKind.Assign => WhitespaceBehavior.PadAround,
        TokenKind.LineStringStart => WhitespaceBehavior.SpaceBefore,
        TokenKind.MultiLineStringStart => WhitespaceBehavior.SpaceBefore,
        TokenKind.Plus => WhitespaceBehavior.SpaceBefore,
        TokenKind.Minus => WhitespaceBehavior.SpaceBefore,
        TokenKind.Star => WhitespaceBehavior.SpaceBefore,
        TokenKind.Slash => WhitespaceBehavior.SpaceBefore,
        TokenKind.PlusAssign => WhitespaceBehavior.SpaceBefore,
        TokenKind.MinusAssign => WhitespaceBehavior.SpaceBefore,
        TokenKind.StarAssign => WhitespaceBehavior.SpaceBefore,
        TokenKind.SlashAssign => WhitespaceBehavior.SpaceBefore,
        TokenKind.GreaterEqual => WhitespaceBehavior.SpaceBefore,
        TokenKind.GreaterThan => WhitespaceBehavior.SpaceBefore,
        TokenKind.LessEqual => WhitespaceBehavior.SpaceBefore,
        TokenKind.LessThan => WhitespaceBehavior.SpaceBefore,
        TokenKind.Equal => WhitespaceBehavior.SpaceBefore,
        TokenKind.LiteralFloat => WhitespaceBehavior.SpaceBefore,
        TokenKind.LiteralInteger => WhitespaceBehavior.SpaceBefore,

        TokenKind.Identifier => WhitespaceBehavior.SpaceBefore,

        _ => WhitespaceBehavior.NoFormatting
    };

    public override void VisitSyntaxToken(Api.Syntax.SyntaxToken node)
    {
        this.HandleTokenComments(node);
        var formattingKind = GetFormattingTokenKind(node);

        var firstToken = this.formatter.CurrentIdx == 0;
        var insertSpace = formattingKind.HasFlag(WhitespaceBehavior.SpaceBefore);
        var doesReturnLine = this.formatter.CurrentToken.DoesReturnLine;
        var insertNewline = doesReturnLine is not null && doesReturnLine.IsCompleted && doesReturnLine.Value;
        var whitespaceNode = node.Kind == TokenKind.StringNewline || node.Kind == TokenKind.EndOfInput;
        if (!insertSpace
            && !firstToken
            && !insertNewline
            && !whitespaceNode)
        {
            if (SyntaxFacts.WillTokenMerges(this.formatter.PreviousToken.Text, node.Text))
            {
                this.formatter.CurrentToken.Kind = WhitespaceBehavior.SpaceBefore;
            }
        }

        this.formatter.SetCurrentTokenInfo(formattingKind, node.Text);

        base.VisitSyntaxToken(node);
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
                this.formatter.CurrentToken.Text = node.Text + " " + comment;
                this.formatter.NextToken.DoesReturnLine = true;
            }
        }
        var leadingComments = node.LeadingTrivia
            .Where(x => x.Kind == TriviaKind.LineComment || x.Kind == TriviaKind.DocumentationComment)
            .Select(x => x.Text)
            .ToArray();
        this.formatter.CurrentToken.LeadingTrivia ??= [];
        this.formatter.CurrentToken.LeadingTrivia.AddRange(leadingComments);
        if (this.formatter.CurrentToken.LeadingTrivia.Count > 0)
        {
            this.formatter.CurrentToken.DoesReturnLine = true;
        }
    }

    public override void VisitSeparatedSyntaxList<TNode>(Api.Syntax.SeparatedSyntaxList<TNode> node)
    {
        if (node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ParameterSyntax>
            or Api.Syntax.SeparatedSyntaxList<Api.Syntax.ExpressionSyntax>)
        {
            using var _ = this.formatter.CreateFoldableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible);
            base.VisitSeparatedSyntaxList(node);
        }
        else
        {
            base.VisitSeparatedSyntaxList(node);
        }
    }

    public override void VisitParameter(Api.Syntax.ParameterSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = this.formatter.Scope.Folded;
        base.VisitParameter(node);
    }

    public override void VisitDeclaration(Api.Syntax.DeclarationSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = true;
        base.VisitDeclaration(node);
    }

    public override void VisitStringExpression(Api.Syntax.StringExpressionSyntax node)
    {
        if (node.OpenQuotes.Kind != TokenKind.MultiLineStringStart)
        {
            this.HandleSingleLineString(node);
            return;
        }

        this.HandleMultiLineString(node);
    }

    private void HandleMultiLineString(Api.Syntax.StringExpressionSyntax node)
    {
        node.OpenQuotes.Accept(this);
        using var _ = this.formatter.CreateScope(this.settings.Indentation);
        var blockCurrentIndentCount = SyntaxFacts.ComputeCutoff(node).Length;

        for (var i = 0; i < node.Parts.Count; i++)
        {
            var curr = node.Parts[i];

            if (curr.IsNewLine || i == 0)
            {
                curr.Accept(this);
                if (i == node.Parts.Count - 1) break;
                HandleNewLine(node, blockCurrentIndentCount, i, curr);
                continue;
            }

            var startIdx = this.formatter.CurrentIdx; // capture position before visiting the tokens of this parts (this will move forward the position)

            // for parts that contains expressions and have return lines.
            foreach (var token in curr.Tokens)
            {
                var newLineCount = token.TrailingTrivia.Where(t => t.Kind == TriviaKind.Newline).Count();
                if (newLineCount > 0)
                {
                    this.formatter.NextToken.DoesReturnLine = true;
                    this.formatter.CurrentToken.Text = token.Text + string.Concat(Enumerable.Repeat(this.settings.Newline, newLineCount - 1));
                }
                token.Accept(this);
            }

            // sets all unset tokens to not return a line.
            var j = 0;
            foreach (var token in curr.Tokens)
            {
                this.formatter.TokensMetadata[startIdx + j].DoesReturnLine ??= false;
                j++;
            }
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        node.CloseQuotes.Accept(this);

        void HandleNewLine(Api.Syntax.StringExpressionSyntax node, int blockCurrentIndentCount, int i, Api.Syntax.StringPartSyntax curr)
        {
            var next = node.Parts[i + 1];
            var tokenText = next.Tokens.First().ValueText!;
            this.formatter.CurrentToken.Text = tokenText[blockCurrentIndentCount..];
            this.formatter.CurrentToken.DoesReturnLine = true;

            if (i > 0 && node.Parts[i - 1].IsNewLine)
            {
                this.formatter.PreviousToken.Text = ""; // PreviousToken is a newline, CurrentToken.DoesReturnLine will produce the newline.
            }
        }
    }

    private void HandleSingleLineString(Api.Syntax.StringExpressionSyntax node)
    {
        // this is a single line string
        node.OpenQuotes.Accept(this);
        // we just sets all tokens in this string to not return a line.
        foreach (var item in node.Parts.Tokens)
        {
            this.formatter.CurrentToken.DoesReturnLine = false;
            item.Accept(this);
        }
        // including the close quote.
        this.formatter.CurrentToken.DoesReturnLine = false;
        node.CloseQuotes.Accept(this);
    }

    public override void VisitBinaryExpression(Api.Syntax.BinaryExpressionSyntax node)
    {
        var closeScope = null as IDisposable;
        var kind = node.Operator.Kind;
        if (node.Parent is not Api.Syntax.BinaryExpressionSyntax { Operator.Kind: var previousKind } || previousKind != kind)
        {
            closeScope = this.formatter.CreateFoldableScope("", FoldPriority.AsLateAsPossible);
        }

        node.Left.Accept(this);

        this.formatter.CurrentToken.DoesReturnLine ??= this.formatter.Scope.Folded;
        node.Operator.Accept(this);
        node.Right.Accept(this);
        closeScope?.Dispose();
    }

    public override void VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        node.OpenBrace.Accept(this);
        this.formatter.CreateScope(this.settings.Indentation, () => node.Statements.Accept(this));
        this.formatter.CurrentToken.DoesReturnLine = true;
        node.CloseBrace.Accept(this);
    }

    public override void VisitInlineFunctionBody(Api.Syntax.InlineFunctionBodySyntax node)
    {
        var curr = this.formatter.CurrentIdx;
        node.Assign.Accept(this);

        using var _ = this.formatter.CreateFoldableScope(curr, FoldPriority.AsSoonAsPossible);
        node.Value.Accept(this);
        node.Semicolon.Accept(this);
    }

    public override void VisitFunctionDeclaration(Api.Syntax.FunctionDeclarationSyntax node)
    {
        this.VisitDeclaration(node);
        if (GetPrevious(node) != null)
        {
            this.formatter.CurrentToken.LeadingTrivia = [""];
        }

        var disposable = this.formatter.CreateScopeAfterNextToken(this.settings.Indentation);
        node.VisibilityModifier?.Accept(this);
        node.FunctionKeyword.Accept(this);
        node.Name.Accept(this);
        if (node.Generics is not null)
        {
            using var _ = this.formatter.CreateFoldableScope(this.settings.Indentation, FoldPriority.AsLateAsPossible);
            node.Generics?.Accept(this);
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
            this.formatter.CurrentToken.DoesReturnLine = true;
            this.formatter.CurrentToken.Kind = WhitespaceBehavior.RemoveOneIndentation;
        }
        else
        {
            var shouldBeMultiLine = node.Parent is Api.Syntax.BlockExpressionSyntax || node.Parent is Api.Syntax.BlockFunctionBodySyntax;
            this.formatter.CurrentToken.DoesReturnLine = new Future<bool>(shouldBeMultiLine);
        }
        base.VisitStatement(node);
    }

    public override void VisitWhileExpression(Api.Syntax.WhileExpressionSyntax node)
    {
        node.WhileKeyword.Accept(this);
        node.OpenParen.Accept(this);
        this.formatter.CreateFoldableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Condition.Accept(this));
        node.CloseParen.Accept(this);
        this.formatter.CreateFoldableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node)
    {
        using var _ = this.formatter.CreateFoldableScope("", FoldPriority.AsSoonAsPossible);

        node.IfKeyword.Accept(this);
        IDisposable? disposable = null;
        node.OpenParen.Accept(this);
        if (this.formatter.PreviousToken.DoesReturnLine?.Value ?? false)
        {
            // there is no reason for an OpenParen to return line except if there is a comment.
            disposable = this.formatter.CreateScope(this.settings.Indentation);
            this.formatter.PreviousToken.ScopeInfo = this.formatter.Scope; // it's easier to change our mind that compute ahead of time.
        }
        this.formatter.CreateFoldableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () =>
        {
            var firstTokenIdx = this.formatter.CurrentIdx;
            node.Condition.Accept(this);
            var firstToken = this.formatter.TokensMetadata[firstTokenIdx];
            if (firstToken.DoesReturnLine?.Value ?? false)
            {
                firstToken.ScopeInfo.Folded.SetValue(true);
            }
        });
        node.CloseParen.Accept(this);
        disposable?.Dispose();

        this.formatter.CreateFoldableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));

        node.Else?.Accept(this);
    }
    public override void VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        if (node.IsElseIf || node.Parent!.Parent is Api.Syntax.ExpressionStatementSyntax)
        {
            this.formatter.CurrentToken.DoesReturnLine = true;
        }
        else
        {
            this.formatter.CurrentToken.DoesReturnLine = this.formatter.Scope.Folded;
        }
        node.ElseKeyword.Accept(this);
        this.formatter.CreateFoldableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Expression.Accept(this));
    }

    public override void VisitBlockExpression(Api.Syntax.BlockExpressionSyntax node)
    {
        // this means we are in a if/while/else, and *can* create an indentation with a regular expression folding:
        // if (blabla) an expression;
        // it can fold:
        // if (blabla)
        //     an expression;
        // but since we are in a block we create our own scope and the if/while/else will never create it's own scope.
        var folded = this.formatter.Scope.Folded;
        if (!folded.IsCompleted) folded.SetValue(false);

        node.OpenBrace.Accept(this);

        this.formatter.CreateScope(this.settings.Indentation, () =>
        {
            node.Statements.Accept(this);
            if (node.Value != null)
            {
                this.formatter.CurrentToken.DoesReturnLine = true;
                node.Value.Accept(this);
            }
        });
        node.CloseBrace.Accept(this);
        this.formatter.PreviousToken.DoesReturnLine = true;
    }

    public override void VisitVariableDeclaration(Api.Syntax.VariableDeclarationSyntax node)
    {
        var disposable = this.formatter.CreateScopeAfterNextToken(this.settings.Indentation);
        node.VisibilityModifier?.Accept(this);
        node.Keyword.Accept(this);
        node.Name.Accept(this);
        disposable.Dispose();
        node.Type?.Accept(this);
        node.Value?.Accept(this);
        node.Semicolon.Accept(this);
    }

    private static Api.Syntax.SyntaxNode? GetPrevious(Api.Syntax.SyntaxNode node)
    {
        var previous = null as Api.Syntax.SyntaxNode;
        foreach (var child in node.Parent!.Children)
        {
            if (child == node) return previous;

            // TODO: temp fix for AST problem.
            if (child is IReadOnlyList<Api.Syntax.SyntaxNode> list)
            {
                var previous2 = null as Api.Syntax.SyntaxNode;
                foreach (var item in list)
                {
                    if (item == node) return previous2;
                    previous2 = item;
                }
            }
            previous = child;
        }

        return null;
    }
}
