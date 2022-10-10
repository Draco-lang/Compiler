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

func main(): int32 {
    ""Hello \n \u{abc123}

#""""""
Foo bar\#n
\#{123}
Baz
""""""#
    val x = true;
    while(x){
    }
    return x;

    var a = (x > y == z) != a + 23 * c += 9;
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
