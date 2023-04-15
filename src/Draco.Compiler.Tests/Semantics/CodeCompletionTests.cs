using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.Semantics;

public sealed class CodeCompletionTests
{
    private void AssertCompletions(ImmutableArray<CompletionItem> actuall, params string[] expected)
    {
        Assert.Equal(expected.Length, actuall.Length);
        actuall = actuall.OrderBy(x => x.Text).ToImmutableArray();
        expected = expected.Order().ToArray();
        for (int i = 0; i < actuall.Length; i++)
        {
            Assert.Equal(expected[i], actuall[i].Text);
        }
    }

    [Fact]
    public void TestLocalCompletionGlobalVariable()
    {
        string code = """
            val global = 5;
            func main(){
                var local = gl
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(2, 18);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("gl")).ToImmutableArray();
        this.AssertCompletions(completions, "global");
    }

    [Fact]
    public void TestLocalCompletionLocalVariable()
    {
        string code = """
            func main(){
                val local = 5;
                var x = lo
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(2, 18);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("lo")).ToImmutableArray();
        this.AssertCompletions(completions, "local");
    }

    [Fact]
    public void TestLocalCompletionFunction()
    {
        string code = """
            func main(){
                var x = so
            }

            func something() = 5;
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(1, 18);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("so")).ToImmutableArray();
        this.AssertCompletions(completions, "something");
    }
}
