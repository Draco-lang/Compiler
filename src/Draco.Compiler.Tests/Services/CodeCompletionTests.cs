using System.Collections.Immutable;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Services;

public sealed class CodeCompletionTests
{
    private static void AssertCompletions(IEnumerable<string> actual, params string[] expected) =>
        AssertCompletions(actual, expected.AsEnumerable());

    private static void AssertCompletions(IEnumerable<string> actual, IEnumerable<string> expected)
    {
        var actualSet = actual.ToHashSet();
        var expectedSet = expected.ToHashSet();
        Assert.True(actualSet.SetEquals(expectedSet));
    }

    private static ImmutableArray<CompletionItem> GetCompletions(string code, char cursor = '|')
    {
        var tree = SyntaxTree.Parse(code);
        var cursorIndex = code.IndexOf(cursor);

        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var completionService = CompletionService.CreateDefault();
        return completionService.GetCompletions(semanticModel, cursorIndex);
    }

    private static ImmutableArray<string> GetCompletionWords(string code, char cursor = '|') =>
        GetCompletions(code, cursor).Select(c => c.Edits[0].Text).ToImmutableArray();

    [Fact]
    public void TestLocalCompletionGlobalVariable()
    {
        // TODO: Can we get rid of all this filtering by filtering in the completion service?
        var completions = GetCompletionWords("""
            val global = 5;
            func main(){
                var local = gl|
            }
            """);

        AssertCompletions(completions, "global", "Single");
    }

    [Fact]
    public void TestLocalCompletionLocalVariable()
    {
        var completions = GetCompletionWords("""
            func main(){
                val local = 5;
                var x = lo|
            }
            """);

        AssertCompletions(completions, "local");
    }

    [Fact]
    public void TestLocalCompletionFunction()
    {
        var completions = GetCompletionWords("""
            func main(){
                var x = so|
            }

            func something(): int32 = 5;
            """);

        AssertCompletions(completions, "something", "Microsoft");
    }

    [Fact]
    public void TestGlobalCompletionGlobalVariable()
    {
        var completions = GetCompletionWords("""
            val global = 5;
            val x = gl|
            """);

        AssertCompletions(completions, "global", "Single");
    }

    [Fact]
    public void TestGlobalCompletionFunction()
    {
        var completions = GetCompletionWords("""
            var x = so|

            func something(): int32 = 5;
            """);

        AssertCompletions(completions, "something", "Microsoft");
    }

    [Fact]
    public void TestCompletionImportedSystem()
    {
        var completions = GetCompletionWords("""
            import System;
            func main(){
                Consol|
            }
            """);

        AssertCompletions(completions, [
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
            "ConsoleSpecialKey"]);
    }

    [Fact]
    public void TestCompletionFullyQualifiedName()
    {
        var completions = GetCompletionWords("""
            func main(){
                System.Consol|
            }
            """);

        AssertCompletions(completions, [
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
            "ConsoleSpecialKey"]);
    }

    [Fact]
    public void TestCompletionImportMember()
    {
        var completions = GetCompletionWords("""
            import System.Co|
            """);

        AssertCompletions(completions, [
            "CodeDom",
            "Collections",
            "ComponentModel",
            "Configuration",
            "Console",
            "Convert",
            "AppContext",
            "BitConverter"]);
    }

    [Fact]
    public void TestCompletionImportRoot()
    {
        var completions = GetCompletionWords("""
            import S|
            """);

        AssertCompletions(completions, "System");
    }

    [Fact]
    public void TestCompletionModuleMemberAccess()
    {
        var completions = GetCompletionWords("""
            import System;
            func main(){
                Console.Wr|
            }
            """);

        AssertCompletions(completions, "Write", "WriteLine");
    }

    [Fact]
    public void TestCompletionLocalMemberAccess()
    {
        var completions = GetCompletionWords("""
            import System.Text;
            func main(){
                var builder = StringBuilder();
                builder.App|
            }
            """);

        AssertCompletions(completions, [
            "Append",
            "AppendFormat",
            "AppendJoin",
            "AppendLine"]);
    }

    [Fact]
    public void TestCompletionTypeSignature()
    {
        var completions = GetCompletions("""
            import System;
            func main(){
                Console.W|
            }
            """);

        var expected = new Dictionary<string, int>
        {
            { "Write", 17},
            { "WriteLine", 18 },
            { "SetWindowPosition", 1 },
            { "SetWindowSize", 1 },
            { "BufferWidth", 1 },
            { "LargestWindowHeight", 1 },
            { "LargestWindowWidth", 1 },
            { "WindowHeight", 1 },
            { "WindowLeft", 1 },
            { "WindowTop", 1 },
            { "WindowWidth", 1 },
        };
        Assert.Equal(expected.Count, completions.Length);
        foreach (var completion in completions)
        {
            Assert.True(expected.TryGetValue(completion.DisplayText, out var expectedCount));
            var gotCount = completion.Symbol is IFunctionGroupSymbol fg ? fg.Functions.Length : 1;
            Assert.Equal(expectedCount, gotCount);
        }
    }

    [Fact]
    public void TestCompletionTypeMemberAccess()
    {
        var completions = GetCompletionWords("""
            import System;
            func main(){
                String.Empt|
            }
            """);

        AssertCompletions(completions, "Empty", "IsNullOrEmpty");
    }

    [Fact]
    public void TestCompletionGenericTypeMemberAccess()
    {
        var completions = GetCompletionWords("""
            import System;
            func main(){
                Memory<int32>.Empt|
            }
            """);

        AssertCompletions(completions, "Empty");
    }

    [Fact]
    public void TestCompletionMemberLvalue()
    {
        var completions = GetCompletionWords("""
            import System;
            func main(){
                Console.WindowW| = 5;
            }
            """);

        AssertCompletions(completions, "WindowWidth", "LargestWindowWidth");
    }

    [Fact]
    public void CompletionsFromStaticClassDontExploreTheConstructor()
    {
        var completions = GetCompletions("""
            import System.Environment;

            func main() {
                val x = l|
            }
            """);

        var docs = completions
            .Select(d => d.Symbol?.Documentation)
            .ToList();
    }

    [Fact]
    public void PrivateMembersAreNotSuggested()
    {
        var completions = GetCompletionWords("""
            import System;

            func main(){
                Foo.|
            }

            module Foo {
                func bar() {}
                internal func baz() {}
                public func qux() {}
            }
            """);

        AssertCompletions(completions, "baz", "qux");
    }
}
