using System;
using Draco.Compiler.Syntax;

namespace Draco.Compiler;

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = @"
func main() {
    WriteLine(""""""
        \{capitalize(bottles(i))} of beer on the wall, \{bottles(i)} of beer.
        Take one down, pass it around, \{bottles(i - 1)} of beer on the wall.
        """""");
}
";
        var srcReader = SourceReader.From(src);
        var lexer = new Lexer(srcReader);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource);
        var cu = parser.ParseCompilationUnit();
        Console.WriteLine(cu.PrettyPrint());
    }
}
