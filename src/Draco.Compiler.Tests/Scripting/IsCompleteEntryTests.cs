using Draco.Compiler.Api.Scripting;

namespace Draco.Compiler.Tests.Repl;

public sealed class IsCompleteEntryTests
{
    [InlineData("", true)]
    [InlineData("// hello comments", true)]
    [InlineData("1", true)]
    [InlineData("1 + 2", true)]
    [InlineData("1 + ", false)]
    [InlineData("foo()", true)]
    [InlineData("foo(", false)]
    [InlineData("var x = 0;", true)]
    [InlineData("x < y", true)]
    [InlineData("x < y < z", true)]
    [InlineData("func foo() {", false)]
    [InlineData("""
        func foo() {
        }
        """, true)]
    [Theory]
    public void IsCompleteEntry(string input, bool expected)
    {
        var result = ReplSession.IsCompleteEntry(input);
        Assert.Equal(expected, result);
    }
}
