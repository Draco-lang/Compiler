using System;
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

        var metadatas = formatter.formatter.TokensMetadata;

        return FormatterEngine.Format(settings, metadatas);
    }

    public override void VisitCompilationUnit(Api.Syntax.CompilationUnitSyntax node)
    {
        this.formatter = new FormatterEngine(node.Tokens.Count(), this.settings);
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

            TokenKind.KeywordFunc => WhitespaceBehavior.PadAround,


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
        this.HandleTokenComments(node);
        this.formatter.SetCurrentTokenInfo(GetFormattingTokenKind(node), node.Text);

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
            || node is Api.Syntax.SeparatedSyntaxList<Api.Syntax.ExpressionSyntax>)
        {
            this.formatter.CreateMaterializableScope(this.settings.Indentation,
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
        this.formatter.CurrentToken.DoesReturnLine = this.formatter.Scope.IsMaterialized;
        base.VisitParameter(node);
    }

    public override void VisitDeclaration(Api.Syntax.DeclarationSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = true;
        var type = node.GetType();
        var data = node switch
        {
            Api.Syntax.FunctionDeclarationSyntax _ => node, // always different, that what we want.
            _ => type as object,
        };

        if (!data.Equals(this.formatter.Scope.Data))
        {
            if (this.formatter.Scope.Data != null)
            {
                this.formatter.CurrentToken.LeadingTrivia = [""]; // a newline is created between each leading trivia.
            }
            this.formatter.Scope.Data = data;
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
                this.formatter.CurrentToken.DoesReturnLine = false;
                item.Accept(this);
            }
            this.formatter.CurrentToken.DoesReturnLine = false;
            node.CloseQuotes.Accept(this);
            return;
        }
        node.OpenQuotes.Accept(this);
        using var _ = this.formatter.CreateScope(this.settings.Indentation);
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
                this.formatter.CurrentToken.Text = tokenText[blockCurrentIndentCount..];
                this.formatter.CurrentToken.DoesReturnLine = true;

                if (i > 0 && node.Parts[i - 1].IsNewLine)
                {
                    this.formatter.PreviousToken.Text = ""; // PreviousToken is a newline, CurrentToken.DoesReturnLine will produce the newline.
                }
            }

            var startIdx = this.formatter.CurrentIdx; // capture position before visiting the tokens of this parts (this will move forward the position)

            // for parts that contains expressions and have return lines.
            foreach (var token in curr.Tokens)
            {
                var newLines = token.TrailingTrivia.Where(t => t.Kind == TriviaKind.Newline).ToArray();
                if (newLines.Length > 0)
                {
                    this.formatter.NextToken.DoesReturnLine = true;
                    this.formatter.CurrentToken.Text = string.Concat(Enumerable.Repeat(this.settings.Newline, newLines.Length - 1).Prepend(token.Text));
                }
                token.Accept(this);
            }

            // default all tokens to never return.
            var tokenCount = curr.Tokens.Count();

            for (var j = 0; j < tokenCount; j++)
            {
                this.formatter.TokensMetadata[startIdx + j].DoesReturnLine ??= false;
            }
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        node.CloseQuotes.Accept(this);
    }

    public override void VisitBinaryExpression(Api.Syntax.BinaryExpressionSyntax node)
    {
        var closeScope = null as IDisposable;
        var kind = node.Operator.Kind;
        if (!(this.formatter.Scope.Data?.Equals(kind) ?? false))
        {
            closeScope = this.formatter.CreateMaterializableScope("", FoldPriority.AsLateAsPossible);
            this.formatter.Scope.Data = kind;
        }

        node.Left.Accept(this);

        if (this.formatter.CurrentToken.DoesReturnLine is null)
        {
            this.formatter.CurrentToken.DoesReturnLine = this.formatter.Scope.IsMaterialized;
        }
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

        using var _ = this.formatter.CreateMaterializableScope(curr, FoldPriority.AsSoonAsPossible);
        node.Value.Accept(this);
        node.Semicolon.Accept(this);
    }

    public override void VisitFunctionDeclaration(Api.Syntax.FunctionDeclarationSyntax node)
    {
        this.VisitDeclaration(node);
        var disposable = this.formatter.CreateScopeAfterNextToken(this.settings.Indentation);
        node.VisibilityModifier?.Accept(this);
        node.FunctionKeyword.Accept(this);
        node.Name.Accept(this);
        if (node.Generics is not null)
        {
            this.formatter.CreateMaterializableScope(this.settings.Indentation, FoldPriority.AsLateAsPossible, () => node.Generics?.Accept(this));
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
            this.formatter.CurrentToken.DoesReturnLine = shouldBeMultiLine;
        }
        base.VisitStatement(node);
    }

    public override void VisitWhileExpression(Api.Syntax.WhileExpressionSyntax node)
    {
        node.WhileKeyword.Accept(this);
        node.OpenParen.Accept(this);
        this.formatter.CreateMaterializableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Condition.Accept(this));
        node.CloseParen.Accept(this);
        this.formatter.CreateMaterializableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node) =>
        this.formatter.CreateMaterializableScope("", FoldPriority.AsSoonAsPossible, () =>
        {
            node.IfKeyword.Accept(this);
            IDisposable? disposable = null;
            node.OpenParen.Accept(this);
            if (this.formatter.PreviousToken.DoesReturnLine?.Value ?? false)
            {
                // there is no reason for an OpenParen to return line except if there is a comment.
                disposable = this.formatter.CreateScope(this.settings.Indentation);
                this.formatter.PreviousToken.ScopeInfo = this.formatter.Scope; // it's easier to change our mind that compute ahead of time.
            }
            this.formatter.CreateMaterializableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () =>
            {
                var firstTokenIdx = this.formatter.CurrentIdx;
                node.Condition.Accept(this);
                var firstToken = this.formatter.TokensMetadata[firstTokenIdx];
                if (firstToken.DoesReturnLine?.Value ?? false)
                {
                    firstToken.ScopeInfo.IsMaterialized.Value = true;
                }
            });
            node.CloseParen.Accept(this);
            disposable?.Dispose();

            this.formatter.CreateMaterializableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Then.Accept(this));

            node.Else?.Accept(this);
        });

    public override void VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        if (node.IsElseIf || node.Parent!.Parent is Api.Syntax.ExpressionStatementSyntax)
        {
            this.formatter.CurrentToken.DoesReturnLine = true;
        }
        else
        {
            this.formatter.CurrentToken.DoesReturnLine = this.formatter.Scope.IsMaterialized;
        }
        node.ElseKeyword.Accept(this);
        this.formatter.CreateMaterializableScope(this.settings.Indentation, FoldPriority.AsSoonAsPossible, () => node.Expression.Accept(this));
    }

    public override void VisitBlockExpression(Api.Syntax.BlockExpressionSyntax node)
    {
        // this means we are in a if/while/else, and *can* create an indentation with a regular expression folding:
        // if (blabla) an expression;
        // it can fold:
        // if (blabla)
        //     an expression;
        // but since we are in a block we create our own scope and the if/while/else will never create it's own scope.
        this.formatter.Scope.IsMaterialized.Value ??= false;

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
}
