using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal;

internal sealed class DiagnosticCollectorVisitor : ParseTreeVisitorBase<Unit>
{
    public ImmutableArray<Diagnostic> Diagnostics => this.diagnostics.ToImmutable();

    private readonly ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    protected override Unit VisitImmutableArray(ImmutableArray<Diagnostic> diags)
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
        var collector = new DiagnosticCollectorVisitor();
        collector.Visit(cu);
        var diags = collector.Diagnostics;
        foreach (var diag in diags) Console.WriteLine(diag);
    }
}
