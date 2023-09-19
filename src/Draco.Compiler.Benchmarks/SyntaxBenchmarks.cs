using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax;
using SyntaxToken = Draco.Compiler.Internal.Syntax.SyntaxToken;

namespace Draco.Compiler.Benchmarks;

public class SyntaxBenchmarks : FolderBenchmarkBase
{
    private SyntaxToken[] tokens = null!;

    public SyntaxBenchmarks()
        : base("syntax")
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.tokens = Lex(this.Input);
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

    [Benchmark]
    public void Parse()
    {
        var tokenSource = TokenSource.From(this.tokens.AsMemory());
        var syntaxDiagnostics = new SyntaxDiagnosticTable();
        var parser = new Parser(tokenSource, syntaxDiagnostics);

        _ = parser.ParseCompilationUnit();
    }

    [Benchmark]
    public void ParseWithStreamingLexer()
    {
        var syntaxDiagnostics = new SyntaxDiagnosticTable();

        var sourceReader = SourceReader.From(this.Input.Code);
        var lexer = new Lexer(sourceReader, syntaxDiagnostics);

        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource, syntaxDiagnostics);

        _ = parser.ParseCompilationUnit();
    }

    private static SyntaxToken[] Lex(SourceCodeParameter parameter)
    {
        var sourceReader = SourceReader.From(parameter.Code);
        var syntaxDiagnostics = new SyntaxDiagnosticTable();
        var lexer = new Lexer(sourceReader, syntaxDiagnostics);

        var result = new List<SyntaxToken>();

        while (true)
        {
            var token = lexer.Lex();
            result.Add(token);

            if (token.Kind == TokenKind.EndOfInput) break;
        }

        return result.ToArray();
    }
}
