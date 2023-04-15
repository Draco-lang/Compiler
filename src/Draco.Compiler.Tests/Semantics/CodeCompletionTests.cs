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

            func something(): int32 = 5;
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(1, 18);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("so")).ToImmutableArray();
        this.AssertCompletions(completions, "something");
    }

    [Fact]
    public void TestGlobalCompletionGlobalVariable()
    {
        string code = """
            val global = 5;
            val x = gl
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(1, 10);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("gl")).ToImmutableArray();
        this.AssertCompletions(completions, "global");
    }

    [Fact]
    public void TestGlobalCompletionFunction()
    {
        string code = """
            var x = so

            func something(): int32 = 5;
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(0, 10);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("so")).ToImmutableArray();
        this.AssertCompletions(completions, "something");
    }

    [Fact]
    public void TestCompletionImportedSystem()
    {
        string code = """
            import System;
            func main(){
                Consol
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(2, 10);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("Consol")).ToImmutableArray();
        var expected = new[]
        {
            "Console",
            "ConsoleCancelEventArgs",
            "ConsoleCancelEventHandler",
            "ConsoleColor",
            "ConsoleKey",
            "ConsoleKeyInfo",
            "ConsoleModifiers",
            "ConsoleSpecialKey"
        };
        this.AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionFullyQualifiedName()
    {
        string code = """
            func main(){
                System.Consol
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(1, 17);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("Consol")).ToImmutableArray();
        var expected = new[]
        {
            "Console",
            "ConsoleCancelEventArgs",
            "ConsoleCancelEventHandler",
            "ConsoleColor",
            "ConsoleKey",
            "ConsoleKeyInfo",
            "ConsoleModifiers",
            "ConsoleSpecialKey"
        };
        this.AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionImportMember()
    {
        string code = """
            import System.Co
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(0, 16);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("Co")).ToImmutableArray();
        var expected = new[]
        {
            "CodeDom",
            "Collections",
            "ComponentModel",
            "Configuration",
            "Console",
            "Convert",
        };
        this.AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionImportRoot()
    {
        string code = """
            import S
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(0, 8);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("S")).ToImmutableArray();
        this.AssertCompletions(completions, "System");
    }

    [Fact]
    public void TestCompletionModuleMemberAccess()
    {
        string code = """
            import System;
            func main(){
                Console.Wr
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(2, 14);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("Wr")).ToImmutableArray();
        var expected = new[]
        {
            "Write",
            "WriteLine",
        };
        this.AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionLocalMemberAccess()
    {
        string code = """
            import System.Text;
            func main(){
                var builder = StringBuilder();
                builder.App
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(3, 15);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.StartsWith("App")).ToImmutableArray();
        var expected = new[]
        {
            "Append",
            "AppendFormat",
            "AppendJoin",
            "AppendLine",
        };
        this.AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionTypeSignature()
    {
        string code = """
            import System;
            func main(){
                Console.W
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(2, 13);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = CompletionService.GetCompletions(tree, semanticModel, cursor).Where(x => x.Text.Contains("W")).ToImmutableArray();
        var expected = new Dictionary<string, string>
        {
            { "Write", "17 overloads"},
            { "WriteLine", "18 overloads" },
            { "SetWindowPosition", "(left: int32, top: int32) -> unit" },
            { "SetWindowSize", "(width: int32, height: int32) -> unit" },
        };
        Assert.Equal(4, completions.Length);
        foreach (var completion in completions)
        {
            Assert.True(expected.TryGetValue(completion.Text, out var type));
            Assert.Equal(type, completion.Type);
        }
    }

    [Fact]
    public void TestSignatureHelpLocalFunction()
    {
        string code = """
            func main(){
                var x = something()
            }

            func something(x: string): int32 = 5;
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(1, 22);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var signatures = SignatureService.GetSignature(tree, semanticModel, cursor);
        Assert.Single(signatures.Signatures);
        Assert.Single(signatures.Signatures[0].Parameters);
        Assert.Equal("func something(x: string): int32", signatures.Signatures[0].Label);
        Assert.Equal("x", signatures.Signatures[0].Parameters[0]);
    }

    [Fact]
    public void TestSignatureHelpModuleMemberAccess()
    {
        string code = """
            import System;
            func main(){
                Console.Write()
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(2, 18);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var signatures = SignatureService.GetSignature(tree, semanticModel, cursor);
        Assert.Equal(17, signatures.Signatures.Length);
    }

    [Fact]
    public void TestSignatureHelpTypeMemberAccess()
    {
        string code = """
            import System.Text;
            func main(){
                var builder = StringBuilder();
                builder.Append();
            }
            """;
        var tree = SyntaxTree.Parse(SourceText.FromText(code));
        var cursor = new SyntaxPosition(3, 18);

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var signatures = SignatureService.GetSignature(tree, semanticModel, cursor);
        Assert.Equal(26, signatures.Signatures.Length);
    }
}
