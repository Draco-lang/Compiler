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
                        this.T(TokenType.CurlyClose);
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
                }
            }
        }
        this.MainFunctionPlaceHolder($"val x = {inputString};", StringPlaceholder);
    }

    private void VariableDeclarationPlaceHolder(string input, Action predicate)
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
                        predicate();
                    }
                    this.T(TokenType.Semicolon);
                }
            }
        }
        this.MainFunctionPlaceHolder($"val x = {input};", StringPlaceholder);
    }

    private void StringNewline()
    {
        this.N<StringPart.Content>();
        {
            this.T(TokenType.StringNewline);
        }
    }

    private void StringContent()
    {
        this.N<StringPart.Content>();
        {
            this.T(TokenType.StringContent);
        }
    }

    private void StringContent(string content)
    {
        this.N<StringPart.Content>();
        {
            this.T(TokenType.StringContent, content);
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
    public void TestLineString()
    {
        void SimpleString()
        {
            this.T(TokenType.LineStringStart);
            this.StringContent("Hello, World!");
            this.T(TokenType.LineStringEnd);
        }
        this.StringTestsPlaceHolder("""
            "Hello, World!"
            """, SimpleString);
    }

    [Fact]
    public void TestMultilineString()
    {
        void SimpleString()
        {
            this.T(TokenType.MultiLineStringStart);
            this.StringContent("Hello, World!");
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
    public void TestLineStringInterpolation()
    {
        void StringInterpolation()
        {
            this.T(TokenType.LineStringStart);
            this.StringContent("Hello, ");
            this.N<StringPart.Interpolation>();
            {
                this.T(TokenType.InterpolationStart);
                this.N<Expr.String>();
                {
                    this.T(TokenType.LineStringStart);
                    this.StringContent("World");
                    this.T(TokenType.LineStringEnd);
                }
                this.T(TokenType.InterpolationEnd);
            }
            this.StringContent("!");
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
            this.StringContent("Hello, \nWorld! 👽");
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
            this.StringContent("Hello, ");
            this.StringNewline();
            this.StringContent("World!");
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
                            this.StringContent("Hello, World!;");
                            this.MissingT(TokenType.LineStringEnd);
                        }
                    }
                    this.MissingT(TokenType.Semicolon);
                }
            }
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
            this.StringContent("Hello, ");
            this.StringNewline();
            this.N<StringPart.Interpolation>();
            {
                this.T(TokenType.InterpolationStart);
                this.N<Expr.String>();
                {
                    this.T(TokenType.LineStringStart);
                    this.StringContent("World!");
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
    public void TestVariableDeclarationWithNoTypeAndWithValueAndMissingSemicolon()
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
    public void TestVariableDeclarationWithNoTypeAndWithNoValue()
    {
        void Declaration()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.T(TokenType.Semicolon);
                }
            }
        }
        this.MainFunctionPlaceHolder("val x;", Declaration);
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithValue()
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
    public void TestVariableDeclarationWithNoTypeAndWithValueAndMissingValue()
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
                        this.N<Expr.Unexpected>();
                        this.T(TokenType.Semicolon);
                    }
                }
            }
        }
        this.MainFunctionPlaceHolder("val x =;", Declaration);
    }

    [Fact]
    public void TestVariableDeclarationWithNoValueAndWithType()
    {
        void Declaration()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<TypeSpecifier>();
                    {
                        this.T(TokenType.Colon);
                        this.N<TypeExpr.Name>();
                        {
                            this.T(TokenType.Identifier);
                        }
                    }
                    this.T(TokenType.Semicolon);
                }
            }
        }
        this.MainFunctionPlaceHolder("val x: int32;", Declaration);
    }

    [Fact]
    public void TestVariableDeclarationWithNoValueAndWithTypeAndMissingType()
    {
        void Declaration()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<TypeSpecifier>();
                    {
                        this.T(TokenType.Colon);
                        this.N<TypeExpr.Name>();
                        {
                            this.MissingT(TokenType.Identifier);
                        }
                    }
                    this.T(TokenType.Semicolon);
                }
            }
        }
        this.MainFunctionPlaceHolder("val x:;", Declaration);
    }

    [Fact]
    public void TestVariableDeclarationWithValueAndWithType()
    {
        void Declaration()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<TypeSpecifier>();
                    {
                        this.T(TokenType.Colon);
                        this.N<TypeExpr.Name>();
                        {
                            this.T(TokenType.Identifier);
                        }
                        this.N<ValueInitializer>();
                        {
                            this.T(TokenType.Assign);
                            this.N<Expr.Literal>();
                            {
                                this.T(TokenType.LiteralInteger);
                            }
                        }
                    }
                    this.T(TokenType.Semicolon);
                }
            }
        }
        this.MainFunctionPlaceHolder("val x: int32 = 5;", Declaration);
    }

    [Fact]
    public void TestVariableDeclarationWithValueAndWithTypeAndMissingValueAndMissingType()
    {
        void Declaration()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<TypeSpecifier>();
                    {
                        this.T(TokenType.Colon);
                        this.N<TypeExpr.Name>();
                        {
                            this.MissingT(TokenType.Identifier);
                        }
                        this.N<ValueInitializer>();
                        {
                            this.T(TokenType.Assign);
                            this.N<Expr.Unexpected>();
                            this.T(TokenType.Semicolon);
                        }
                    }
                }
            }
        }
        this.MainFunctionPlaceHolder("val x: =;", Declaration);
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
            if (5 > 0){
                val x = 5;
            }
            else {
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
            }
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
        this.MainFunctionPlaceHolder("""
            else {
                val y = 8;
            }
            """, OnlyElse);
    }

    [Fact]
    public void TestIfStatementMissingClosingParen()
    {
        void IfMissingParan()
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
                this.MissingT(TokenType.ParenClose);
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
            }
        }
        this.MainFunctionPlaceHolder("""
            if (5 > 0 {
                val x = 5;
            }
            """, IfMissingParan);
    }

    [Fact]
    public void TestIfStatementMissingContents()
    {
        void IfMissingStatement()
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
                this.N<Expr.Unexpected>();
            }
        }
        this.MainFunctionPlaceHolder("""
            if (5 > 0)
            """, IfMissingStatement);
    }

    [Fact]
    public void TestIfElseExpression()
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
                this.N<Expr.Literal>();
                {
                    this.T(TokenType.LiteralInteger);
                }
                this.N<ElseClause>();
                {
                    this.T(TokenType.KeywordElse);
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                }
            }
        }
        this.VariableDeclarationPlaceHolder("""
            if (5 > 0) 3 else 9
            """, IfElse);
    }

    [Fact]
    public void TestIfExpressionNoElse()
    {
        void IfExpression()
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
                this.N<Expr.Literal>();
                {
                    this.T(TokenType.LiteralInteger);
                }
            }
        }
        this.VariableDeclarationPlaceHolder("""
            if (5 > 0) 3
            """, IfExpression);
    }

    [Fact]
    public void TestWhileStatement()
    {
        void WhileStatement()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVar);
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
            this.N<Expr.While>();
            {
                this.T(TokenType.KeywordWhile);
                this.T(TokenType.ParenOpen);
                this.N<Expr.Relational>();
                {
                    this.N<Expr.Name>();
                    {
                        this.T(TokenType.Identifier);
                    }
                    this.N<Expr.ComparisonElement>();
                    {
                        this.T(TokenType.LessThan);
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
                    this.N<Stmt.Expr>();
                    {
                        this.N<Expr.Binary>();
                        {
                            this.N<Expr.Name>();
                            {
                                this.T(TokenType.Identifier);
                            }
                            this.T(TokenType.Assign);
                            this.N<Expr.Binary>();
                            {
                                this.N<Expr.Name>();
                                {
                                    this.T(TokenType.Identifier);
                                }
                                this.T(TokenType.Plus);
                                this.N<Expr.Literal>();
                                {
                                    this.T(TokenType.LiteralInteger);
                                }
                            }
                            this.T(TokenType.Semicolon);
                        }
                    }
                }
            }
            this.T(TokenType.CurlyClose);
        }
        this.MainFunctionPlaceHolder("""
            var x = 0;
            while (x < 5) {
                x = x + 1;
            }
            """, WhileStatement);
    }

    [Fact]
    public void TestWhileStatementMissingClosingParen()
    {
        void WhileStatement()
        {
            this.N<Stmt.Decl>();
            {
                this.N<Decl.Variable>();
                {
                    this.T(TokenType.KeywordVar);
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
            this.N<Expr.While>();
            {
                this.T(TokenType.KeywordWhile);
                this.T(TokenType.ParenOpen);
                this.N<Expr.Relational>();
                {
                    this.N<Expr.Name>();
                    {
                        this.T(TokenType.Identifier);
                    }
                    this.N<Expr.ComparisonElement>();
                    {
                        this.T(TokenType.LessThan);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                }
                this.MissingT(TokenType.ParenClose);
                this.N<Expr.UnitStmt>();
                this.N<Stmt.Expr>();
                this.N<Expr.Block>();
                this.T(TokenType.CurlyOpen);
                this.N<BlockContents>();
                {
                    this.N<Stmt.Expr>();
                    {
                        this.N<Expr.Binary>();
                        {
                            this.N<Expr.Name>();
                            {
                                this.T(TokenType.Identifier);
                            }
                            this.T(TokenType.Assign);
                            this.N<Expr.Binary>();
                            {
                                this.N<Expr.Name>();
                                {
                                    this.T(TokenType.Identifier);
                                }
                                this.T(TokenType.Plus);
                                this.N<Expr.Literal>();
                                {
                                    this.T(TokenType.LiteralInteger);
                                }
                            }
                            this.T(TokenType.Semicolon);
                        }
                    }
                }
            }
            this.T(TokenType.CurlyClose);
        }
        this.MainFunctionPlaceHolder("""
            var x = 0;
            while (x < 5 {
                x = x + 1;
            }
            """, WhileStatement);
    }

    [Fact]
    public void TestWhileStatementMissingContents()
    {
        void WhileStatement()
        {
            this.N<Expr.While>();
            {
                this.T(TokenType.KeywordWhile);
                this.T(TokenType.ParenOpen);
                this.N<Expr.Relational>();
                {
                    this.N<Expr.Name>();
                    {
                        this.T(TokenType.Identifier);
                    }
                    this.N<Expr.ComparisonElement>();
                    {
                        this.T(TokenType.LessThan);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                }
                this.T(TokenType.ParenClose);
                this.N<Expr.UnitStmt>();
                this.N<Stmt.Expr>();
                this.N<Expr.Unexpected>();
            }
        }
        this.MainFunctionPlaceHolder("""
            while (x < 5)
            """, WhileStatement);
    }

    [Fact]
    public void TestLabelDeclaration()
    {
        void LabelDeclaration()
        {
            this.N<Stmt.Decl>();
            this.N<Decl.Label>();
            {
                this.T(TokenType.Identifier, "myLabel");
                this.T(TokenType.Colon);
            }
        }
        this.MainFunctionPlaceHolder("""
            myLabel:
            """, LabelDeclaration);
    }

    [Fact]
    public void TestLabelDeclarationNewlineBeforeColon()
    {
        void LabelDeclaration()
        {
            this.N<Stmt.Decl>();
            this.N<Decl.Label>();
            {
                this.T(TokenType.Identifier, "myLabel");
                this.T(TokenType.Colon);
            }
        }
        this.MainFunctionPlaceHolder("""
            myLabel
            :
            """, LabelDeclaration);
    }

    [Fact]
    public void TestGoto()
    {
        void GotoStatement()
        {
            this.N<Stmt.Decl>();
            this.N<Decl.Label>();
            {
                this.T(TokenType.Identifier, "myLabel");
                this.T(TokenType.Colon);
            }
            this.N<Stmt.Expr>();
            this.N<Expr.Goto>();
            {
                this.T(TokenType.KeywordGoto);
                this.N<Expr.Name>();
                {
                    this.T(TokenType.Identifier);
                }
            }
            this.T(TokenType.Semicolon);
        }
        this.MainFunctionPlaceHolder("""
            myLabel:
                goto myLabel;
            """, GotoStatement);
    }

    [Fact]
    public void TestGotoNoLabelSpecified()
    {
        void GotoStatement()
        {
            this.N<Stmt.Expr>();
            this.N<Expr.Goto>();
            {
                this.T(TokenType.KeywordGoto);
                this.N<Expr.Unexpected>();
            }
            this.T(TokenType.Semicolon);
        }
        this.MainFunctionPlaceHolder("""
                goto;
            """, GotoStatement);
    }

    [Fact]
    public void TestReturnWithValue()
    {
        void ReturnStatement()
        {
            this.N<Stmt.Expr>();
            this.N<Expr.Return>();
            {
                this.T(TokenType.KeywordReturn);
                this.N<Expr.Binary>();
                {
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                    this.T(TokenType.Plus);
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                }
            }
            this.T(TokenType.Semicolon);
        }
        this.MainFunctionPlaceHolder("""
                return 2 + 1;
            """, ReturnStatement);
    }

    [Fact]
    public void TestReturnWithoutValue()
    {
        void ReturnStatement()
        {
            this.N<Stmt.Expr>();
            this.N<Expr.Return>();
            {
                this.T(TokenType.KeywordReturn);
            }
            this.T(TokenType.Semicolon);
        }
        this.MainFunctionPlaceHolder("""
                return;
            """, ReturnStatement);
    }

    [Fact]
    public void TestBlockExpressionWithValue()
    {
        void BlockStatement()
        {
            this.N<Expr.Block>();
            this.T(TokenType.CurlyOpen);
            this.N<Expr.BlockContents>();
            {
                this.N<Stmt.Decl>();
                {
                    this.N<Decl.Variable>();
                    {
                        this.T(TokenType.KeywordVar);
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
                this.N<Expr.Name>();
                {
                    this.T(TokenType.Identifier);
                }
            }
            this.T(TokenType.CurlyClose);
        }
        this.MainFunctionPlaceHolder("""
            {
                var x = 5;
                x
            }
            """, BlockStatement);
    }

    [Fact]
    public void TestBlockExpressionWithoutValue()
    {
        void BlockStatement()
        {
            this.N<Expr.Block>();
            this.T(TokenType.CurlyOpen);
            this.N<Expr.BlockContents>();
            {
                this.N<Stmt.Decl>();
                {
                    this.N<Decl.Variable>();
                    {
                        this.T(TokenType.KeywordVar);
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
        this.MainFunctionPlaceHolder("""
            {
                var x = 5;
            }
            """, BlockStatement);
    }

    [Fact]
    public void TestBlockExpressionEmpty()
    {
        void BlockStatement()
        {
            this.N<Expr.Block>();
            this.T(TokenType.CurlyOpen);
            this.N<Expr.BlockContents>();
            this.T(TokenType.CurlyClose);
        }
        this.MainFunctionPlaceHolder("""
            {
            }
            """, BlockStatement);
    }

    [Fact]
    public void TestOperatorPlusMinusTimesDividedMod()
    {
        void Operators()
        {
            this.N<Expr.Binary>();
            {
                this.N<Expr.Binary>();
                {
                    this.N<Expr.Binary>();
                    {
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger, "3");
                        }
                        this.T(TokenType.KeywordMod);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger, "2");
                        }
                    }
                    this.T(TokenType.Plus);
                    this.N<Expr.Binary>();
                    {
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger, "2");
                        }
                        this.T(TokenType.Star);
                        this.N<Expr.Unary>();
                        {
                            this.T(TokenType.Minus);
                            this.N<Expr.Literal>();
                            {
                                this.T(TokenType.LiteralInteger, "8");
                            }
                        }
                    }
                }
                this.T(TokenType.Minus);
                this.N<Expr.Binary>();
                {
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger, "9");
                    }
                    this.T(TokenType.Slash);
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger, "3");
                    }
                }
            }
        }
        this.VariableDeclarationPlaceHolder("""
            3 mod 2 + 2 * -8 - 9 / 3
            """, Operators);
    }

    [Fact]
    public void TestOperatorAndOrNot()
    {
        void Operators()
        {
            this.N<Expr.Binary>();
            {
                this.N<Expr.Binary>();
                {
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.KeywordTrue);
                    }
                    this.T(TokenType.KeywordAnd);
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.KeywordFalse);
                    }
                }
                this.T(TokenType.KeywordOr);
                this.N<Expr.Unary>();
                {
                    this.T(TokenType.KeywordNot);
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.KeywordFalse);
                    }
                }
            }
        }
        this.VariableDeclarationPlaceHolder("""
            true and false or not false
            """, Operators);
    }

    [Fact]
    public void TestOperatorGreaterThanPlusTimes()
    {
        void Operators()
        {
            this.N<Expr.Relational>();
            {
                this.N<Expr.Binary>();
                {
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger, "3");
                    }
                    this.T(TokenType.Plus);
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger, "2");
                    }
                }
                this.N<Expr.ComparisonElement>();
                {
                    this.T(TokenType.GreaterThan);
                    this.N<Expr.Binary>();
                    {
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger, "2");
                        }
                        this.T(TokenType.Star);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger, "3");
                        }
                    }
                }
            }
        }
        this.VariableDeclarationPlaceHolder("""
            3 + 2 > 2 * 3
            """, Operators);
    }

    [Fact]
    public void TestOperatorChainedRelations()
    {
        void Operators()
        {
            this.N<Expr.Binary>();
            {
                this.N<Expr.Relational>();
                {
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger, "3");
                    }
                    this.N<Expr.ComparisonElement>();
                    {
                        this.T(TokenType.GreaterThan);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger, "2");
                        }
                        this.N<ComparisonElement>();
                        {
                            this.T(TokenType.LessThan);
                            this.N<Expr.Literal>();
                            {
                                this.T(TokenType.LiteralInteger, "8");
                            }
                        }
                    }
                }
                this.T(TokenType.KeywordOr);
                this.N<Expr.Relational>();
                {
                    this.N<Expr.Literal>();
                    {
                        this.T(TokenType.LiteralInteger, "5");
                    }
                    this.N<Expr.ComparisonElement>();
                    {
                        this.T(TokenType.Equal);
                        this.N<Expr.Literal>();
                        {
                            this.T(TokenType.LiteralInteger, "3");
                        }
                    }
                }
            }
        }
        this.VariableDeclarationPlaceHolder("""
            3 > 2 < 8 or 5 == 3
            """, Operators);
    }
}
