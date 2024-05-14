using System.Diagnostics.Metrics;
using System.Linq;
using System.Resources;
using Draco.Compiler.Internal.Syntax.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.Formatter.Csharp;

public sealed class CSharpFormatter(CSharpFormatterSettings settings) : CSharpSyntaxWalker(SyntaxWalkerDepth.Token)
{
    private readonly CSharpFormatterSettings settings = settings;
    private FormatterEngine formatter = null!;

    public static string Format(SyntaxTree tree, CSharpFormatterSettings? settings = null)
    {
        settings ??= CSharpFormatterSettings.Default;

        var formatter = new CSharpFormatter(settings);
        formatter.Visit(tree.GetRoot());

        var metadatas = formatter.formatter.TokensMetadata;

        return FormatterEngine.Format(settings, metadatas);
    }

    public override void VisitCompilationUnit(CompilationUnitSyntax node)
    {
        this.formatter = new FormatterEngine(node.DescendantTokens().Count(), this.settings);
        base.VisitCompilationUnit(node);
    }

    private static WhitespaceBehavior GetFormattingTokenKind(SyntaxToken token) => token.Kind() switch
    {
        SyntaxKind.AndKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.ElseKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.ForKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.GotoKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.UsingDirective => WhitespaceBehavior.PadAround,
        SyntaxKind.InKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.InternalKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.ModuleKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.OrKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.ReturnKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.PublicKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.VarKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.IfKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.WhileKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.StaticKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.SealedKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.ForEachKeyword => WhitespaceBehavior.PadAround,

        SyntaxKind.TrueKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.FalseKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.CommaToken => WhitespaceBehavior.SpaceAfter,

        SyntaxKind.SemicolonToken => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
        SyntaxKind.OpenBraceToken => WhitespaceBehavior.SpaceBefore | WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken,
        SyntaxKind.OpenParenToken => WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken,
        SyntaxKind.OpenBracketToken => WhitespaceBehavior.Whitespace,
        SyntaxKind.CloseParenToken => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
        SyntaxKind.InterpolatedStringStartToken => WhitespaceBehavior.Whitespace,
        SyntaxKind.DotToken => WhitespaceBehavior.Whitespace,

        SyntaxKind.EqualsToken => WhitespaceBehavior.PadAround,
        SyntaxKind.InterpolatedSingleLineRawStringStartToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.InterpolatedMultiLineRawStringStartToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.PlusToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.MinusToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.AsteriskToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.SlashToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.PlusEqualsToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.MinusEqualsToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.AsteriskEqualsToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.SlashEqualsToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.GreaterThanEqualsToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.GreaterThanToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.LessThanEqualsToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.LessThanToken => WhitespaceBehavior.SpaceBefore,
        SyntaxKind.NumericLiteralToken => WhitespaceBehavior.SpaceBefore,

        SyntaxKind.IdentifierToken => WhitespaceBehavior.SpaceBefore,

        _ => WhitespaceBehavior.NoFormatting
    };

