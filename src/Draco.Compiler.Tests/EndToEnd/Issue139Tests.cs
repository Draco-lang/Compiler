using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.EndToEnd;

// https://github.com/Draco-lang/Compiler/issues/139
public sealed class Issue139Tests
{
    // TODO: This needs to be fixed, once we have interpolation
#if false
    [InlineData("""
        func main() {
            var msg = "Hello! \{}";
        }
        """)]
#endif
    [InlineData("""
        func main() {
            var i = i +
        }
        """)]
    [InlineData("""
        func main() {
            return;
        }
        """)]
    [InlineData("""
        func main(): {}
        """)]
    [InlineData("""
        func main() {
            val x = foo("a", "b");
        }
        func foo(x: string, x: string): string = x;
        """)]
    [InlineData("""
        func main() {}
        func foo(): Foo = x;
        """)]
    [InlineData("""
        func main() {}
        var x = 0;
        var x = 0;
        """)]
    [InlineData("""
        func main(a: int32) {}
        """)]
    [InlineData("""
        var b = "2";
        func b(b: string): string { return b; }
        """)]
    [InlineData("""
        func a(a: string): string {}
        """)]
    [InlineData("""
        func main() {
            var 
            foo();
        }
        foo() {}
        """)]
    [InlineData("""
        func main() {
            int32 = 0;
        }
        """)]
    [InlineData("""
        func main(): int32 {
            return int32;
        }
        """)]
    [InlineData("""
        val x = x;
        """)]
    [InlineData("""
        func main() {
            foo(0.1);
        }
        """)]
    [InlineData("""
        func main() {
            1 = 1;
        }
        """)]
    [InlineData("""
        func foo(): int32 {
            if ({ return 4 }) {
                if (true) {
                    return 0;
                }
                else {
                    return 1;
                }
            }
        }
        """)]
    [InlineData("""
        func foo() {
            if (false) {
            lbl:
                return;
            }
            while (false) {
                goto lbl;
            }
        }
        """)]
    [InlineData(""""
        func foo() {
            bar("""Hello""");
            bar("""
            Hello""");
            bar("""Hello
            """);
            bar("""
        Hello
            """);
            bar("""
            Hello
            """);
        }
        func bar(s: string) {}
        """")]
    [InlineData(""""
        func main() {
            ;
        }
        """")]
    [InlineData(""""
        func main()
        }
        """")]
    [Theory]
    public void DoesNotCrash(string source)
    {
        var syntaxTree = SyntaxTree.Parse(source);
        var compilation = Compilation.Create(ImmutableArray.Create(syntaxTree));
        _ = compilation.Diagnostics.ToList();
        compilation.Emit(new MemoryStream());
    }
}
