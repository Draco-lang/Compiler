using System;
using Draco.Compiler.Syntax;

namespace Draco.Compiler;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = @"
// Simple hello world
from System.Console import { WriteLine };

func main() {
    """"""
    Hello!
    """"""
    WriteLine(0);
    ""Bye""
}
";
        var srcReader = SourceReader.From(src);
        var lexer = new Lexer(srcReader);
        while (true)
        {
            var token = lexer.Next();
            Console.WriteLine(token);
            if (token.Type == TokenType.EndOfInput) break;
        }
    }
}
