using System;
using Draco.Compiler.Syntax;

namespace Draco.Compiler;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = @"
func main(x: int32, y: int32) {
    val input = ReadLine();
    if (input == 0) {
        Write(0);
    }
    else {
        // We just assume 1 for anything non-0
        while (true) Write(1);
    }
}
";
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
