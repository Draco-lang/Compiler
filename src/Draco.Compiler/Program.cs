using System;
using Draco.Compiler.Syntax;

namespace Draco.Compiler;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = @"
// Simple hello world

var x;
val y: int32;
func main() {

}
";
        var srcReader = SourceReader.From(src);
        var lexer = new Lexer(srcReader);
        var tokenSource = TokenSource.From(lexer);
        Parser parser = new Parser(tokenSource);
        parser.ParseCompilationUnit();
        //while (true)
        //{
        //    var token = lexer.Lex();
        //    Console.WriteLine(token);
        //    foreach (var d in token.Diagnostics) Console.WriteLine(d);
        //    if (token.Type == TokenType.EndOfInput) break;
        //}
    }
}
