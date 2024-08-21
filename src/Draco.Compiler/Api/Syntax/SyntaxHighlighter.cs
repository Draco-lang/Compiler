using System.Collections.Generic;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Provides syntax highlighting for source code.
/// </summary>
public static class SyntaxHighlighter
{
    /// <summary>
    /// Syntax highlights the given <paramref name="tree"/>, optionally using the given <paramref name="semanticModel"/>.
    /// </summary>
    /// <param name="tree">The syntax tree to highlight.</param>
    /// <param name="semanticModel">The semantic model to use for highlighting for more accurate results.</param>
    /// <returns>The highlighted fragments of the source code.</returns>
    public static IEnumerable<HighlightFragment> Highlight(SyntaxTree tree, SemanticModel? semanticModel = null)
    {
        foreach (var token in tree.Root.Tokens)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                foreach (var fragment in Highlight(trivia, semanticModel)) yield return fragment;
            }

            foreach (var fragment in Highlight(token, semanticModel)) yield return fragment;

            foreach (var trivia in token.TrailingTrivia)
            {
                foreach (var fragment in Highlight(trivia, semanticModel)) yield return fragment;
            }
        }
    }

    private static IEnumerable<HighlightFragment> Highlight(SyntaxTrivia trivia, SemanticModel? semanticModel) => trivia.Kind switch
    {
        TriviaKind.Whitespace
     or TriviaKind.Newline => Fragment(trivia, SyntaxColoring.Whitespace),

        TriviaKind.LineComment => Fragment(trivia, SyntaxColoring.LineComment),
        TriviaKind.DocumentationComment => Fragment(trivia, SyntaxColoring.DocumentationComment),

        _ => Fragment(trivia, SyntaxColoring.Unknown),
    };

    private static IEnumerable<HighlightFragment> Highlight(SyntaxToken token, SemanticModel? semanticModel) => token.Kind switch
    {
        TokenKind.LiteralInteger
     or TokenKind.LiteralFloat => Fragment(token, SyntaxColoring.NumberLiteral),

        TokenKind.KeywordTrue
     or TokenKind.KeywordFalse => Fragment(token, SyntaxColoring.BooleanLiteral),

        TokenKind.LineStringStart
     or TokenKind.MultiLineStringStart
     or TokenKind.LineStringEnd
     or TokenKind.MultiLineStringEnd => Fragment(token, SyntaxColoring.StringQuotes),

        TokenKind.StringNewline
     or TokenKind.EscapeSequence => Fragment(token, SyntaxColoring.EscapeSequence),

        TokenKind.InterpolationStart
     or TokenKind.InterpolationEnd => Fragment(token, SyntaxColoring.InterpolationQuotes),

        TokenKind.StringContent => Fragment(token, SyntaxColoring.StringContent),

        // Characters are split up into quotes and contents
        TokenKind.LiteralCharacter => SplitUp(token, [
            (1, SyntaxColoring.CharacterQuotes),
            // The categorization depends if this is an escape
            (token.Text.Length - 2, token.Text.Contains('\\') ? SyntaxColoring.EscapeSequence : SyntaxColoring.CharacterContent),
            (1, SyntaxColoring.CharacterQuotes)]),

        TokenKind.Plus
     or TokenKind.Minus
     or TokenKind.Star
     or TokenKind.Slash
     or TokenKind.LessEqual
     or TokenKind.GreaterEqual
     or TokenKind.Equal
     or TokenKind.NotEqual
     or TokenKind.PlusAssign
     or TokenKind.MinusAssign
     or TokenKind.StarAssign
     or TokenKind.SlashAssign
     or TokenKind.KeywordMod
     or TokenKind.KeywordRem
     or TokenKind.KeywordAnd
     or TokenKind.KeywordOr
     or TokenKind.KeywordNot => Fragment(token, SyntaxColoring.Operator),

        // Greater than and less than are special cases because they can be used for generics
        TokenKind.GreaterThan
     or TokenKind.LessThan when token.Parent
            is GenericExpressionSyntax
            or GenericParameterListSyntax => Fragment(token, SyntaxColoring.Parenthesis),

        TokenKind.GreaterThan
     or TokenKind.LessThan => Fragment(token, SyntaxColoring.Operator),

        // Assignemnt is only an operator if it is not part of an inline function body
        TokenKind.Assign when token.Parent is not InlineFunctionBodySyntax => Fragment(token, SyntaxColoring.Operator),

        TokenKind.Dot
     or TokenKind.Comma
     or TokenKind.Colon
     or TokenKind.Semicolon
     or TokenKind.Ellipsis => Fragment(token, SyntaxColoring.Punctuation),

        TokenKind.KeywordIf
     or TokenKind.KeywordElse
     or TokenKind.KeywordWhile
     or TokenKind.KeywordFor
     or TokenKind.KeywordIn
     or TokenKind.KeywordReturn
     or TokenKind.KeywordGoto => Fragment(token, SyntaxColoring.ControlFlowKeyword),

        TokenKind.KeywordVar
     or TokenKind.KeywordVal
     or TokenKind.KeywordFunc
     or TokenKind.KeywordImport
     or TokenKind.KeywordModule => Fragment(token, SyntaxColoring.DeclarationKeyword),

        TokenKind.KeywordInternal
     or TokenKind.KeywordPublic => Fragment(token, SyntaxColoring.VisibilityKeyword),

        TokenKind.ParenOpen
     or TokenKind.ParenClose
     or TokenKind.CurlyOpen
     or TokenKind.CurlyClose
     or TokenKind.BracketOpen
     or TokenKind.BracketClose => Fragment(token, SyntaxColoring.Parenthesis),

        TokenKind.Identifier => Fragment(token, ColorIdentifier(token, semanticModel)),

        _ => Fragment(token, SyntaxColoring.Unknown),
    };

    private static SyntaxColoring ColorIdentifier(SyntaxToken token, SemanticModel? semanticModel)
    {
        // Make a guess based on syntax
        if (token.Parent is ParameterSyntax param && param.Name.Equals(token)) return SyntaxColoring.ParameterName;
        if (token.Parent is VariableDeclarationSyntax varDecl && varDecl.Name.Equals(token)) return SyntaxColoring.VariableName;
        if (token.Parent is FunctionDeclarationSyntax funcDecl && funcDecl.Name.Equals(token)) return SyntaxColoring.FunctionName;

        if (semanticModel is not null)
        {
            var referenced = semanticModel.GetReferencedSymbol(token);
            if (referenced is not null)
            {
                // NOTE: Do we want to simplify this in the API?
                while (referenced is IAliasSymbol alias) referenced = alias.Substitution;
                return referenced switch
                {
                    ITypeSymbol t => t.IsValueType
                        ? SyntaxColoring.ValueTypeName
                        : SyntaxColoring.ReferenceTypeName,
                    IFunctionSymbol => SyntaxColoring.FunctionName,
                    IParameterSymbol => SyntaxColoring.ParameterName,
                    IVariableSymbol => SyntaxColoring.VariableName,
                    _ => SyntaxColoring.Unknown,
                };
            }
        }

        // Best effort approximation
        if (token.Parent is NameTypeSyntax) return SyntaxColoring.ReferenceTypeName;

        return SyntaxColoring.Unknown;
    }

    private static IEnumerable<HighlightFragment> Fragment(SyntaxTrivia trivia, SyntaxColoring color) =>
        [new HighlightFragment(trivia, color)];

    private static IEnumerable<HighlightFragment> Fragment(SyntaxToken token, SyntaxColoring color) =>
        [new HighlightFragment(token, color)];

    private static IEnumerable<HighlightFragment> SplitUp(SyntaxToken token, IEnumerable<(int Length, SyntaxColoring Color)> parts)
    {
        var offset = 0;
        foreach (var (len, color) in parts)
        {
            if (token.Text.Length <= offset) break;

            yield return new HighlightFragment(token, new SourceSpan(offset, len), color);
            offset += len;
        }
    }
}
