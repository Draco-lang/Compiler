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
    public LexerBenchmarks()
        : base("syntax")
    {
    }

    [Benchmark]
    public void Lex()
    {
        var sourceReader = SourceReader.From(this.Input.Code);
        var syntaxDiagnostics = new SyntaxDiagnosticTable();
        var lexer = new Lexer(sourceReader, syntaxDiagnostics);

        while (true)
        {
            var token = lexer.Lex();
            if (token.Kind == TokenKind.EndOfInput) break;
        }
    }
}
