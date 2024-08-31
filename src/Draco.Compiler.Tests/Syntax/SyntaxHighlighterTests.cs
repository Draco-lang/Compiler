using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Tests.Semantics;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Syntax;

public sealed class SyntaxHighlighterTests
{
    private const string sampleCode = """
        import System;
        import System.Console;
        import System.Collections.Generic;

        /// Hello doc comment
        func hashThem(x: int32, y: int32): int32 {
            var h: HashCode = default<HashCode>();
            h.Add(x);
            return h.ToHashCode();
        }

        // This is a comment
        public func main() {
            WriteLine("h(1, 2) = \{hashThem(1, 2)}");

            var l1 = List<string>();
            l1.Add("a\nb");

            val l2 = List<char>();
            l2.Add('a');
            l2.Add('\u{abc}');
        }
        """;

    private static readonly ImmutableArray<(string Text, SyntaxColoring Color)> expectedHighlighting = [
        ("import", SyntaxColoring.DeclarationKeyword),
        ("System", SyntaxColoring.ModuleName),
        (";", SyntaxColoring.Punctuation),

        ("import", SyntaxColoring.DeclarationKeyword),
        ("System", SyntaxColoring.ModuleName),
        (".", SyntaxColoring.Punctuation),
        ("Console", SyntaxColoring.ModuleName),
        (";", SyntaxColoring.Punctuation),

        ("import", SyntaxColoring.DeclarationKeyword),
        ("System", SyntaxColoring.ModuleName),
        (".", SyntaxColoring.Punctuation),
        ("Collections", SyntaxColoring.ModuleName),
        (".", SyntaxColoring.Punctuation),
        ("Generic", SyntaxColoring.ModuleName),
        (";", SyntaxColoring.Punctuation),

        ("/// Hello doc comment", SyntaxColoring.DocumentationComment),
        ("func", SyntaxColoring.DeclarationKeyword),
        ("hashThem", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        ("x", SyntaxColoring.ParameterName),
        (":", SyntaxColoring.Punctuation),
        ("int32", SyntaxColoring.ValueTypeName),
        (",", SyntaxColoring.Punctuation),
        ("y", SyntaxColoring.ParameterName),
        (":", SyntaxColoring.Punctuation),
        ("int32", SyntaxColoring.ValueTypeName),
        (")", SyntaxColoring.Parenthesis),
        (":", SyntaxColoring.Punctuation),
        ("int32", SyntaxColoring.ValueTypeName),
        ("{", SyntaxColoring.Parenthesis),

        ("var", SyntaxColoring.DeclarationKeyword),
        ("h", SyntaxColoring.VariableName),
        (":", SyntaxColoring.Punctuation),
        ("HashCode", SyntaxColoring.ValueTypeName),
        ("=", SyntaxColoring.Operator),
        ("default", SyntaxColoring.FunctionName),
        ("<", SyntaxColoring.Parenthesis),
        ("HashCode", SyntaxColoring.ValueTypeName),
        (">", SyntaxColoring.Parenthesis),
        ("(", SyntaxColoring.Parenthesis),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("h", SyntaxColoring.VariableName),
        (".", SyntaxColoring.Punctuation),
        ("Add", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        ("x", SyntaxColoring.ParameterName),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("return", SyntaxColoring.ControlFlowKeyword),
        ("h", SyntaxColoring.VariableName),
        (".", SyntaxColoring.Punctuation),
        ("ToHashCode", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("}", SyntaxColoring.Parenthesis),

        ("// This is a comment", SyntaxColoring.LineComment),
        ("public", SyntaxColoring.VisibilityKeyword),
        ("func", SyntaxColoring.DeclarationKeyword),
        ("main", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        (")", SyntaxColoring.Parenthesis),
        ("{", SyntaxColoring.Parenthesis),

        ("WriteLine", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        ("\"", SyntaxColoring.StringQuotes),
        ("h(1, 2) = ", SyntaxColoring.StringContent),
        ("\\{", SyntaxColoring.InterpolationQuotes),
        ("hashThem", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        ("1", SyntaxColoring.NumberLiteral),
        (",", SyntaxColoring.Punctuation),
        ("2", SyntaxColoring.NumberLiteral),
        (")", SyntaxColoring.Parenthesis),
        ("}", SyntaxColoring.InterpolationQuotes),
        ("\"", SyntaxColoring.StringQuotes),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("var", SyntaxColoring.DeclarationKeyword),
        ("l1", SyntaxColoring.VariableName),
        ("=", SyntaxColoring.Operator),
        ("List", SyntaxColoring.FunctionName),
        ("<", SyntaxColoring.Parenthesis),
        ("string", SyntaxColoring.ReferenceTypeName),
        (">", SyntaxColoring.Parenthesis),
        ("(", SyntaxColoring.Parenthesis),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("l1", SyntaxColoring.VariableName),
        (".", SyntaxColoring.Punctuation),
        ("Add", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        ("\"", SyntaxColoring.StringQuotes),
        ("a", SyntaxColoring.StringContent),
        ("\\n", SyntaxColoring.EscapeSequence),
        ("b", SyntaxColoring.StringContent),
        ("\"", SyntaxColoring.StringQuotes),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("val", SyntaxColoring.DeclarationKeyword),
        ("l2", SyntaxColoring.VariableName),
        ("=", SyntaxColoring.Operator),
        ("List", SyntaxColoring.FunctionName),
        ("<", SyntaxColoring.Parenthesis),
        ("char", SyntaxColoring.ValueTypeName),
        (">", SyntaxColoring.Parenthesis),
        ("(", SyntaxColoring.Parenthesis),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("l2", SyntaxColoring.VariableName),
        (".", SyntaxColoring.Punctuation),
        ("Add", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        ("'", SyntaxColoring.CharacterQuotes),
        ("a", SyntaxColoring.CharacterContent),
        ("'", SyntaxColoring.CharacterQuotes),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("l2", SyntaxColoring.VariableName),
        (".", SyntaxColoring.Punctuation),
        ("Add", SyntaxColoring.FunctionName),
        ("(", SyntaxColoring.Parenthesis),
        ("'", SyntaxColoring.CharacterQuotes),
        ("\\u{abc}", SyntaxColoring.EscapeSequence),
        ("'", SyntaxColoring.CharacterQuotes),
        (")", SyntaxColoring.Parenthesis),
        (";", SyntaxColoring.Punctuation),

        ("}", SyntaxColoring.Parenthesis),

        ("", SyntaxColoring.Unknown),
    ];

    [Fact]
    public void HighlightingTest()
    {
        // Arrange
        var tree = SyntaxTree.Parse(sampleCode);
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        // Act
        var highlighting = SyntaxHighlighter.Highlight(tree, semanticModel);

        // Assert
        var nonWhitespaceHighlighting = highlighting
            .Where(h => h.Color != SyntaxColoring.Whitespace)
            .Select(h => (Text: h.Text, Color: h.Color));
        Assert.Equal(expectedHighlighting, nonWhitespaceHighlighting);
    }
}
