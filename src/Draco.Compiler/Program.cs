using System;
using System.Collections.Generic;
using Draco.Compiler.Diagnostics;
using Draco.Compiler.Syntax;
using Draco.Compiler.Utilities;

namespace Draco.Compiler;

internal sealed class DiagnosticCollectorVisitor : ParseTreeVisitorBase<Unit>
{
    public ValueArray<Diagnostic> Diagnostics => this.diagnostics.ToValue();

    private readonly ValueArray<Diagnostic>.Builder diagnostics = ValueArray.CreateBuilder<Diagnostic>();

    protected override Unit VisitValueArray(ValueArray<Diagnostic> diags)
    {
        foreach (var item in diags) this.diagnostics.Add(item);
        return default;
    }
}

internal class Program
{
    internal static void Main(string[] args)
    {
        var src = """
func main() {
    print("Hello, \q world!")
    print("Hi")
}
""";
        var srcReader = SourceReader.From(src);
        var lexer = new Lexer(srcReader);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource);
        var cu = parser.ParseCompilationUnit();
        var cuRed = (Syntax.Public.ParseTree.CompilationUnit)Syntax.Public.ParseTree.ToRed(null, cu);
        var collector = new DiagnosticCollectorVisitor();
        collector.Visit(cu);
        var diags = collector.Diagnostics;
        foreach (var diag in diags) Console.WriteLine(diag);
    }
}
