using Draco.Compiler.Internal.Declarations;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.EndToEnd;

public sealed class DoesNotCrashTests
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
    [InlineData("""
        func foo(): string = "\{0}";
        """)]
    [InlineData("""
        import System.Console;

        )main(func  {
            WriteLine("   ").Console;
            );}
        """)]
    [InlineData("""
        func foo() {
            val a  = 1e;
        }
        """)]
    [InlineData("""
        import System.Console;
        import System.Linq;

        func fib(n:int32): int32 = {
                for (i in Range(0 if (n < .) 1 else fib(n - 1) + fib(n - 2)
                ;func main() ,fib(i)
            }

        )WriteLine("   fib(  i}  ) =   fibi});}
        """)]
    [InlineData("""
        import System.Console;

        func main() {
            System.Console();
        }
        """)]
    [InlineData("""
        import System.Collections.Generic;

        func f() = List();
        """)]
    [InlineData("""
        import System.Console;
        import System.Linq.Enumerable;

        val width = 80;
        val height = 40;

        func count_neighbors(x:int32, y:int32, map:Array2D<bool>): int32 {
            return 0;
        }

        func tick(front:Array2D<bool>, back:Array2D<bool>) {
            for (y in Range(0, height)) {
                for (x in Range(0, width)) {
                    val neighbors = count_neighbors(x, y);
                    back[x,y] = if (front[x,y]) 2 <= neighbors else if (neighbors == 3) true else false;
                }
            }
        }
        """)]
    [InlineData("""
        import System.Console;

        func arrayOf<T>(...vs:Array<T {
            WriteLine(s);
            func arrayOf<T>(...vs:Array<T>): Array<T> 
            func main() {
                for (s in arrayOf(" ", "   " ;
                func arrayOf<T>(...vs:Array,  " "))>=
            }
        }
        """)]
    [InlineData("""
        import System.Console;

        func arrayOf<T>(,  " ")){WriteLine(vs;func main() {
            for (s in arrayOf(" ", "   def   ", ... vs
            :Array<T>
            ):Array<T> = for (s in arrayOf( 
            func arrayOf<T>(...vs:Array<T>): Array<T> = for (s in arrayOf(" ", " " " "

            )){
                WriteLine(s);
            }
        }
        """)]
    [InlineData("""
        func main() {
            var front = Array2D(0, 0);
            var back = Array2D(0, 0);
            front[3,5] = back;
            val temp = front;
            back = temp;
            back = temp;
        }
        """)]
    [Theory]
    public void DoesNotCrash(string source)
    {
        var peStream = new MemoryStream();
        var compilation = CreateCompilation(source);
        _ = compilation.Emit(peStream);
    }

    [Fact]
    public void EmptyDeclarationTableDoesNotCrash()
    {
        _ = DeclarationTable.Empty;
    }
}
