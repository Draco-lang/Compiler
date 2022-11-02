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

    private void T(TokenType type) => this.N<Token>(t => t.Type == type && t.Diagnostics.Length == 0);
    private void T(TokenType type, string value) => this.N<Token>(t => t.Type == type && t.ValueText == value && t.Diagnostics.Length == 0);

    private void MissingT(TokenType type) => this.N<Token>(t => t.Type == type && t.Diagnostics.Length > 0);

    private void MainFunctionPlaceHolder(string inputString, Action predicate)
    {
        this.ParseCompilationUnit($$"""
            func main(){
                {{inputString}}
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
                        {
                            predicate();
                        }
                    }
                }
            }
        }
    }

    private void StringTestsPlaceHolder(string inputString, Action predicate)
    {
        void StringPlaceholder()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<ValueInitializer>();
                    {
                        this.T(TokenType.Assign);
                        this.N<Expr.String>();
                        {
                            predicate();
                        }
                    }
                    this.T(TokenType.Semicolon);
                    this.T(TokenType.CurlyClose);
                }
            }
        }
        this.MainFunctionPlaceHolder($"val x = {inputString};", StringPlaceholder);
    }

    private void StringNewline()
    {
        this.N<StringPart.Content>();
        {
            this.T(TokenType.StringNewline);
        }
    }

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

    [Fact]
    public void TestEmptyFuncWithoutClosingCurly()
    {
        this.ParseCompilationUnit("""
            func main() {
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
                        this.MissingT(TokenType.CurlyClose);
                    }
                }
            }
        }
    }

    [Fact]
    public void TestSimpleString()
    {
        void SimpleString()
        {
            this.T(TokenType.LineStringStart);
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "Hello, World!");
            }
            this.T(TokenType.LineStringEnd);
        }
        this.StringTestsPlaceHolder("""
            "Hello, World!"
            """, SimpleString);
    }

    [Fact]
    public void TestSimpleMultilineString()
    {
        void SimpleString()
        {
            this.T(TokenType.MultiLineStringStart);
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "Hello, World!");
            }
            this.T(TokenType.MultiLineStringEnd);
        }
        string quotes = "\"\"\"";
        this.StringTestsPlaceHolder($"""
            {quotes}
            Hello, World!
            {quotes}
            """, SimpleString);
    }

    [Fact]
    public void TestStringInterpolation()
    {
        void StringInterpolation()
        {
            this.T(TokenType.LineStringStart);
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "Hello, ");
            }
            this.N<StringPart.Interpolation>();
            {
                this.T(TokenType.InterpolationStart);
                this.N<Expr.String>();
                {
                    this.T(TokenType.LineStringStart);
                    this.N<StringPart.Content>();
                    {
                        this.T(TokenType.StringContent, "World");
                    }
                    this.T(TokenType.LineStringEnd);
                }
                this.T(TokenType.InterpolationEnd);
            }
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "!");
            }
            this.T(TokenType.LineStringEnd);
        }
        this.StringTestsPlaceHolder("""
            "Hello, \{"World"}!"
            """, StringInterpolation);
    }

    [Fact]
    public void TestStringEscapes()
    {
        void StringEscapes()
        {
            this.T(TokenType.LineStringStart);
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "Hello, \nWorld! 👽");
            }
            this.T(TokenType.LineStringEnd);
        }
        this.StringTestsPlaceHolder("""
            "Hello, \nWorld! \u{1F47D}"
            """, StringEscapes);
    }

    [Fact]
    public void TestMultilineStringContinuations()
    {
        void StringContinuations()
        {
            this.T(TokenType.MultiLineStringStart);
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "Hello, ");
            }
            this.StringNewline();
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "World!");
            }
            this.T(TokenType.MultiLineStringEnd);
        }
        var quotes = "\"\"\"";
        this.StringTestsPlaceHolder($"""
            {quotes}
            Hello, \    
            World!
            {quotes}
            """, StringContinuations);
    }

    [Fact]
    public void TestLineStringUnclosed()
    {
        void UnclosedString()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<ValueInitializer>();
                    {
                        this.T(TokenType.Assign);
                        this.N<Expr.String>();
                        {
                            this.T(TokenType.LineStringStart);
                            this.N<StringPart.Content>();
                            {
                                this.T(TokenType.StringContent, "Hello, World!;");
                            }
                            this.MissingT(TokenType.LineStringEnd);
                        }
                    }
                    this.MissingT(TokenType.Semicolon);
                }
            }
            this.T(TokenType.CurlyClose);
        }

        // There is semicolon at the end, because the string is put into declaration
        this.MainFunctionPlaceHolder("""
            val x = "Hello, World!;
            """, UnclosedString);
    }

    [Fact]
    public void TestMultilineStringUnclosedInterpolation()
    {
        void UnclosedString()
        {
            this.T(TokenType.MultiLineStringStart);
            this.N<StringPart.Content>();
            {
                this.T(TokenType.StringContent, "Hello, ");
            }
            this.StringNewline();
            this.N<StringPart.Interpolation>();
            {
                this.T(TokenType.InterpolationStart);
                this.N<Expr.String>();
                {
                    this.T(TokenType.LineStringStart);
                    this.N<StringPart.Content>();
                    {
                        this.T(TokenType.StringContent, "World!");
                    }
                    this.T(TokenType.LineStringEnd);
                }
            }
            //this.StringNewline();
            this.MissingT(TokenType.InterpolationEnd);
            this.T(TokenType.MultiLineStringEnd);
        }
        var quotes = "\"\"\"";
        this.StringTestsPlaceHolder($$"""
            {{quotes}}
            Hello, 
            \{"World!"
            {{quotes}}
            """, UnclosedString);
    }

    [Fact]
    public void TestDeclaration()
    {
        void Declaration()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<ValueInitializer>();
                    {
                        this.T(TokenType.Assign);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                    this.T(TokenType.Semicolon);
                }
            }
        }
        this.MainFunctionPlaceHolder("val x = 5;", Declaration);
    }

    [Fact]
    public void TestDeclarationMissingSemicolon()
    {
        void Declaration()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<ValueInitializer>();
                    {
                        this.T(TokenType.Assign);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                    this.MissingT(TokenType.Semicolon);
                }
            }
        }
        this.MainFunctionPlaceHolder("val x = 5", Declaration);
    }

    [Fact]
    public void TestIfElseStatements()
    {
        void IfElse()
        {
            this.N<Expr.If>();
            {
                this.T(TokenType.KeywordIf);
                this.T(TokenType.ParenOpen);
                this.N<Expr.Relational>();
                {
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                    this.N<Expr.ComparisonElement>();
                    {
                        this.T(TokenType.GreaterThan);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                }
                this.T(TokenType.ParenClose);
                this.N<Expr.UnitStmt>();
                this.N<Stmt.Expr>();
                this.N<Expr.Block>();
                this.T(TokenType.CurlyOpen);
                this.N<BlockContents>();
                {
                    this.N<Stmt.Decl>();
                    {
                        this.N<Decl.Variable>();
                        {
                            this.T(TokenType.KeywordVal);
                            this.T(TokenType.Identifier);
                            this.N<ValueInitializer>();
                            {
                                this.T(TokenType.Assign);
                                this.N<Expr.Literal>();
                                {
                                    this.T(TokenType.LiteralInteger);
                                }
                            }
                            this.T(TokenType.Semicolon);
                        }
                    }
                }
                this.T(TokenType.CurlyClose);
                this.N<Expr.ElseClause>();
                {
                    this.T(TokenType.KeywordElse);
                    this.N<Expr.UnitStmt>();
                    this.N<Stmt.Expr>();
                    this.N<Expr.Block>();
                    this.T(TokenType.CurlyOpen);
                    this.N<BlockContents>();
                    {
                        this.N<Stmt.Decl>();
                        {
                            this.N<Decl.Variable>();
                            {
                                this.T(TokenType.KeywordVal);
                                this.T(TokenType.Identifier);
                                this.N<ValueInitializer>();
                                {
                                    this.T(TokenType.Assign);
                                    this.N<Expr.Literal>();
                                    {
                                        this.T(TokenType.LiteralCharacter);
                                    }
                                }
                                this.T(TokenType.Semicolon);
                            }
                        }
                    }
                    this.T(TokenType.CurlyClose);
                }
            }
        }
        this.MainFunctionPlaceHolder("""
            if(5 > 0){
                val x = 5;
            }
            else{
                val y = 'c';
            }
            """, IfElse);
    }

    [Fact]
    public void TestElseStatementsMissingIf()
    {
        void OnlyElse()
        {
            this.N<Stmt.Unexpected>();
            {
                this.T(TokenType.KeywordElse);
                this.N<Expr.Block>();
                this.T(TokenType.CurlyOpen);
                this.N<BlockContents>();
                {
                    this.N<Stmt.Decl>();
                    {
                        this.N<Decl.Variable>();
                        {
                            this.T(TokenType.KeywordVal);
                            this.T(TokenType.Identifier);
                            this.N<ValueInitializer>();
                            {
                                this.T(TokenType.Assign);
                                this.N<Expr.Literal>();
                                {
                                    this.T(TokenType.LiteralInteger);
                                }
                            }
                            this.T(TokenType.Semicolon);
                        }
                    }
                }
                this.T(TokenType.CurlyClose);
            }
        }
        this.MainFunctionPlaceHolder("""
            else{
                val y = 8;
            }
            """, OnlyElse);
    }
}
