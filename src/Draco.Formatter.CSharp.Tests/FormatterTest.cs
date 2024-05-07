using Draco.Formatter.Csharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Draco.Formatter.CSharp.Test;

public sealed class FormatterTest(ITestOutputHelper logger)
{
    [Fact]
    public void SomeCodeSampleShouldBeFormattedCorrectly()
    {
        var input = """"
            class Program
            {
                public static void Main()
                {
                    Console.WriteLine("Hello, World!");
                }
            }

            """";
        var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(input));
        var formatted = CSharpFormatter.Format(tree);
        logger.WriteLine(formatted);
        Assert.Equal(input, formatted, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ThisFileShouldBeFormattedCorrectly()
    {
        var input = File.ReadAllText("../../../../Draco.Formatter.Csharp.Tests/FormatterTest.cs");
        var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(input));
        var formatted = CSharpFormatter.Format(tree);
        logger.WriteLine(formatted);
        Assert.Equal(input, formatted, ignoreLineEndingDifferences: true);
    }
}
