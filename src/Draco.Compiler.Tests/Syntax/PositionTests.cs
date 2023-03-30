using Draco.Compiler.Api.Syntax;
using Range = Draco.Compiler.Api.Syntax.SyntaxRange;

namespace Draco.Compiler.Tests.Syntax;

public sealed class PositionTests
{
    [Fact]
    public void TestPositionCalculation()
    {
        var tree = SyntaxTree.Parse(""""
            // Hello world
            var a = """
                This is a string
                With multiple lines
                """;

            func foo() {
                // A new variable
                var b = a;
                println(b);
            }
            """");

        var aDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var bDecl = tree.FindInChildren<VariableDeclarationSyntax>(1);

        Assert.Equal(new Range(
            Start: new(Line: 1, Column: 0),
            End: new(Line: 4, Column: 8)),
            aDecl.Range);
        Assert.Equal(new Range(
            Start: new(Line: 8, Column: 4),
            End: new(Line: 8, Column: 14)),
            bDecl.Range);
    }
}