    public override void VisitToken(SyntaxToken node)
    {
        if (node.IsKind(SyntaxKind.None)) return;

        base.VisitToken(node);
        var formattingKind = GetFormattingTokenKind(node);

        var notFirstToken = this.formatter.CurrentIdx > 0;
        var doesntInsertSpace = !formattingKind.HasFlag(WhitespaceBehavior.SpaceBefore);
        var insertNewline = this.formatter.CurrentToken.DoesReturnLine?.Value == true;
        var notWhitespaceNode = !node.IsKind(SyntaxKind.EndOfFileToken);
        if (doesntInsertSpace && notFirstToken && !insertNewline && notWhitespaceNode)
        {
            var tokens = SyntaxFactory.ParseTokens(this.formatter.PreviousToken.Text + node.Text);
            var secondToken = tokens.Skip(1).First();
            if (secondToken.IsKind(SyntaxKind.EndOfFileToken)) // this means the 2 tokens merged in a single one, we want to avoid that.
            {
                this.formatter.CurrentToken.Kind = WhitespaceBehavior.SpaceBefore;
            }
        }

        this.formatter.SetCurrentTokenInfo(formattingKind, node.Text);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (GetPreviousNode(node) != null)
        {
            this.formatter.CurrentToken.LeadingTrivia = [""];
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        foreach (var attribute in node.AttributeLists)
        {
            attribute.Accept(this);
        }

        foreach (var modifier in node.Modifiers)
        {
            this.VisitToken(modifier);
        }

        this.VisitToken(node.Keyword);
        this.VisitToken(node.Identifier);
        node.TypeParameterList?.Accept(this);
        node.ParameterList?.Accept(this);
        node.BaseList?.Accept(this);

        foreach (var constraint in node.ConstraintClauses)
        {
            constraint.Accept(this);
        }

        this.formatter.CurrentToken.DoesReturnLine = true;
        this.VisitToken(node.OpenBraceToken);
        this.formatter.CreateScope(this.settings.Indentation, () =>
        {
            foreach (var member in node.Members)
            {
                this.formatter.CurrentToken.DoesReturnLine = true;
                member.Accept(this);
            }
        });
        this.formatter.CurrentToken.DoesReturnLine = true;
        this.VisitToken(node.CloseBraceToken);
        this.VisitToken(node.SemicolonToken);
    }

    public override void VisitBaseList(BaseListSyntax node)
    {
        this.formatter.CurrentToken.Kind = WhitespaceBehavior.PadAround;
        base.VisitBaseList(node);
    }

    public override void VisitBlock(BlockSyntax node)
    {
        foreach (var attribute in node.AttributeLists)
        {
            attribute.Accept(this);
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        this.VisitToken(node.OpenBraceToken);
        this.formatter.CreateScope(this.settings.Indentation, () =>
        {
            foreach (var statement in node.Statements)
            {
                this.formatter.CurrentToken.DoesReturnLine = true;
                statement.Accept(this);
            }
        });
        this.formatter.CurrentToken.DoesReturnLine = true;
        this.VisitToken(node.CloseBraceToken);
    }

    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = true;
        if (GetPreviousNode(node) is not UsingDirectiveSyntax and not null)
        {
            this.formatter.CurrentToken.LeadingTrivia = [""];
        }
        base.VisitUsingDirective(node);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        if (GetPreviousNode(node) != null)
        {
            this.formatter.CurrentToken.LeadingTrivia = [""];
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        base.VisitNamespaceDeclaration(node);
    }


    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        if (GetPreviousNode(node) != null)
        {
            this.formatter.CurrentToken.LeadingTrivia = [""];
        }
        //var newData = typeof(FileScopedNamespaceDeclarationSyntax);
        //if (!newData.Equals(this.formatter.Scope.Data))
        //{
        //    if (this.formatter.Scope.Data != null)
        //    {
        //        this.formatter.CurrentToken.LeadingTrivia = [""]; // a newline is created between each leading trivia.
        //    }
        //    this.formatter.Scope.Data = newData;
        //}
        this.formatter.CurrentToken.DoesReturnLine = true;
        base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (GetPreviousNode(node) != null)
        {
            this.formatter.CurrentToken.LeadingTrivia = [""];
        }

        this.formatter.CurrentToken.DoesReturnLine = true;
        foreach (var attribute in node.AttributeLists)
        {
            attribute.Accept(this);
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        foreach (var modifier in node.Modifiers)
        {
            this.VisitToken(modifier);
        }
        node.ReturnType.Accept(this);
        node.ExplicitInterfaceSpecifier?.Accept(this);
        this.VisitToken(node.Identifier);
        node.TypeParameterList?.Accept(this);
        node.ParameterList.Accept(this);
        foreach (var constraint in node.ConstraintClauses)
        {
            constraint.Accept(this);
        }
        node.Body?.Accept(this);
        // gets index of the current method
        var count = node.Parent!.ChildNodes().Count();
        var membersIdx = node.Parent!.ChildNodes()
            .Select((n, i) => (n, i))
            .Where(x => x.n == node)
            .Select(x => x.i)
            .FirstOrDefault();
        this.VisitToken(node.SemicolonToken);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = true;
        base.VisitFieldDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (GetPreviousNode(node) != null)
        {
            this.formatter.CurrentToken.LeadingTrivia = [""];
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        foreach (var attribute in node.AttributeLists)
        {
            attribute.Accept(this);
        }
        this.formatter.CurrentToken.DoesReturnLine = true;
        foreach (var modifier in node.Modifiers)
        {
            this.VisitToken(modifier);
        }
        this.VisitToken(node.Identifier);
        node.ParameterList.Accept(this);
        if (node.Initializer != null)
        {
            using var scope = this.formatter.CreateMaterializableScope(this.settings.Indentation, FoldPriority.AsLateAsPossible);
            this.formatter.CurrentToken.DoesReturnLine = this.formatter.Scope.IsMaterialized;
            if (this.settings.NewLineBeforeConstructorInitializer)
            {
                this.formatter.Scope.IsMaterialized.Value = true;
            }
            this.formatter.CurrentToken.Kind = WhitespaceBehavior.PadAround;
            node.Initializer.Accept(this);
        }
        node.Body?.Accept(this);
        node.ExpressionBody?.Accept(this);
        this.VisitToken(node.SemicolonToken);
    }

    public override void VisitArgumentList(ArgumentListSyntax node) =>
        this.formatter.CreateMaterializableScope(this.settings.Indentation,
                FoldPriority.AsSoonAsPossible,
                () => base.VisitArgumentList(node)
        );

    public override void VisitArgument(ArgumentSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = this.formatter.Scope.IsMaterialized;
        base.VisitArgument(node);
    }

    private static SyntaxNode? GetPreviousNode(SyntaxNode node)
    {
        var parent = node.Parent;
        if (parent == null) return null;
        var previous = null as SyntaxNode;
        foreach (var child in parent.ChildNodes())
        {
            if (child is ParameterListSyntax) continue;
            if (child == node) return previous;
            previous = child;
        }
        return null;
    }

}
