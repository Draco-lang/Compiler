using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Syntax;
using static Draco.Compiler.Internal.Syntax.ParseTree;

namespace Draco.Compiler.Tests.Syntax;

public sealed class ParserTests
{
    private static T ParseInto<T>(string text, Func<Parser, T> func)
    {
        var srcReader = SourceReader.From(text);
        var lexer = new Lexer(srcReader);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource);
        return func(parser);
    }

    private IEnumerator<ParseTree>? treeEnumerator;

    private void ParseCompilationUnit(string text)
    {
        this.treeEnumerator = ParseInto(text, p => p.ParseCompilationUnit())
            .InOrderTraverse()
            .GetEnumerator();
    }

    private void N<T>(Predicate<T> predicate)
    {
        Assert.NotNull(this.treeEnumerator);
        Assert.True(this.treeEnumerator!.MoveNext());
        var node = this.treeEnumerator.Current;
        Assert.IsType<T>(node);
        Assert.True(predicate((T)(object)node));
    }

    private void N<T>() => this.N<T>(_ => true);

    private void T(TokenType type) => this.N<Token>(t => t.Type == type);

    [Fact]
    public void TestEmpty()
    {
        this.ParseCompilationUnit(string.Empty);

        this.N<CompilationUnit>();
        {
            this.T(TokenType.EndOfInput);
        }
    }

    [Fact]
    public void TestEmptyFunc()
    {
        this.ParseCompilationUnit("""
            func main() {
            }
            """);

        this.N<CompilationUnit>();
        {
            this.N<Decl.Func>();
            {
                this.T(TokenType.KeywordFunc);
                this.T(TokenType.Identifier);

                this.T(TokenType.ParenOpen);
                this.T(TokenType.ParenClose);

                this.N<FuncBody.BlockBody>();
                {
                    this.N<Expr.Block>();
                    {
                        this.T(TokenType.CurlyOpen);
                        this.N<BlockContents>();
                        this.T(TokenType.CurlyClose);
                    }
                }
            }
        }
    }
}
