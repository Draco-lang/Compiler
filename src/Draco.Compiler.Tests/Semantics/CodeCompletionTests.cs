using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.Semantics;

public sealed class CodeCompletionTests
{
    private static void AssertCompletions(ImmutableArray<TextEdit> actual, params string[] expected) =>
        Assert.True(actual.Select(x => x.Text).ToHashSet().SetEquals(expected));

    private static ImmutableArray<CompletionItem> GetCompletions(SyntaxTree tree, SemanticModel model, SyntaxPosition cursor)
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
        var code = """
            val global = 5;
            func main(){
                var local = gl|
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("gl"))).ToImmutableArray();
        AssertCompletions(completions, "global");
    }

    [Fact]
    public void TestLocalCompletionLocalVariable()
    {
        var code = """
            func main(){
                val local = 5;
                var x = lo|
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("lo"))).ToImmutableArray();
        AssertCompletions(completions, "local");
    }

    [Fact]
    public void TestLocalCompletionFunction()
    {
        var code = """
            func main(){
                var x = so|
            }

            func something(): int32 = 5;
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("so"))).ToImmutableArray();
        AssertCompletions(completions, "something");
    }

    [Fact]
    public void TestGlobalCompletionGlobalVariable()
    {
        var code = """
            val global = 5;
            val x = gl|
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("gl"))).ToImmutableArray();
        AssertCompletions(completions, "global");
    }

    [Fact]
    public void TestGlobalCompletionFunction()
    {
        var code = """
            var x = so|

            func something(): int32 = 5;
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("so"))).ToImmutableArray();
        AssertCompletions(completions, "something");
    }

    [Fact]
    public void TestCompletionImportedSystem()
    {
        var code = """
            import System;
            func main(){
                Consol|
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Consol"))).ToImmutableArray();
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
        AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionFullyQualifiedName()
    {
        var code = """
            func main(){
                System.Consol|
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Consol"))).ToImmutableArray();
        var expected = new[]
        {
            "Console",
            "ConsoleCancelEventArgs", // The actual type
            "ConsoleCancelEventArgs", // Its ctor
            "ConsoleCancelEventHandler", // The actual type
            "ConsoleCancelEventHandler", // Its ctor
            "ConsoleColor",
            "ConsoleKey",
            "ConsoleKeyInfo", // The actual type
            "ConsoleKeyInfo", // Its ctor
            "ConsoleModifiers",
            "ConsoleSpecialKey"
        };
        AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionImportMember()
    {
        var code = """
            import System.Co|
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Co"))).ToImmutableArray();
        var expected = new[]
        {
            "CodeDom",
            "Collections",
            "ComponentModel",
            "Configuration",
            "Console",
            "Convert",
        };
        AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionImportRoot()
    {
        var code = """
            import S|
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("S"))).ToImmutableArray();
        AssertCompletions(completions, "System");
    }

    [Fact]
    public void TestCompletionModuleMemberAccess()
    {
        var code = """
            import System;
            func main(){
                Console.Wr|
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Wr"))).ToImmutableArray();
        var expected = new[]
        {
            "Write",
            "WriteLine",
        };
        AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionLocalMemberAccess()
    {
        var code = """
            import System.Text;
            func main(){
                var builder = StringBuilder();
                builder.App|
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("App"))).ToImmutableArray();
        var expected = new[]
        {
            "Append",
            "AppendFormat",
            "AppendJoin",
            "AppendLine",
        };
        AssertCompletions(completions, expected);
    }

    [Fact]
    public void TestCompletionTypeSignature()
    {
        var code = """
            import System;
            func main(){
                Console.W|
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var completions = GetCompletions(tree, semanticModel, cursor).Where(x => x.DisplayText.Contains("W")).ToImmutableArray();
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
        var code = """
            func main(){
                var x = something(|)
            }

            func something(x: string): int32 = 5;
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

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
        var code = """
            import System;
            func main(){
                Console.Write(|)
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

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
        var code = """
            import System.Text;
            func main(){
                var builder = StringBuilder();
                builder.Append(|);
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

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

    [Fact]
    public void TestSignatureHelpTypeConstructor()
    {
        var code = """
            import System.Text;
            func main(){
                var builder = StringBuilder(|);
            }
            """;
        var source = SourceText.FromText(code);
        var tree = SyntaxTree.Parse(source);
        var cursor = source.IndexToSyntaxPosition(code.IndexOf('|'));

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(tree);
        var service = new SignatureService();
        var signatures = service.GetSignature(tree, semanticModel, cursor);
        Assert.NotNull(signatures);
        Assert.Equal(6, signatures.Overloads.Length);
    }
}
