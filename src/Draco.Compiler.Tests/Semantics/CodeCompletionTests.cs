using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.Semantics;

public sealed class CodeCompletionTests
{
    private void AssertCompletions(ImmutableArray<CompletionItem> actuall, params string[] expected)
    {
        Assert.Equal(expected.Length, actuall.Length);
        actuall = actuall.OrderBy(x => x.Change.InsertedText).ToImmutableArray();
        expected = expected.Order().ToArray();
        for (int i = 0; i < actuall.Length; i++)
        {
            Assert.Equal(expected[i], actuall[i].Change.InsertedText);
        }
    }

    private ImmutableArray<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel model, SyntaxPosition cursor)
    {
        var service = new CompletionService();
        service.AddProvider(new KeywordCompletionProvider());
        service.AddProvider(new SemanticCompletionProvider());
        return service.GetCompletions(tree, model, cursor);
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("gl")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("lo")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("so")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("gl")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("so")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("Consol")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("Consol")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("Co")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("S")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("Wr")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.StartsWith("App")).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.Change.InsertedText.Contains("W")).ToImmutableArray();
        var expected = new Dictionary<string, int>
        {
            { "Write", 17},
            { "WriteLine", 18 },
            { "SetWindowPosition", 1 },
            { "SetWindowSize", 1 },
        };
        Assert.Equal(4, completions.Length);
        foreach (var completion in completions)
        {
            Assert.True(expected.TryGetValue(completion.Change.InsertedText, out var type));
            Assert.Equal(type, completion.Symbols.Length);
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
        Assert.NotNull(signatures);
        Assert.Single(signatures.Overloads);
        Assert.Single(signatures.Overloads[0].Parameters);
        Assert.True(signatures.CurrentOverload.Equals(signatures.Overloads[0]));
        Assert.True(signatures.CurrentParameter.Equals(signatures.Overloads[0].Parameters[0]));
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
        Assert.NotNull(signatures);
        Assert.Equal(17, signatures.Overloads.Length);
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
        Assert.NotNull(signatures);
        Assert.Equal(26, signatures.Overloads.Length);
    }
}
