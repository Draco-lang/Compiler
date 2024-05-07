using System.Linq;
using Draco.Compiler.Internal.Syntax.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.Formatter.Csharp;

public sealed class CSharpFormatter(FormatterSettings settings) : CSharpSyntaxWalker(SyntaxWalkerDepth.Token)
{
    private readonly FormatterSettings settings = settings;
    private FormatterEngine formatter = null!;

    public static string Format(SyntaxTree tree, FormatterSettings? settings = null)
    {
        settings ??= FormatterSettings.Default;

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

        SyntaxKind.TrueKeyword => WhitespaceBehavior.PadAround,
        SyntaxKind.FalseKeyword => WhitespaceBehavior.PadAround,

        SyntaxKind.SemicolonToken => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
        SyntaxKind.OpenBraceToken => WhitespaceBehavior.PadLeft | WhitespaceBehavior.BehaveAsWhiteSpaceForNextToken,
        SyntaxKind.OpenParenToken => WhitespaceBehavior.Whitespace,
        SyntaxKind.OpenBracketToken => WhitespaceBehavior.Whitespace,
        SyntaxKind.CloseParenToken => WhitespaceBehavior.BehaveAsWhiteSpaceForPreviousToken,
        SyntaxKind.InterpolatedStringStartToken => WhitespaceBehavior.Whitespace,
        SyntaxKind.DotToken => WhitespaceBehavior.Whitespace,

        SyntaxKind.EqualsToken => WhitespaceBehavior.PadAround,
        SyntaxKind.InterpolatedSingleLineRawStringStartToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.InterpolatedMultiLineRawStringStartToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.PlusToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.MinusToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.AsteriskToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.SlashToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.PlusEqualsToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.MinusEqualsToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.AsteriskEqualsToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.SlashEqualsToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.GreaterThanEqualsToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.GreaterThanToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.LessThanEqualsToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.LessThanToken => WhitespaceBehavior.PadLeft,
        SyntaxKind.NumericLiteralToken => WhitespaceBehavior.PadLeft,

        SyntaxKind.IdentifierToken => WhitespaceBehavior.PadLeft,

        _ => WhitespaceBehavior.NoFormatting
    };

    public override void VisitToken(SyntaxToken node)
    {
        if(node.IsKind(SyntaxKind.None)) return;

        base.VisitToken(node);
        this.formatter.SetCurrentTokenInfo(GetFormattingTokenKind(node), node.Text);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
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
        base.VisitUsingDirective(node);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = true;
        base.VisitNamespaceDeclaration(node);
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        this.formatter.CurrentToken.DoesReturnLine = true;
        base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
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
}
