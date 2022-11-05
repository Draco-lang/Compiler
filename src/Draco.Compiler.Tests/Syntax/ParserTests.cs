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

    private void ParseDeclaration(string text)
    {
        this.treeEnumerator = ParseInto(text, p => p.ParseDeclaration())
            .InOrderTraverse()
            .GetEnumerator();
    }

    private void ParseExpression(string text)
    {
        this.treeEnumerator = ParseInto(text, p => p.ParseExpr())
            .InOrderTraverse()
            .GetEnumerator();
    }

    private void ParseStatement(string text)
    {
        this.treeEnumerator = ParseInto(text, p => p.ParseStatement(true))
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
        this.ParseExpression("""
            "Hello, World!"
            """);
        this.N<Expr.String>();
        {
            this.T(TokenType.LineStringStart);
            this.StringContent("Hello, World!");
            this.T(TokenType.LineStringEnd);
        }
    }

    [Fact]
    public void TestMultilineString()
    {
        this.ParseExpression($""""
            """
            Hello, World!
            """
            """");
        this.N<Expr.String>();
        {
            this.T(TokenType.MultiLineStringStart);
            this.StringContent("Hello, World!");
            this.T(TokenType.MultiLineStringEnd);
        }
    }

    [Fact]
    public void TestMultilineStringEmpty()
    {
        this.ParseExpression($""""
            """
            """
            """");
        this.N<Expr.String>();
        {
            this.T(TokenType.MultiLineStringStart);
            this.T(TokenType.MultiLineStringEnd);
        }
    }

    [Fact]
    public void TestLineStringEmpty()
    {
        this.ParseExpression($""""
            ""
            """");
        this.N<Expr.String>();
        {
            this.T(TokenType.LineStringStart);
            this.T(TokenType.LineStringEnd);
        }
    }

    [Fact]
    public void TestLineStringInterpolation()
    {
        this.ParseExpression("""
            "Hello, \{"World"}!"
            """);
        this.N<Expr.String>();
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
    }

    [Fact]
    public void TestStringEscapes()
    {
        this.ParseExpression("""
            "Hello, \nWorld! \u{1F47D}"
            """);
        this.N<Expr.String>();
        {
            this.T(TokenType.LineStringStart);
            this.StringContent("Hello, \nWorld! 👽");
            this.T(TokenType.LineStringEnd);
        }
    }

    [Fact]
    public void TestMultilineStringContinuations()
    {
        this.ParseExpression($""""
            """
            Hello, \    
            World!
            """
            """");
        this.N<Expr.String>();
        {
            this.T(TokenType.MultiLineStringStart);
            this.StringContent("Hello, ");
            this.StringNewline();
            this.StringContent("World!");
            this.T(TokenType.MultiLineStringEnd);
        }
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
            this.N<Expr.String>();
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
        }
        var quotes = "\"\"\"";
        this.MainFunctionPlaceHolder($$""""
            """
            Hello, 
            \{"World!"
            """
            """", UnclosedString);
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
        this.ParseDeclaration("val x;");
        this.N<Decl.Variable>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.T(TokenType.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithValue()
    {
        this.ParseDeclaration("val x = 5;");
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

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithMissingValue()
    {
        this.ParseDeclaration("val x =;");
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

    [Fact]
    public void TestVariableDeclarationWithTypeAndWithNoValue()
    {
        this.ParseDeclaration("val x: int32;");
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

    [Fact]
    public void TestVariableDeclarationWithMissingTypeAndNoValue()
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
    public void TestVariableDeclarationWithTypeAndWithValue()
    {
        this.ParseDeclaration("val x: int32 = 5;");
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

    [Fact]
    public void TestVariableDeclarationWithMissingTypeAndMissingValue()
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
        this.ParseStatement("""
            if (5 > 0){
                val x = 5;
            }
            else {
                val y = 'c';
            }
            """);
        this.N<Stmt.Expr>();
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
        this.ParseStatement("""
            if (5 > 0 {
                val x = 5;
            }
            """);
        this.N<Stmt.Expr>();
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
        this.ParseExpression("""
            if (5 > 0) 3 else 9
            """);

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

    [Fact]
    public void TestIfExpressionNoElse()
    {
        this.ParseExpression("""
            if (5 > 0) 3
            """);

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

    [Fact]
    public void TestWhileStatement()
    {
        this.ParseStatement("""
            while (x < 5) {
                x = x + 1;
            }
            """);

        this.N<Stmt.Expr>();
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

    [Fact]
    public void TestWhileStatementMissingClosingParen()
    {
        this.ParseStatement("""
            while (x < 5 {
                x = x + 1;
            }
            """);

        this.N<Stmt.Expr>();
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
        this.ParseDeclaration("""
            myLabel:
            """);

        this.N<Decl.Label>();
        {
            this.T(TokenType.Identifier, "myLabel");
            this.T(TokenType.Colon);
        }
    }

    [Fact]
    public void TestLabelDeclarationNewlineBeforeColon()
    {
        this.ParseDeclaration("""
            myLabel
            :
            """);

        this.N<Decl.Label>();
        {
            this.T(TokenType.Identifier, "myLabel");
            this.T(TokenType.Colon);
        }
    }

    [Fact]
    public void TestGoto()
    {
        this.ParseExpression("""
            goto myLabel
            """);

        this.N<Expr.Goto>();
        {
            this.T(TokenType.KeywordGoto);
            this.N<Expr.Name>();
            {
                this.T(TokenType.Identifier);
            }
        }
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
        this.ParseExpression("""
                return 2 + 1
            """);

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
    }

    [Fact]
    public void TestReturnWithoutValue()
    {
        this.ParseExpression("""
                return
            """);

        this.N<Expr.Return>();
        {
            this.T(TokenType.KeywordReturn);
        }
    }

    [Fact]
    public void TestBlockExpressionWithValue()
    {
        this.ParseExpression("""
            {
                var x = 5;
                x
            }
            """);

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

    [Fact]
    public void TestBlockExpressionWithoutValue()
    {
        this.ParseExpression("""
            {
                var x = 5;
            }
            """);

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

    [Fact]
    public void TestBlockExpressionEmpty()
    {
        this.ParseExpression("""
            {
            }
            """);

        this.N<Expr.Block>();
        this.T(TokenType.CurlyOpen);
        this.N<Expr.BlockContents>();
        this.T(TokenType.CurlyClose);
    }

    [Fact]
    public void TestOperatorPlusMinusTimesDividedMod()
    {
        this.ParseExpression("""
            3 mod 2 + 2 * -8 - 9 / 3
            """);

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

    [Fact]
    public void TestOperatorAndOrNot()
    {
        this.ParseExpression("""
            true and false or not false
            """);

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

    [Fact]
    public void TestOperatorGreaterThanPlusTimes()
    {
        this.ParseExpression("""
            3 + 2 > 2 * 3
            """);

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

    [Fact]
    public void TestOperatorChainedRelations()
    {
        this.ParseExpression("""
            3 > 2 < 8 or 5 == 3
            """);

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
}
