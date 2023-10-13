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

    private Lexer lexer = null!;
    private Parser parser = null!;

    public SyntaxBenchmarks()
        : base("syntax")
    {
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        this.tokens = LexFromBenchmarkParameter(this.Input);
    }

    [IterationSetup(Target = nameof(Lex))]
    public void LexSetup()
    {
        var sourceReader = SourceReader.From(this.Input.Code);
        var syntaxDiagnostics = new SyntaxDiagnosticTable();
        this.lexer = new Lexer(sourceReader, syntaxDiagnostics);
    }

    [IterationSetup(Target = nameof(Parse))]
    public void ParseSetup()
    {
        var tokenSource = TokenSource.From(this.tokens.AsMemory());
        var syntaxDiagnostics = new SyntaxDiagnosticTable();
        this.parser = new Parser(tokenSource, syntaxDiagnostics);
    }

    [IterationSetup(Target = nameof(ParseWithStreamingLexer))]
    public void ParseWithStreamingLexerSetup()
    {
        var syntaxDiagnostics = new SyntaxDiagnosticTable();

        var sourceReader = SourceReader.From(this.Input.Code);
        this.lexer = new Lexer(sourceReader, syntaxDiagnostics);

        var tokenSource = TokenSource.From(this.lexer);
        this.parser = new Parser(tokenSource, syntaxDiagnostics);
    }

    [Benchmark]
    public int Lex()
    {
        var count = 0;
        while (true)
        {
            var token = this.lexer.Lex();
            ++count;
            if (token.Kind == TokenKind.EndOfInput) break;
        }
        return count;
    }

    [Benchmark]
    public object Parse() =>
        this.parser.ParseCompilationUnit();

    [Benchmark]
    public object ParseWithStreamingLexer() =>
        this.parser.ParseCompilationUnit();

    private static SyntaxToken[] LexFromBenchmarkParameter(SourceCodeParameter parameter)
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
