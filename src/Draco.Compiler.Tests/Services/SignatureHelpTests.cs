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
        Assert.True(signatures.CurrentOverload.Equals(signatures.Overloads[0]));
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
}
