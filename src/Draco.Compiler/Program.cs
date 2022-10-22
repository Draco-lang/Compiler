using System;
using Draco.Compiler.Syntax;

namespace Draco.Compiler;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = """"
func main(x: int32, y: int32) {
    Console.WriteLine("""
    Hello, " \n world! \{3 * 3}
""");
    var z = if(3 == 5) 3 else 5;
    x = if(z == 3) 8;
    if(z = 3){
        y = 5;
    }
}
"""";
        var srcReader = SourceReader.From(src);
        var lexer = new Lexer(srcReader);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource);
        var cu = parser.ParseCompilationUnit();
        var transpiler = new CSharpTranspiler();
        transpiler.VisitNode(cu);
        Console.WriteLine(transpiler.GeneratedCode);
    }
}
