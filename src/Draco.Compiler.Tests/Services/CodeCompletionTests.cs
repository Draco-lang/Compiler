using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Services;

public sealed class CodeCompletionTests
{
    private static void AssertCompletions(IEnumerable<TextEdit> actual, params string[] expected) =>
        AssertCompletions(actual, expected.AsEnumerable());

    private static void AssertCompletions(IEnumerable<TextEdit> actual, IEnumerable<string> expected)
    {
        var actualSet = actual.Select(x => x.Text).ToHashSet();
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

    [Fact]
    public void TestLocalCompletionGlobalVariable()
    {
        // TODO: Can we get rid of all this filtering by filtering in the completion service?
        var completions = GetCompletions("""
            val global = 5;
            func main(){
                var local = gl|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("gl")));

        AssertCompletions(completions, "global");
    }

    [Fact]
    public void TestLocalCompletionLocalVariable()
    {
        var completions = GetCompletions("""
            func main(){
                val local = 5;
                var x = lo|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("lo")));

        AssertCompletions(completions, "local");
    }

    [Fact]
    public void TestLocalCompletionFunction()
    {
        var completions = GetCompletions("""
            func main(){
                var x = so|
            }

            func something(): int32 = 5;
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("so")));

        AssertCompletions(completions, "something");
    }

    [Fact]
    public void TestGlobalCompletionGlobalVariable()
    {
        var completions = GetCompletions("""
            val global = 5;
            val x = gl|
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("gl")));

        AssertCompletions(completions, "global");
    }

    [Fact]
    public void TestGlobalCompletionFunction()
    {
        var completions = GetCompletions("""
            var x = so|

            func something(): int32 = 5;
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("so")));

        AssertCompletions(completions, "something");
    }

    [Fact]
    public void TestCompletionImportedSystem()
    {
        var completions = GetCompletions("""
            import System;
            func main(){
                Consol|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Consol")));

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
        var completions = GetCompletions("""
            func main(){
                System.Consol|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Consol")));

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
        var completions = GetCompletions("""
            import System.Co|
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Co")));

        AssertCompletions(completions, [
            "CodeDom",
            "Collections",
            "ComponentModel",
            "Configuration",
            "Console",
            "Convert"]);
    }

    [Fact]
    public void TestCompletionImportRoot()
    {
        var completions = GetCompletions("""
            import S|
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith('S')));

        AssertCompletions(completions, "System");
    }

    [Fact]
    public void TestCompletionModuleMemberAccess()
    {
        var completions = GetCompletions("""
            import System;
            func main(){
                Console.Wr|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Wr")));

        AssertCompletions(completions, "Write", "WriteLine");
    }

    [Fact]
    public void TestCompletionLocalMemberAccess()
    {
        var completions = GetCompletions("""
            import System.Text;
            func main(){
                var builder = StringBuilder();
                builder.App|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("App")));

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
            """).Where(x => x.DisplayText.Contains('W')).ToImmutableArray();

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
            Assert.True(expected.TryGetValue(completion.DisplayText, out var type));
            Assert.Equal(type, completion.Symbols.Length);
        }
    }

    [Fact]
    public void TestCompletionTypeMemberAccess()
    {
        var completions = GetCompletions("""
            import System;
            func main(){
                String.Empt|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Empt", StringComparison.Ordinal)));

        AssertCompletions(completions, "Empty");
    }

    [Fact]
    public void TestCompletionGenericTypeMemberAccess()
    {
        var completions = GetCompletions("""
            import System;
            func main(){
                Memory<int32>.Empt|
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.StartsWith("Empt", StringComparison.Ordinal)));

        AssertCompletions(completions, "Empty");
    }

    [Fact]
    public void TestCompletionMemberLvalue()
    {
        var completions = GetCompletions("""
            import System;
            func main(){
                Console.WindowW| = 5;
            }
            """).SelectMany(x => x.Edits.Where(y => y.Text.Contains("WindowW")));

        AssertCompletions(completions, "WindowWidth", "LargestWindowWidth");
    }
}
