using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.Semantics;

public sealed class CodeCompletionTests
{
    private void AssertCompletions(ImmutableArray<TextEdit> actuall, params string[] expected)
    {
        Assert.Equal(expected.Length, actuall.Length);
        actuall = actuall.OrderBy(x => x.Text).ToImmutableArray();
        expected = expected.Order().ToArray();
        for (int i = 0; i < actuall.Length; i++)
        {
            Assert.Equal(expected[i], actuall[i].Text);
        }
    }

    private ImmutableArray<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel model, SyntaxPosition cursor)
    {
        var service = new CompletionService();
        service.AddProvider(new KeywordCompletionProvider());
        service.AddProvider(new ExpressionCompletionProvider());
        service.AddProvider(new MemberAccessCompletionProvider());
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("gl"))).ToImmutableArray();
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
        var cursor = new SyntaxPosition(2, 14);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("lo"))).ToImmutableArray();
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
        var cursor = new SyntaxPosition(1, 14);
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("so"))).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("gl"))).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("so"))).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Consol"))).ToImmutableArray();
        var expected = new[]
        {
            "Console",
            "ConsoleCancelEventArgs", // The actuall type
            "ConsoleCancelEventArgs", // Its ctor
            "ConsoleCancelEventHandler", // The actuall type
            "ConsoleCancelEventHandler", // Its ctor
            "ConsoleColor",
            "ConsoleKey",
            "ConsoleKeyInfo", // The actuall type
            "ConsoleKeyInfo", // Its ctor
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Consol"))).ToImmutableArray();
        var expected = new[]
        {
            "Console",
            "ConsoleCancelEventArgs", // The actuall type
            "ConsoleCancelEventArgs", // Its ctor
            "ConsoleCancelEventHandler", // The actuall type
            "ConsoleCancelEventHandler", // Its ctor
            "ConsoleColor",
            "ConsoleKey",
            "ConsoleKeyInfo", // The actuall type
            "ConsoleKeyInfo", // Its ctor
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Co"))).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("S"))).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Wr"))).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("App"))).ToImmutableArray();
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
        var completions = this.GetCompletions(tree, semanticModel, cursor).Where(x => x.DisplayText.Contains("W")).ToImmutableArray();
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
            Assert.True(expected.TryGetValue(completion.DisplayText, out var type));
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
        var service = new SignatureService();
        var signatures = service.GetSignature(tree, semanticModel, cursor);
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
        var service = new SignatureService();
        var signatures = service.GetSignature(tree, semanticModel, cursor);
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
        var service = new SignatureService();
        var signatures = service.GetSignature(tree, semanticModel, cursor);
        Assert.NotNull(signatures);
        Assert.Equal(26, signatures.Overloads.Length);
    }
}
