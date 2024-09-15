using Draco.Compiler.Api.Services.Signature;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Services;

public sealed class SignatureHelpTests
{
    private static SignatureItem? GetSignatureHelp(string code, char cursor = '|')
    {
        var tree = SyntaxTree.Parse(code);
        var cursorIndex = code.IndexOf(cursor);

        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var signatureService = SignatureService.CreateDefault();
        return signatureService.GetSignature(semanticModel, cursorIndex);
    }

    [Fact]
    public void TestSignatureHelpLocalFunction()
    {
        var signatures = GetSignatureHelp("""
            func main(){
                var x = something(|)
            }

            func something(x: string): int32 = 5;
            """);

        Assert.NotNull(signatures);
        Assert.NotNull(signatures.CurrentParameter);
        Assert.Single(signatures.Overloads);
        Assert.Single(signatures.Overloads[0].Parameters);
        Assert.True(signatures.BestMatch.Equals(signatures.Overloads[0]));
        Assert.True(signatures.CurrentParameter.Equals(signatures.Overloads[0].Parameters[0]));
    }

    [Fact]
    public void TestSignatureHelpModuleMemberAccess()
    {
        var signatures = GetSignatureHelp("""
            import System;
            func main(){
                Console.Write(|)
            }
            """);

        Assert.NotNull(signatures);
        Assert.Equal(17, signatures.Overloads.Length);
    }

    [Fact]
    public void TestSignatureHelpTypeMemberAccess()
    {
        var signatures = GetSignatureHelp("""
            import System.Text;
            func main(){
                var builder = StringBuilder();
                builder.Append(|);
            }
            """);

        Assert.NotNull(signatures);
        Assert.Equal(26, signatures.Overloads.Length);
    }

    [Fact]
    public void TestSignatureHelpTypeConstructor()
    {
        var signatures = GetSignatureHelp("""
            import System.Text;
            func main(){
                var builder = StringBuilder(|);
            }
            """);

        Assert.NotNull(signatures);
        Assert.Equal(6, signatures.Overloads.Length);
    }

    [Theory]
    [InlineData("foo(|12, \"asd\", true)", 0)]
    [InlineData("foo(12|, \"asd\", true)", 0)]
    [InlineData("foo(12,| \"asd\", true)", 1)]
    [InlineData("foo(12, \"asd\"|, true)", 1)]
    [InlineData("foo(12, \"asd\",| true)", 2)]
    [InlineData("foo(12, \"asd\", true|)", 2)]
    public void TestSignatureHelpCursorPosition(string call, int paramIndex)
    {
        var signatures = GetSignatureHelp($$"""
            func main() {
                var builder = {{call}};
            }

            func foo(x: int32, y: string, z: bool) {}
            """);

        Assert.NotNull(signatures);
        Assert.Single(signatures.Overloads);
        var overload = signatures.Overloads[0];
        Assert.Equal(overload, signatures.BestMatch);
        Assert.Equal(overload.Parameters[paramIndex], signatures.CurrentParameter);
    }

    [Theory]
    [InlineData("foo(|12, \"asd\", true)", 0)]
    [InlineData("foo(12|, \"asd\", true)", 0)]
    [InlineData("foo(12,| \"asd\", true)", 1)]
    [InlineData("foo(12, \"asd\"|, true)", 1)]
    [InlineData("foo(12, \"asd\",| true)", 2)]
    [InlineData("foo(12, \"asd\", true|)", 2)]
    [InlineData("foo(12, \"asd\", true|, false, false)", 2)]
    [InlineData("foo(12, \"asd\", true, false|, false)", 2)]
    [InlineData("foo(12, \"asd\", true, false, |false)", 2)]
    [InlineData("foo(12, \"asd\", true, false, false|)", 2)]
    public void TestSignatureHelpVariadicCursorPosition(string call, int paramIndex)
    {
        var signatures = GetSignatureHelp($$"""
            func main() {
                var builder = {{call}};
            }

            func foo(x: int32, y: string, ...z: Array<bool>) {}
            """);

        Assert.NotNull(signatures);
        Assert.Single(signatures.Overloads);
        var overload = signatures.Overloads[0];
        Assert.Equal(overload, signatures.BestMatch);
        Assert.Equal(overload.Parameters[paramIndex], signatures.CurrentParameter);
    }
}
