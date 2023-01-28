using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Syntax;
using TokenType = Draco.Compiler.Api.Syntax.TokenType;

namespace Draco.Compiler.Tests.Syntax;

public sealed class ParserTests
{
    private IEnumerator<SyntaxNode> treeEnumerator = Enumerable.Empty<SyntaxNode>().GetEnumerator();
    private ConditionalWeakTable<SyntaxNode, Diagnostic> diagnostics = new();

    private void ParseInto<T>(string text, Func<Parser, T> func)
        where T : SyntaxNode
    {
        var srcReader = SourceReader.From(text);
        var lexer = new Lexer(srcReader);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource);
        this.diagnostics = parser.Diagnostics;
        this.treeEnumerator = func(parser).PreOrderTraverse().GetEnumerator();
    }

    private void ParseCompilationUnit(string text) =>
        this.ParseInto(text, p => p.ParseCompilationUnit());

    private void ParseDeclaration(string text) =>
        this.ParseInto(text, p => p.ParseDeclaration());

    private void ParseExpression(string text) =>
        this.ParseInto(text, p => p.ParseExpression());

    private void ParseStatement(string text) =>
        this.ParseInto(text, p => p.ParseStatement(true));

    private void N<T>(Predicate<T> predicate)
        where T : SyntaxNode
    {
        Assert.NotNull(this.treeEnumerator);
        Assert.True(this.treeEnumerator.MoveNext());
        var node = this.treeEnumerator.Current;
        Assert.IsType<T>(node);
        Assert.True(predicate((T)(object)node));
    }

    private void N<T>()
        where T : SyntaxNode => this.N<T>(_ => true);

    private void T(TokenType type) => this.N<SyntaxToken>(
           t => t.Type == type
        && !this.diagnostics.TryGetValue(t, out _));

    private void T(TokenType type, string text) => this.N<SyntaxToken>(
           t => t.Type == type
        && t.Text == text
        && this.diagnostics.TryGetValue(t, out _));

    private void TValue(TokenType type, string value) => this.N<SyntaxToken>(
           t => t.Type == type
        && t.ValueText == value
        && !this.diagnostics.TryGetValue(t, out _));

    private void MissingT(TokenType type) => this.N<SyntaxToken>(
           t => t.Type == type
        && this.diagnostics.TryGetValue(t, out _));

    private void MainFunctionPlaceHolder(string inputString, Action predicate)
    {
        this.ParseCompilationUnit($$"""
            func main(){
                {{inputString}}
            }
            """);
        this.N<CompilationUnitSyntax>();
        {
            this.N<FunctionDeclarationSyntax>();
            {
                this.T(TokenType.KeywordFunc);
                this.T(TokenType.Identifier);

                this.T(TokenType.ParenOpen);
                this.T(TokenType.ParenClose);

                this.N<BlockFunctionBodySyntax>();
                {
                    this.T(TokenType.CurlyOpen);
                    predicate();
                    this.T(TokenType.CurlyClose);
                }

            }
        }
    }

    private void StringNewline()
    {
        this.N<TextStringPartSyntax>();
        {
            this.T(TokenType.StringNewline);
        }
    }

    private void StringContent(string content)
    {
        this.N<TextStringPartSyntax>();
        {
            this.TValue(TokenType.StringContent, content);
        }
    }

    [Fact]
    public void TestEmpty()
    {
        this.ParseCompilationUnit(string.Empty);

        this.N<CompilationUnitSyntax>();
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

        this.N<CompilationUnitSyntax>();
        {
            this.N<FunctionDeclarationSyntax>();
            {
                this.T(TokenType.KeywordFunc);
                this.T(TokenType.Identifier);

                this.T(TokenType.ParenOpen);
                this.T(TokenType.ParenClose);

                this.N<BlockFunctionBodySyntax>();
                {
                    this.T(TokenType.CurlyOpen);
                    this.T(TokenType.CurlyClose);
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

        this.N<CompilationUnitSyntax>();
        {
            this.N<FunctionDeclarationSyntax>();
            {
                this.T(TokenType.KeywordFunc);
                this.T(TokenType.Identifier);

                this.T(TokenType.ParenOpen);
                this.T(TokenType.ParenClose);

                this.N<BlockFunctionBodySyntax>();
                {
                    this.T(TokenType.CurlyOpen);
                    this.MissingT(TokenType.CurlyClose);
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
        this.N<StringExpressionSyntax>();
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
        this.N<StringExpressionSyntax>();
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
        this.N<StringExpressionSyntax>();
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
        this.N<StringExpressionSyntax>();
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
        this.N<StringExpressionSyntax>();
        {
            this.T(TokenType.LineStringStart);
            this.StringContent("Hello, ");
            this.N<InterpolationStringPartSyntax>();
            {
                this.T(TokenType.InterpolationStart);
                this.N<StringExpressionSyntax>();
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
        this.N<StringExpressionSyntax>();
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
        this.N<StringExpressionSyntax>();
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
        // There is semicolon at the end, because the string is put into declaration
        this.MainFunctionPlaceHolder("""
            val x = "Hello, World!;
            """, () =>
        {
            this.N<DeclarationStatementSyntax>();
            {
                this.N<VariableDeclarationSyntax>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenType.Assign);
                        this.N<StringExpressionSyntax>();
                        {
                            this.T(TokenType.LineStringStart);
                            this.StringContent("Hello, World!;");
                            this.MissingT(TokenType.LineStringEnd);
                        }
                    }
                    this.MissingT(TokenType.Semicolon);
                }
            }
        });
    }

    [Fact]
    public void TestNewLineInLineString()
    {
        this.MainFunctionPlaceHolder("""
            val x = "Hello, World!
            ";
            """, () =>
        {
            this.N<DeclarationStatementSyntax>();
            {
                this.N<VariableDeclarationSyntax>();
                {
                    this.T(TokenType.KeywordVal);
                    this.T(TokenType.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenType.Assign);
                        this.N<StringExpressionSyntax>();
                        {
                            this.T(TokenType.LineStringStart);
                            this.StringContent("Hello, World!");
                            this.MissingT(TokenType.LineStringEnd);
                        }
                    }
                    this.MissingT(TokenType.Semicolon);
                }
            }
            this.N<StringExpressionSyntax>();
            {
                this.T(TokenType.LineStringStart);
                this.StringContent(";");
                this.MissingT(TokenType.LineStringEnd);
            }
        });
    }

    [Fact]
    public void TestMultilineStringUnclosedInterpolation()
    {
        const string trailingSpace = " ";
        this.ParseExpression($$""""
            """
            Hello,{{trailingSpace}}
            \{"World!"
            """
            """");
        this.N<StringExpressionSyntax>();
        {
            this.T(TokenType.MultiLineStringStart);
            this.StringContent($"Hello,{trailingSpace}");
            this.StringNewline();
            this.N<InterpolationStringPartSyntax>();
            {
                this.T(TokenType.InterpolationStart);
                this.N<StringExpressionSyntax>();
                {
                    this.T(TokenType.LineStringStart);
                    this.StringContent("World!");
                    this.T(TokenType.LineStringEnd);
                }
            }
            this.MissingT(TokenType.InterpolationEnd);
            this.MissingT(TokenType.MultiLineStringEnd);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithValueAndMissingSemicolon()
    {
        this.ParseDeclaration("val x = 5");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.N<ValueSpecifierSyntax>();
            {
                this.T(TokenType.Assign);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger);
                }
            }
            this.MissingT(TokenType.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithNoValue()
    {
        this.ParseDeclaration("val x;");
        this.N<VariableDeclarationSyntax>();
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
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.N<ValueSpecifierSyntax>();
            {
                this.T(TokenType.Assign);
                this.N<LiteralExpressionSyntax>();
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
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.N<ValueSpecifierSyntax>();
            {
                this.T(TokenType.Assign);
                this.N<UnexpectedExpressionSyntax>();
                this.T(TokenType.Semicolon);
            }
        }
    }

    [Fact]
    public void TestVariableDeclarationWithTypeAndWithNoValue()
    {
        this.ParseDeclaration("val x: int32;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenType.Colon);
                this.N<NameTypeSyntax>();
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
        this.ParseDeclaration("val x:;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenType.Colon);
                this.N<UnexpectedTypeSyntax>();
            }
            this.T(TokenType.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithTypeAndWithValue()
    {
        this.ParseDeclaration("val x: int32 = 5;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenType.Colon);
                this.N<NameTypeSyntax>();
                {
                    this.T(TokenType.Identifier);
                }
                this.N<ValueSpecifierSyntax>();
                {
                    this.T(TokenType.Assign);
                    this.N<LiteralExpressionSyntax>();
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
        this.ParseDeclaration("val x: =;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenType.KeywordVal);
            this.T(TokenType.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenType.Colon);
                this.N<UnexpectedTypeSyntax>();
                this.N<ValueSpecifierSyntax>();
                {
                    this.T(TokenType.Assign);
                    this.N<UnexpectedExpressionSyntax>();
                    this.T(TokenType.Semicolon);
                }
            }
        }
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
        this.N<ExpressionStatementSyntax>();
        this.N<IfExpressionSyntax>();
        {
            this.T(TokenType.KeywordIf);
            this.T(TokenType.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger);
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                }
            }
            this.T(TokenType.ParenClose);
            this.N<StatementExpressionSyntax>();
            this.N<ExpressionStatementSyntax>();
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenType.CurlyOpen);
                this.N<DeclarationStatementSyntax>();
                {
                    this.N<VariableDeclarationSyntax>();
                    {
                        this.T(TokenType.KeywordVal);
                        this.T(TokenType.Identifier);
                        this.N<ValueSpecifierSyntax>();
                        {
                            this.T(TokenType.Assign);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenType.LiteralInteger);
                            }
                        }
                        this.T(TokenType.Semicolon);
                    }
                }
                this.T(TokenType.CurlyClose);
            }
            this.N<ElseClauseSyntax>();
            {
                this.T(TokenType.KeywordElse);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<BlockExpressionSyntax>();
                {
                    this.T(TokenType.CurlyOpen);
                    this.N<VariableDeclarationSyntax>();
                    {
                        this.T(TokenType.KeywordVal);
                        this.T(TokenType.Identifier);
                        this.N<ValueSpecifierSyntax>();
                        {
                            this.T(TokenType.Assign);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenType.LiteralCharacter);
                            }
                        }
                        this.T(TokenType.Semicolon);
                    }
                    this.T(TokenType.CurlyClose);
                }
            }
        }
    }

    [Fact]
    public void TestElseStatementsMissingIf()
    {
        this.MainFunctionPlaceHolder("""
            else {
                val y = 8;
            }
            """, () =>
        {
            this.N<UnexpectedStatementSyntax>();
            {
                this.T(TokenType.KeywordElse);
            }
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenType.CurlyOpen);
                this.N<DeclarationStatementSyntax>();
                {
                    this.N<VariableDeclarationSyntax>();
                    {
                        this.T(TokenType.KeywordVal);
                        this.T(TokenType.Identifier);
                        this.N<ValueSpecifierSyntax>();
                        {
                            this.T(TokenType.Assign);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenType.LiteralInteger);
                            }
                        }
                        this.T(TokenType.Semicolon);
                    }
                }
                this.T(TokenType.CurlyClose);
            }
        });
    }

    [Fact]
    public void TestIfStatementMissingClosingParen()
    {
        this.ParseStatement("""
            if (5 > 0 {
                val x = 5;
            }
            """);
        this.N<ExpressionStatementSyntax>();
        {
            this.N<IfExpressionSyntax>();
            {
                this.T(TokenType.KeywordIf);
                this.T(TokenType.ParenOpen);
                this.N<RelationalExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                    this.N<ComparisonElementSyntax>();
                    {
                        this.T(TokenType.GreaterThan);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                }
                this.MissingT(TokenType.ParenClose);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<BlockExpressionSyntax>();
                {
                    this.T(TokenType.CurlyOpen);
                    this.N<DeclarationStatementSyntax>();
                    {
                        this.N<VariableDeclarationSyntax>();
                        {
                            this.T(TokenType.KeywordVal);
                            this.T(TokenType.Identifier);
                            this.N<ValueSpecifierSyntax>();
                            {
                                this.T(TokenType.Assign);
                                this.N<LiteralExpressionSyntax>();
                                {
                                    this.T(TokenType.LiteralInteger);
                                }
                            }
                            this.T(TokenType.Semicolon);
                        }
                    }
                    this.T(TokenType.CurlyClose);
                }
            }
        }
    }

    [Fact]
    public void TestIfStatementMissingContents()
    {
        this.MainFunctionPlaceHolder("""
            if (5 > 0)
            """, () =>
        {
            this.N<IfExpressionSyntax>();
            {
                this.T(TokenType.KeywordIf);
                this.T(TokenType.ParenOpen);
                this.N<RelationalExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                    this.N<ComparisonElementSyntax>();
                    {
                        this.T(TokenType.GreaterThan);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                }
                this.T(TokenType.ParenClose);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<UnexpectedExpressionSyntax>();
                // TODO: This should be unnecessary
                this.MissingT(TokenType.Semicolon);
            }
        });
    }

    [Fact]
    public void TestIfElseExpression()
    {
        this.ParseExpression("""
            if (5 > 0) 3 else 9
            """);

        this.N<IfExpressionSyntax>();
        {
            this.T(TokenType.KeywordIf);
            this.T(TokenType.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger);
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                }
            }
            this.T(TokenType.ParenClose);
            this.N<LiteralExpressionSyntax>();
            {
                this.T(TokenType.LiteralInteger);
            }
            this.N<ElseClauseSyntax>();
            {
                this.T(TokenType.KeywordElse);
                this.N<LiteralExpressionSyntax>();
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

        this.N<IfExpressionSyntax>();
        {
            this.T(TokenType.KeywordIf);
            this.T(TokenType.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger);
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                }
            }
            this.T(TokenType.ParenClose);
            this.N<LiteralExpressionSyntax>();
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

        this.N<ExpressionStatementSyntax>();
        this.N<WhileExpressionSyntax>();
        {
            this.T(TokenType.KeywordWhile);
            this.T(TokenType.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenType.Identifier);
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.LessThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                }
            }
            this.T(TokenType.ParenClose);
            this.N<StatementExpressionSyntax>();
            this.N<ExpressionStatementSyntax>();
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenType.CurlyOpen);
                this.N<ExpressionStatementSyntax>();
                {
                    this.N<BinaryExpressionSyntax>();
                    {
                        this.N<NameExpressionSyntax>();
                        {
                            this.T(TokenType.Identifier);
                        }
                        this.T(TokenType.Assign);
                        this.N<BinaryExpressionSyntax>();
                        {
                            this.N<NameExpressionSyntax>();
                            {
                                this.T(TokenType.Identifier);
                            }
                            this.T(TokenType.Plus);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenType.LiteralInteger);
                            }
                        }
                        this.T(TokenType.Semicolon);
                    }
                }
                this.T(TokenType.CurlyClose);
            }
        }
    }

    [Fact]
    public void TestWhileStatementMissingClosingParen()
    {
        this.ParseStatement("""
            while (x < 5 {
                x = x + 1;
            }
            """);

        this.N<ExpressionStatementSyntax>();
        this.N<WhileExpressionSyntax>();
        {
            this.T(TokenType.KeywordWhile);
            this.T(TokenType.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenType.Identifier);
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.LessThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger);
                    }
                }
            }
            this.MissingT(TokenType.ParenClose);
            this.N<StatementExpressionSyntax>();
            this.N<ExpressionStatementSyntax>();
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenType.CurlyOpen);
                this.N<ExpressionStatementSyntax>();
                {
                    this.N<BinaryExpressionSyntax>();
                    {
                        this.N<NameExpressionSyntax>();
                        {
                            this.T(TokenType.Identifier);
                        }
                        this.T(TokenType.Assign);
                        this.N<BinaryExpressionSyntax>();
                        {
                            this.N<NameExpressionSyntax>();
                            {
                                this.T(TokenType.Identifier);
                            }
                            this.T(TokenType.Plus);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenType.LiteralInteger);
                            }
                        }
                        this.T(TokenType.Semicolon);
                    }
                }
                this.T(TokenType.CurlyClose);
            }
        }
    }

    [Fact]
    public void TestWhileStatementMissingContents()
    {
        this.MainFunctionPlaceHolder("""
            while (x < 5)
            """, () =>
        {
            this.N<WhileExpressionSyntax>();
            {
                this.T(TokenType.KeywordWhile);
                this.T(TokenType.ParenOpen);
                this.N<RelationalExpressionSyntax>();
                {
                    this.N<NameExpressionSyntax>();
                    {
                        this.T(TokenType.Identifier);
                    }
                    this.N<ComparisonElementSyntax>();
                    {
                        this.T(TokenType.LessThan);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                }
                this.T(TokenType.ParenClose);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<UnexpectedExpressionSyntax>();
                // TODO: This should be unnecessary
                this.MissingT(TokenType.Semicolon);
            }
        });
    }

    [Fact]
    public void TestLabelDeclaration()
    {
        this.ParseDeclaration("""
            myLabel:
            """);

        this.N<LabelDeclarationSyntax>();
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

        this.N<LabelDeclarationSyntax>();
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

        this.N<GotoExpressionSyntax>();
        {
            this.T(TokenType.KeywordGoto);
            this.N<NameLabelSyntax>();
            {
                this.T(TokenType.Identifier);
            }
        }
    }

    [Fact]
    public void TestGotoNoLabelSpecified()
    {
        this.MainFunctionPlaceHolder("""
                goto;
            """, () =>
        {
            this.N<ExpressionStatementSyntax>();
            this.N<GotoExpressionSyntax>();
            {
                this.T(TokenType.KeywordGoto);
                this.N<NameLabelSyntax>();
                {
                    this.MissingT(TokenType.Identifier);
                }
            }
            this.T(TokenType.Semicolon);
        });
    }

    [Fact]
    public void TestReturnWithValue()
    {
        this.ParseExpression("""
                return 2 + 1
            """);

        this.N<ReturnExpressionSyntax>();
        {
            this.T(TokenType.KeywordReturn);
            this.N<BinaryExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger);
                }
                this.T(TokenType.Plus);
                this.N<LiteralExpressionSyntax>();
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

        this.N<ReturnExpressionSyntax>();
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

        this.N<BlockExpressionSyntax>();
        {
            this.T(TokenType.CurlyOpen);
            this.N<DeclarationStatementSyntax>();
            {
                this.N<VariableDeclarationSyntax>();
                {
                    this.T(TokenType.KeywordVar);
                    this.T(TokenType.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenType.Assign);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                    this.T(TokenType.Semicolon);
                }
            }
            this.N<NameExpressionSyntax>();
            {
                this.T(TokenType.Identifier);
            }
            this.T(TokenType.CurlyClose);
        }
    }

    [Fact]
    public void TestBlockExpressionWithoutValue()
    {
        this.ParseExpression("""
            {
                var x = 5;
            }
            """);

        this.N<BlockExpressionSyntax>();
        {
            this.T(TokenType.CurlyOpen);
            this.N<DeclarationStatementSyntax>();
            {
                this.N<VariableDeclarationSyntax>();
                {
                    this.T(TokenType.KeywordVar);
                    this.T(TokenType.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenType.Assign);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenType.LiteralInteger);
                        }
                    }
                    this.T(TokenType.Semicolon);
                }
            }
            this.T(TokenType.CurlyClose);
        }
    }

    [Fact]
    public void TestBlockExpressionEmpty()
    {
        this.ParseExpression("""
            {
            }
            """);

        this.N<BlockExpressionSyntax>();
        {
            this.T(TokenType.CurlyOpen);
            this.T(TokenType.CurlyClose);
        }
    }

    [Fact]
    public void TestOperatorPlusMinusTimesDividedMod()
    {
        this.ParseExpression("""
            3 mod 2 + 2 * -8.6 - 9 / 3
            """);

        this.N<BinaryExpressionSyntax>();
        {
            this.N<BinaryExpressionSyntax>();
            {
                this.N<BinaryExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger, "3");
                    }
                    this.T(TokenType.KeywordMod);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger, "2");
                    }
                }
                this.T(TokenType.Plus);
                this.N<BinaryExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger, "2");
                    }
                    this.T(TokenType.Star);
                    this.N<UnaryExpressionSyntax>();
                    {
                        this.T(TokenType.Minus);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenType.LiteralFloat, "8.6");
                        }
                    }
                }
            }
            this.T(TokenType.Minus);
            this.N<BinaryExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger, "9");
                }
                this.T(TokenType.Slash);
                this.N<LiteralExpressionSyntax>();
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

        this.N<BinaryExpressionSyntax>();
        {
            this.N<BinaryExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.KeywordTrue);
                }
                this.T(TokenType.KeywordAnd);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.KeywordFalse);
                }
            }
            this.T(TokenType.KeywordOr);
            this.N<UnaryExpressionSyntax>();
            {
                this.T(TokenType.KeywordNot);
                this.N<LiteralExpressionSyntax>();
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
            3.8 + 2 > 2 * 3
            """);

        this.N<RelationalExpressionSyntax>();
        {
            this.N<BinaryExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralFloat, "3.8");
                }
                this.T(TokenType.Plus);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger, "2");
                }
            }
            this.N<ComparisonElementSyntax>();
            {
                this.T(TokenType.GreaterThan);
                this.N<BinaryExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger, "2");
                    }
                    this.T(TokenType.Star);
                    this.N<LiteralExpressionSyntax>();
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
            3 > 2.89 < 8 or 5 == 3
            """);

        this.N<BinaryExpressionSyntax>();
        {
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger, "3");
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralFloat, "2.89");
                    }
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.LessThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger, "8");
                    }
                }
            }
            this.T(TokenType.KeywordOr);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenType.LiteralInteger, "5");
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenType.Equal);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenType.LiteralInteger, "3");
                    }
                }
            }
        }
    }

    [Fact]
    public void TestEmptyInterpolation()
    {
        this.ParseExpression("""
            "a\{}b"
            """);

        this.N<StringExpressionSyntax>();
        {
            this.T(TokenType.LineStringStart, "\"");
            this.StringContent("a");
            this.N<InterpolationStringPartSyntax>();
            {
                this.T(TokenType.InterpolationStart);
                this.N<UnexpectedExpressionSyntax>();
                this.T(TokenType.InterpolationEnd);
            }
            this.StringContent("b");
            this.T(TokenType.LineStringEnd, "\"");
        }
    }
}
