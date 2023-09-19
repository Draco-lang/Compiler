using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Benchmarks;

public class LexerBenchmarks : FolderBenchmarkBase
{
    private Lexer lexer = null!;

    public LexerBenchmarks()
        : base("lexer")
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        var sourceReader = SourceReader.From(this.Input.Code);
        var syntaxDiagnostics = new SyntaxDiagnosticTable();
        this.lexer = new Lexer(sourceReader, syntaxDiagnostics);
    }

    [Benchmark]
    public void Lex()
    {
        while (true)
        {
            var token = this.lexer.Lex();
            if (token.Kind == TokenKind.EndOfInput) break;
        }
    }
}
