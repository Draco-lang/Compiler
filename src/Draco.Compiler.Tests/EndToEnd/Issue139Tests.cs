using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.EndToEnd;

// https://github.com/Draco-lang/Compiler/issues/139
public sealed class Issue139Tests
{
    [InlineData("""
        func main() {
            var msg = "Hello! \{}";
        }
        """)]
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
    [InlineData(""""
        func main(){
            var x;
            x = x();
        }
        """")]
    [InlineData(""""
        func main() {
            func 
        }
        """")]
    [InlineData(""""
         func main() {
             println[]
         }
         """")]
    [InlineData(""""
        func main() {
            println("'att't"'"t''ork;");
        }
        """")]
    [InlineData(""""
        func main() {
            println("'att't"'"'ork;");
        }
        """")]
    [InlineData(""""
        8'\
        """")]
    [InlineData(""""
        w08'\
        """")]
    [InlineData(""""
        6w08'\
        """")]
    [InlineData(""""
        func main() {
            System
        }
        """")]
    [InlineData(""""
        func main(){
            while(x < s)
            func something();
        """")]
    [InlineData(""""
        func foo(): System = 5;
        """")]
    [InlineData(""""
        func main(){
            foo
        }
        func foo() {}
        """")]
    [InlineData(""""
        import System.Collections.Immutable;
        func main(){
            ImmutableArray<int32>.Empt|
        }
        """")]
    [InlineData(""""
        import System.Collections.Immutable;
        func foo(): ImmutableArray { }
        """")]
    [InlineData("public import Foo")]
    [InlineData("label:")]
    [Theory]
    public void DoesNotCrash(string source)
    {
        var syntaxTree = SyntaxTree.Parse(source);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        _ = compilation.Diagnostics.ToList();
        compilation.Emit(peStream: new MemoryStream());
    }
}
