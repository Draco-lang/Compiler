using System;
using Draco.Compiler.Syntax;

namespace Draco.Compiler;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = @"
var x = y + z * 4 + foo(1, 2) - bar[1];
";
        var srcReader = SourceReader.From(src);
        var lexer = new Lexer(srcReader);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource);
        var cu = parser.ParseCompilationUnit();
        Console.WriteLine(cu.PrettyPrint());
    }
}
