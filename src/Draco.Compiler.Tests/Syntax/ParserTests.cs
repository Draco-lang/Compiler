using Draco.Compiler.Internal.Syntax;
using Draco.Trace;
using TokenKind = Draco.Compiler.Api.Syntax.TokenKind;

namespace Draco.Compiler.Tests.Syntax;

public sealed class ParserTests
{
    private readonly SyntaxDiagnosticTable diagnostics = new();
    private IEnumerator<SyntaxNode> treeEnumerator = Enumerable.Empty<SyntaxNode>().GetEnumerator();

    private void ParseInto<T>(string text, Func<Parser, T> func)
        where T : SyntaxNode
    {
        var srcReader = SourceReader.From(text);
        var lexer = new Lexer(srcReader, this.diagnostics, tracer: Tracer.Null);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource, this.diagnostics, tracer: Tracer.Null);
        this.treeEnumerator = func(parser).PreOrderTraverse().GetEnumerator();
    }

    private void ParseCompilationUnit(string text) =>
        this.ParseInto(text, p => p.ParseCompilationUnit());

    private void ParseDeclaration(string text) =>
        this.ParseInto(text, p => p.ParseDeclaration());

    private void ParseLocalDeclaration(string text) =>
        this.ParseInto(text, p => p.ParseDeclaration(true));

    private void ParseExpression(string text) =>
        this.ParseInto(text, p => p.ParseExpression());

    private void ParseStatement(string text) =>
        this.ParseInto(text, p => p.ParseStatement(true));

    private void N<T>(Predicate<T> predicate)
        where T : SyntaxNode
    {
        Assert.True(this.treeEnumerator.MoveNext());
        var node = this.treeEnumerator.Current;
        Assert.IsType<T>(node);
        Assert.True(predicate((T)node));
    }

    private void N<T>()
        where T : SyntaxNode => this.N<T>(_ => true);

    private void T(TokenKind type) => this.N<SyntaxToken>(t =>
           t.Kind == type
        && this.diagnostics.Get(t).Count == 0);

    private void T(TokenKind type, string text) => this.N<SyntaxToken>(t =>
           t.Kind == type
        && t.Text == text
        && this.diagnostics.Get(t).Count == 0);

    private void TValue(TokenKind type, string value) => this.N<SyntaxToken>(t =>
           t.Kind == type
        && t.ValueText == value
        && this.diagnostics.Get(t).Count == 0);

    private void MissingT(TokenKind type) => this.N<SyntaxToken>(t =>
           t.Kind == type
        && this.diagnostics.Get(t).Count > 0);

    private void MainFunctionPlaceHolder(string inputString, Action predicate)
    {
        this.ParseCompilationUnit($$"""
            func main() {
                {{inputString}}
            }
            """);
        this.N<CompilationUnitSyntax>();
        {
            this.N<SyntaxList<DeclarationSyntax>>();
            this.N<FunctionDeclarationSyntax>();
            {
                this.T(TokenKind.KeywordFunc);
                this.T(TokenKind.Identifier);

                this.T(TokenKind.ParenOpen);
                this.N<SeparatedSyntaxList<ParameterSyntax>>();
                this.T(TokenKind.ParenClose);

                this.N<BlockFunctionBodySyntax>();
                {
                    this.T(TokenKind.CurlyOpen);
                    this.N<SyntaxList<StatementSyntax>>();
                    predicate();
                    this.T(TokenKind.CurlyClose);
                }

            }
        }
    }

    private void StringNewline()
    {
        this.N<TextStringPartSyntax>();
        {
            this.T(TokenKind.StringNewline);
        }
    }

    private void StringContent(string content)
    {
        this.N<TextStringPartSyntax>();
        {
            this.TValue(TokenKind.StringContent, content);
        }
    }

    [Fact]
    public void TestEmpty()
    {
        this.ParseCompilationUnit(string.Empty);

        this.N<CompilationUnitSyntax>();
        this.N<SyntaxList<DeclarationSyntax>>();
        {
            this.T(TokenKind.EndOfInput);
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
        this.N<SyntaxList<DeclarationSyntax>>();
        {
            this.N<FunctionDeclarationSyntax>();
            {
                this.T(TokenKind.KeywordFunc);
                this.T(TokenKind.Identifier);

                this.T(TokenKind.ParenOpen);
                this.N<SeparatedSyntaxList<ParameterSyntax>>();
                this.T(TokenKind.ParenClose);

                this.N<BlockFunctionBodySyntax>();
                {
                    this.T(TokenKind.CurlyOpen);
                    this.N<SyntaxList<StatementSyntax>>();
                    this.T(TokenKind.CurlyClose);
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
        this.N<SyntaxList<DeclarationSyntax>>();
        {
            this.N<FunctionDeclarationSyntax>();
            {
                this.T(TokenKind.KeywordFunc);
                this.T(TokenKind.Identifier);

                this.T(TokenKind.ParenOpen);
                this.N<SeparatedSyntaxList<ParameterSyntax>>();
                this.T(TokenKind.ParenClose);

                this.N<BlockFunctionBodySyntax>();
                {
                    this.T(TokenKind.CurlyOpen);
                    this.N<SyntaxList<StatementSyntax>>();
                    this.MissingT(TokenKind.CurlyClose);
                }
            }
        }
    }

    [Fact]
    public void TestGenericFunc()
    {
        this.ParseCompilationUnit("""
            func foo<T, U>() {
            }
            """);

        this.N<CompilationUnitSyntax>();
        this.N<SyntaxList<DeclarationSyntax>>();
        {
            this.N<FunctionDeclarationSyntax>();
            {
                this.T(TokenKind.KeywordFunc);
                this.T(TokenKind.Identifier);

                this.N<GenericParameterListSyntax>();
                {
                    this.T(TokenKind.LessThan);
                    this.N<SeparatedSyntaxList<GenericParameterSyntax>>();
                    {
                        this.N<GenericParameterSyntax>();
                        {
                            this.T(TokenKind.Identifier, "T");
                        }
                        this.T(TokenKind.Comma);
                        this.N<GenericParameterSyntax>();
                        {
                            this.T(TokenKind.Identifier, "U");
                        }
                    }
                    this.T(TokenKind.GreaterThan);
                }

                this.T(TokenKind.ParenOpen);
                this.N<SeparatedSyntaxList<ParameterSyntax>>();
                this.T(TokenKind.ParenClose);

                this.N<BlockFunctionBodySyntax>();
                {
                    this.T(TokenKind.CurlyOpen);
                    this.N<SyntaxList<StatementSyntax>>();
                    this.T(TokenKind.CurlyClose);
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
            this.T(TokenKind.LineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.StringContent("Hello, World!");
            this.T(TokenKind.LineStringEnd);
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
            this.T(TokenKind.MultiLineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.StringContent("Hello, World!");
            this.T(TokenKind.MultiLineStringEnd);
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
            this.T(TokenKind.MultiLineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.T(TokenKind.MultiLineStringEnd);
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
            this.T(TokenKind.LineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.T(TokenKind.LineStringEnd);
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
            this.T(TokenKind.LineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.StringContent("Hello, ");
            this.N<InterpolationStringPartSyntax>();
            {
                this.T(TokenKind.InterpolationStart);
                this.N<StringExpressionSyntax>();
                {
                    this.T(TokenKind.LineStringStart);
                    this.N<SyntaxList<StringPartSyntax>>();
                    this.StringContent("World");
                    this.T(TokenKind.LineStringEnd);
                }
                this.T(TokenKind.InterpolationEnd);
            }
            this.StringContent("!");
            this.T(TokenKind.LineStringEnd);
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
            this.T(TokenKind.LineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.StringContent("Hello, \nWorld! 👽");
            this.T(TokenKind.LineStringEnd);
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
            this.T(TokenKind.MultiLineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.StringContent("Hello, ");
            this.StringNewline();
            this.StringContent("World!");
            this.T(TokenKind.MultiLineStringEnd);
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
                    this.T(TokenKind.KeywordVal);
                    this.T(TokenKind.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenKind.Assign);
                        this.N<StringExpressionSyntax>();
                        {
                            this.T(TokenKind.LineStringStart);
                            this.N<SyntaxList<StringPartSyntax>>();
                            this.StringContent("Hello, World!;");
                            this.MissingT(TokenKind.LineStringEnd);
                        }
                    }
                    this.MissingT(TokenKind.Semicolon);
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
                    this.T(TokenKind.KeywordVal);
                    this.T(TokenKind.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenKind.Assign);
                        this.N<StringExpressionSyntax>();
                        {
                            this.T(TokenKind.LineStringStart);
                            this.N<SyntaxList<StringPartSyntax>>();
                            this.StringContent("Hello, World!");
                            this.MissingT(TokenKind.LineStringEnd);
                        }
                    }
                    this.MissingT(TokenKind.Semicolon);
                }
            }
            this.N<ExpressionStatementSyntax>();
            this.N<StringExpressionSyntax>();
            {
                this.T(TokenKind.LineStringStart);
                this.N<SyntaxList<StringPartSyntax>>();
                this.StringContent(";");
                this.MissingT(TokenKind.LineStringEnd);
            }
            // TODO: Can we get rid of this?
            // Or do we want to?
            this.MissingT(TokenKind.Semicolon);
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
            this.T(TokenKind.MultiLineStringStart);
            this.N<SyntaxList<StringPartSyntax>>();
            this.StringContent($"Hello,{trailingSpace}");
            this.StringNewline();
            this.N<InterpolationStringPartSyntax>();
            {
                this.T(TokenKind.InterpolationStart);
                this.N<StringExpressionSyntax>();
                {
                    this.T(TokenKind.LineStringStart);
                    this.N<SyntaxList<StringPartSyntax>>();
                    this.StringContent("World!");
                    this.T(TokenKind.LineStringEnd);
                }
            }
            this.MissingT(TokenKind.InterpolationEnd);
            this.MissingT(TokenKind.MultiLineStringEnd);
        }
    }

    [Fact]
    public void TestUnexpectedDeclarationStartingWithVisibilityModifier()
    {
        this.ParseDeclaration("internal gibrish, more gibrish");
        this.N<UnexpectedDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordInternal);
            this.N<SyntaxList<SyntaxNode>>();
            {
                this.T(TokenKind.Identifier, "gibrish");
                this.T(TokenKind.Comma);
                this.T(TokenKind.Identifier, "more");
                this.T(TokenKind.Identifier, "gibrish");
            }
        }
    }

    [Fact]
    public void TestVariableDeclarationStatementStartingWithVisibilityModifier()
    {
        this.ParseStatement("internal var x = 0;");
        this.N<ExpressionStatementSyntax>();
        {
            this.N<UnexpectedExpressionSyntax>();
            {
                this.N<SyntaxList<SyntaxNode>>();
                {
                    this.T(TokenKind.KeywordInternal);
                    this.T(TokenKind.KeywordVar);
                    // NOTE: It is cut off at the first expression starter, which is the identifier, the rest of the code would be in next statement
                    this.MissingT(TokenKind.Semicolon);
                }
            }
        }
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithValueAndMissingSemicolon()
    {
        this.ParseDeclaration("val x = 5");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.N<ValueSpecifierSyntax>();
            {
                this.T(TokenKind.Assign);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
                }
            }
            this.MissingT(TokenKind.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithNoValue()
    {
        this.ParseDeclaration("val x;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.T(TokenKind.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithValue()
    {
        this.ParseDeclaration("val x = 5;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.N<ValueSpecifierSyntax>();
            {
                this.T(TokenKind.Assign);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
                }
            }
            this.T(TokenKind.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithNoTypeAndWithMissingValue()
    {
        this.ParseDeclaration("val x =;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.N<ValueSpecifierSyntax>();
            {
                this.T(TokenKind.Assign);
                this.N<UnexpectedExpressionSyntax>();
                {
                    this.N<SyntaxList<SyntaxNode>>();
                }
                this.T(TokenKind.Semicolon);
            }
        }
    }

    [Fact]
    public void TestVariableDeclarationWithTypeAndWithNoValue()
    {
        this.ParseDeclaration("val x: int32;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenKind.Colon);
                this.N<NameTypeSyntax>();
                {
                    this.T(TokenKind.Identifier);
                }
            }
            this.T(TokenKind.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithMissingTypeAndNoValue()
    {
        this.ParseDeclaration("val x:;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenKind.Colon);
                this.N<UnexpectedTypeSyntax>();
                {
                    this.N<SyntaxList<SyntaxNode>>();
                }
            }
            this.T(TokenKind.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithTypeAndWithValue()
    {
        this.ParseDeclaration("val x: int32 = 5;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenKind.Colon);
                this.N<NameTypeSyntax>();
                {
                    this.T(TokenKind.Identifier);
                }
                this.N<ValueSpecifierSyntax>();
                {
                    this.T(TokenKind.Assign);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                }
            }
            this.T(TokenKind.Semicolon);
        }
    }

    [Fact]
    public void TestVariableDeclarationWithMissingTypeAndMissingValue()
    {
        this.ParseDeclaration("val x: =;");
        this.N<VariableDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordVal);
            this.T(TokenKind.Identifier);
            this.N<TypeSpecifierSyntax>();
            {
                this.T(TokenKind.Colon);
                this.N<UnexpectedTypeSyntax>();
                {
                    this.N<SyntaxList<SyntaxNode>>();
                }
                this.N<ValueSpecifierSyntax>();
                {
                    this.T(TokenKind.Assign);
                    this.N<UnexpectedExpressionSyntax>();
                    {
                        this.N<SyntaxList<SyntaxNode>>();
                    }
                    this.T(TokenKind.Semicolon);
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
            this.T(TokenKind.KeywordIf);
            this.T(TokenKind.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
                }
                this.N<SyntaxList<ComparisonElementSyntax>>();
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                }
            }
            this.T(TokenKind.ParenClose);
            this.N<StatementExpressionSyntax>();
            this.N<ExpressionStatementSyntax>();
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenKind.CurlyOpen);
                this.N<SyntaxList<StatementSyntax>>();
                this.N<DeclarationStatementSyntax>();
                {
                    this.N<VariableDeclarationSyntax>();
                    {
                        this.T(TokenKind.KeywordVal);
                        this.T(TokenKind.Identifier);
                        this.N<ValueSpecifierSyntax>();
                        {
                            this.T(TokenKind.Assign);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenKind.LiteralInteger);
                            }
                        }
                        this.T(TokenKind.Semicolon);
                    }
                }
                this.T(TokenKind.CurlyClose);
            }
            this.N<ElseClauseSyntax>();
            {
                this.T(TokenKind.KeywordElse);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<BlockExpressionSyntax>();
                {
                    this.T(TokenKind.CurlyOpen);
                    this.N<SyntaxList<StatementSyntax>>();
                    this.N<DeclarationStatementSyntax>();
                    this.N<VariableDeclarationSyntax>();
                    {
                        this.T(TokenKind.KeywordVal);
                        this.T(TokenKind.Identifier);
                        this.N<ValueSpecifierSyntax>();
                        {
                            this.T(TokenKind.Assign);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenKind.LiteralCharacter);
                            }
                        }
                        this.T(TokenKind.Semicolon);
                    }
                    this.T(TokenKind.CurlyClose);
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
                this.N<SyntaxList<SyntaxNode>>();
                this.T(TokenKind.KeywordElse);
            }
            this.N<ExpressionStatementSyntax>();
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenKind.CurlyOpen);
                this.N<SyntaxList<StatementSyntax>>();
                this.N<DeclarationStatementSyntax>();
                {
                    this.N<VariableDeclarationSyntax>();
                    {
                        this.T(TokenKind.KeywordVal);
                        this.T(TokenKind.Identifier);
                        this.N<ValueSpecifierSyntax>();
                        {
                            this.T(TokenKind.Assign);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenKind.LiteralInteger);
                            }
                        }
                        this.T(TokenKind.Semicolon);
                    }
                }
                this.T(TokenKind.CurlyClose);
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
                this.T(TokenKind.KeywordIf);
                this.T(TokenKind.ParenOpen);
                this.N<RelationalExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                    this.N<SyntaxList<ComparisonElementSyntax>>();
                    this.N<ComparisonElementSyntax>();
                    {
                        this.T(TokenKind.GreaterThan);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralInteger);
                        }
                    }
                }
                this.MissingT(TokenKind.ParenClose);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<BlockExpressionSyntax>();
                {
                    this.T(TokenKind.CurlyOpen);
                    this.N<SyntaxList<StatementSyntax>>();
                    this.N<DeclarationStatementSyntax>();
                    {
                        this.N<VariableDeclarationSyntax>();
                        {
                            this.T(TokenKind.KeywordVal);
                            this.T(TokenKind.Identifier);
                            this.N<ValueSpecifierSyntax>();
                            {
                                this.T(TokenKind.Assign);
                                this.N<LiteralExpressionSyntax>();
                                {
                                    this.T(TokenKind.LiteralInteger);
                                }
                            }
                            this.T(TokenKind.Semicolon);
                        }
                    }
                    this.T(TokenKind.CurlyClose);
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
            this.N<ExpressionStatementSyntax>();
            this.N<IfExpressionSyntax>();
            {
                this.T(TokenKind.KeywordIf);
                this.T(TokenKind.ParenOpen);
                this.N<RelationalExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                    this.N<SyntaxList<ComparisonElementSyntax>>();
                    this.N<ComparisonElementSyntax>();
                    {
                        this.T(TokenKind.GreaterThan);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralInteger);
                        }
                    }
                }
                this.T(TokenKind.ParenClose);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<UnexpectedExpressionSyntax>();
                {
                    this.N<SyntaxList<SyntaxNode>>();
                }
                // TODO: This should be unnecessary
                this.MissingT(TokenKind.Semicolon);
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
            this.T(TokenKind.KeywordIf);
            this.T(TokenKind.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
                }
                this.N<SyntaxList<ComparisonElementSyntax>>();
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                }
            }
            this.T(TokenKind.ParenClose);
            this.N<LiteralExpressionSyntax>();
            {
                this.T(TokenKind.LiteralInteger);
            }
            this.N<ElseClauseSyntax>();
            {
                this.T(TokenKind.KeywordElse);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
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
            this.T(TokenKind.KeywordIf);
            this.T(TokenKind.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
                }
                this.N<SyntaxList<ComparisonElementSyntax>>();
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                }
            }
            this.T(TokenKind.ParenClose);
            this.N<LiteralExpressionSyntax>();
            {
                this.T(TokenKind.LiteralInteger);
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
            this.T(TokenKind.KeywordWhile);
            this.T(TokenKind.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenKind.Identifier);
                }
                this.N<SyntaxList<ComparisonElementSyntax>>();
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.LessThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                }
            }
            this.T(TokenKind.ParenClose);
            this.N<StatementExpressionSyntax>();
            this.N<ExpressionStatementSyntax>();
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenKind.CurlyOpen);
                this.N<SyntaxList<StatementSyntax>>();
                this.N<ExpressionStatementSyntax>();
                {
                    this.N<BinaryExpressionSyntax>();
                    {
                        this.N<NameExpressionSyntax>();
                        {
                            this.T(TokenKind.Identifier);
                        }
                        this.T(TokenKind.Assign);
                        this.N<BinaryExpressionSyntax>();
                        {
                            this.N<NameExpressionSyntax>();
                            {
                                this.T(TokenKind.Identifier);
                            }
                            this.T(TokenKind.Plus);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenKind.LiteralInteger);
                            }
                        }
                        this.T(TokenKind.Semicolon);
                    }
                }
                this.T(TokenKind.CurlyClose);
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
            this.T(TokenKind.KeywordWhile);
            this.T(TokenKind.ParenOpen);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenKind.Identifier);
                }
                this.N<SyntaxList<ComparisonElementSyntax>>();
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.LessThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger);
                    }
                }
            }
            this.MissingT(TokenKind.ParenClose);
            this.N<StatementExpressionSyntax>();
            this.N<ExpressionStatementSyntax>();
            this.N<BlockExpressionSyntax>();
            {
                this.T(TokenKind.CurlyOpen);
                this.N<SyntaxList<StatementSyntax>>();
                this.N<ExpressionStatementSyntax>();
                {
                    this.N<BinaryExpressionSyntax>();
                    {
                        this.N<NameExpressionSyntax>();
                        {
                            this.T(TokenKind.Identifier);
                        }
                        this.T(TokenKind.Assign);
                        this.N<BinaryExpressionSyntax>();
                        {
                            this.N<NameExpressionSyntax>();
                            {
                                this.T(TokenKind.Identifier);
                            }
                            this.T(TokenKind.Plus);
                            this.N<LiteralExpressionSyntax>();
                            {
                                this.T(TokenKind.LiteralInteger);
                            }
                        }
                        this.T(TokenKind.Semicolon);
                    }
                }
                this.T(TokenKind.CurlyClose);
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
            this.N<ExpressionStatementSyntax>();
            this.N<WhileExpressionSyntax>();
            {
                this.T(TokenKind.KeywordWhile);
                this.T(TokenKind.ParenOpen);
                this.N<RelationalExpressionSyntax>();
                {
                    this.N<NameExpressionSyntax>();
                    {
                        this.T(TokenKind.Identifier);
                    }
                    this.N<SyntaxList<ComparisonElementSyntax>>();
                    this.N<ComparisonElementSyntax>();
                    {
                        this.T(TokenKind.LessThan);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralInteger);
                        }
                    }
                }
                this.T(TokenKind.ParenClose);
                this.N<StatementExpressionSyntax>();
                this.N<ExpressionStatementSyntax>();
                this.N<UnexpectedExpressionSyntax>();
                {
                    this.N<SyntaxList<SyntaxNode>>();
                }
                // TODO: This should be unnecessary
                this.MissingT(TokenKind.Semicolon);
            }
        });
    }

    [Fact]
    public void TestLabelDeclarationInGlobalContext()
    {
        this.ParseDeclaration("""
            myLabel:
            """);

        this.N<UnexpectedDeclarationSyntax>();
        this.N<SyntaxList<SyntaxNode>>();
        this.N<LabelDeclarationSyntax>();
        {
            this.T(TokenKind.Identifier, "myLabel");
            this.T(TokenKind.Colon);
        }
    }

    [Fact]
    public void TestLabelDeclaration()
    {
        this.ParseLocalDeclaration("""
            myLabel:
            """);

        this.N<LabelDeclarationSyntax>();
        {
            this.T(TokenKind.Identifier, "myLabel");
            this.T(TokenKind.Colon);
        }
    }

    [Fact]
    public void TestLabelDeclarationNewlineBeforeColon()
    {
        this.ParseLocalDeclaration("""
            myLabel
            :
            """);

        this.N<LabelDeclarationSyntax>();
        {
            this.T(TokenKind.Identifier, "myLabel");
            this.T(TokenKind.Colon);
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
            this.T(TokenKind.KeywordGoto);
            this.N<NameLabelSyntax>();
            {
                this.T(TokenKind.Identifier);
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
                this.T(TokenKind.KeywordGoto);
                this.N<NameLabelSyntax>();
                {
                    this.MissingT(TokenKind.Identifier);
                }
            }
            this.T(TokenKind.Semicolon);
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
            this.T(TokenKind.KeywordReturn);
            this.N<BinaryExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
                }
                this.T(TokenKind.Plus);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger);
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
            this.T(TokenKind.KeywordReturn);
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
            this.T(TokenKind.CurlyOpen);
            this.N<SyntaxList<StatementSyntax>>();
            this.N<DeclarationStatementSyntax>();
            {
                this.N<VariableDeclarationSyntax>();
                {
                    this.T(TokenKind.KeywordVar);
                    this.T(TokenKind.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenKind.Assign);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralInteger);
                        }
                    }
                    this.T(TokenKind.Semicolon);
                }
            }
            this.N<NameExpressionSyntax>();
            {
                this.T(TokenKind.Identifier);
            }
            this.T(TokenKind.CurlyClose);
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
            this.T(TokenKind.CurlyOpen);
            this.N<SyntaxList<StatementSyntax>>();
            this.N<DeclarationStatementSyntax>();
            {
                this.N<VariableDeclarationSyntax>();
                {
                    this.T(TokenKind.KeywordVar);
                    this.T(TokenKind.Identifier);
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenKind.Assign);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralInteger);
                        }
                    }
                    this.T(TokenKind.Semicolon);
                }
            }
            this.T(TokenKind.CurlyClose);
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
            this.T(TokenKind.CurlyOpen);
            this.N<SyntaxList<StatementSyntax>>();
            this.T(TokenKind.CurlyClose);
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
                        this.T(TokenKind.LiteralInteger, "3");
                    }
                    this.T(TokenKind.KeywordMod);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger, "2");
                    }
                }
                this.T(TokenKind.Plus);
                this.N<BinaryExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger, "2");
                    }
                    this.T(TokenKind.Star);
                    this.N<UnaryExpressionSyntax>();
                    {
                        this.T(TokenKind.Minus);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralFloat, "8.6");
                        }
                    }
                }
            }
            this.T(TokenKind.Minus);
            this.N<BinaryExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger, "9");
                }
                this.T(TokenKind.Slash);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger, "3");
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
                    this.T(TokenKind.KeywordTrue);
                }
                this.T(TokenKind.KeywordAnd);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.KeywordFalse);
                }
            }
            this.T(TokenKind.KeywordOr);
            this.N<UnaryExpressionSyntax>();
            {
                this.T(TokenKind.KeywordNot);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.KeywordFalse);
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
                    this.T(TokenKind.LiteralFloat, "3.8");
                }
                this.T(TokenKind.Plus);
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger, "2");
                }
            }
            this.N<SyntaxList<ComparisonElementSyntax>>();
            this.N<ComparisonElementSyntax>();
            {
                this.T(TokenKind.GreaterThan);
                this.N<BinaryExpressionSyntax>();
                {
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger, "2");
                    }
                    this.T(TokenKind.Star);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger, "3");
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
                    this.T(TokenKind.LiteralInteger, "3");
                }
                this.N<SyntaxList<ComparisonElementSyntax>>();
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.GreaterThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralFloat, "2.89");
                    }
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.LessThan);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger, "8");
                    }
                }
            }
            this.T(TokenKind.KeywordOr);
            this.N<RelationalExpressionSyntax>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger, "5");
                }
                this.N<SyntaxList<ComparisonElementSyntax>>();
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.Equal);
                    this.N<LiteralExpressionSyntax>();
                    {
                        this.T(TokenKind.LiteralInteger, "3");
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
            this.T(TokenKind.LineStringStart, "\"");
            this.N<SyntaxList<StringPartSyntax>>();
            this.StringContent("a");
            this.N<InterpolationStringPartSyntax>();
            {
                this.T(TokenKind.InterpolationStart);
                this.N<UnexpectedExpressionSyntax>();
                {
                    this.N<SyntaxList<SyntaxNode>>();
                }
                this.T(TokenKind.InterpolationEnd);
            }
            this.StringContent("b");
            this.T(TokenKind.LineStringEnd, "\"");
        }
    }

    [Fact]
    public void TestFunctionCall()
    {
        this.ParseExpression("""
            foo()
            """);

        this.N<CallExpressionSyntax>();
        {
            this.N<NameExpressionSyntax>();
            {
                this.T(TokenKind.Identifier, "foo");
            }
            this.T(TokenKind.ParenOpen);
            this.N<SeparatedSyntaxList<ExpressionSyntax>>();
            this.T(TokenKind.ParenClose);
        }
    }

    [Fact]
    public void TestDoubleFunctionCall()
    {
        this.ParseExpression("""
            foo()()
            """);

        this.N<CallExpressionSyntax>();
        {
            this.N<CallExpressionSyntax>();
            {
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenKind.Identifier, "foo");
                }
                this.T(TokenKind.ParenOpen);
                this.N<SeparatedSyntaxList<ExpressionSyntax>>();
                this.T(TokenKind.ParenClose);
            }
            this.T(TokenKind.ParenOpen);
            this.N<SeparatedSyntaxList<ExpressionSyntax>>();
            this.T(TokenKind.ParenClose);
        }
    }

    [Fact]
    public void TestGenericCall()
    {
        this.ParseExpression("""
            foo<T>()
            """);

        this.N<CallExpressionSyntax>();
        {
            this.N<GenericExpressionSyntax>();
            {
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenKind.Identifier, "foo");
                }
                this.T(TokenKind.LessThan);
                this.N<SeparatedSyntaxList<TypeSyntax>>();
                {
                    this.N<NameTypeSyntax>();
                    {
                        this.T(TokenKind.Identifier, "T");
                    }
                }
                this.T(TokenKind.GreaterThan);
            }
            this.T(TokenKind.ParenOpen);
            this.N<SeparatedSyntaxList<ExpressionSyntax>>();
            this.T(TokenKind.ParenClose);
        }
    }

    [Fact]
    public void TestGenericCallWithParameter()
    {
        this.ParseExpression("""
            foo<T>(0)
            """);

        this.N<CallExpressionSyntax>();
        {
            this.N<GenericExpressionSyntax>();
            {
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenKind.Identifier, "foo");
                }
                this.T(TokenKind.LessThan);
                this.N<SeparatedSyntaxList<TypeSyntax>>();
                {
                    this.N<NameTypeSyntax>();
                    {
                        this.T(TokenKind.Identifier, "T");
                    }
                }
                this.T(TokenKind.GreaterThan);
            }
            this.T(TokenKind.ParenOpen);
            this.N<SeparatedSyntaxList<ExpressionSyntax>>();
            {
                this.N<LiteralExpressionSyntax>();
                {
                    this.T(TokenKind.LiteralInteger, "0");
                }
            }
            this.T(TokenKind.ParenClose);
        }
    }

    [Fact]
    public void TestRelationalExpressionDisambiguatedFromGenericCall()
    {
        this.ParseExpression("""
            (foo)<T>(0)
            """);

        this.N<RelationalExpressionSyntax>();
        {
            this.N<GroupingExpressionSyntax>();
            {
                this.T(TokenKind.ParenOpen);
                this.N<NameExpressionSyntax>();
                {
                    this.T(TokenKind.Identifier, "foo");
                }
                this.T(TokenKind.ParenClose);
            }
            this.N<SyntaxList<ComparisonElementSyntax>>();
            {
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.LessThan);
                    this.N<NameExpressionSyntax>();
                    {
                        this.T(TokenKind.Identifier, "T");
                    }
                }
                this.N<ComparisonElementSyntax>();
                {
                    this.T(TokenKind.GreaterThan);
                    this.N<GroupingExpressionSyntax>();
                    {
                        this.T(TokenKind.ParenOpen);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralInteger, "0");
                        }
                        this.T(TokenKind.ParenClose);
                    }
                }
            }
        }
    }

    [Fact]
    public void TestEmptyModuleDeclaration()
    {
        this.ParseDeclaration("""
            module Foo { }
            """);

        this.N<ModuleDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordModule);
            this.T(TokenKind.Identifier, "Foo");
        }
    }

    [Fact]
    public void TestModuleDeclarationContainingVariableAndUnexpectedDeclaration()
    {
        this.ParseDeclaration("""
            module Foo
            {
                var bar = 0;
                baz();
            }
            """);

        this.N<ModuleDeclarationSyntax>();
        {
            this.T(TokenKind.KeywordModule);
            this.T(TokenKind.Identifier, "Foo");
            this.T(TokenKind.CurlyOpen);
            this.N<SyntaxList<DeclarationSyntax>>();
            {
                this.N<VariableDeclarationSyntax>();
                {
                    this.T(TokenKind.KeywordVar);
                    this.T(TokenKind.Identifier, "bar");
                    this.N<ValueSpecifierSyntax>();
                    {
                        this.T(TokenKind.Assign);
                        this.N<LiteralExpressionSyntax>();
                        {
                            this.T(TokenKind.LiteralInteger, "0");
                        }
                    }
                    this.T(TokenKind.Semicolon);
                }
                this.N<UnexpectedDeclarationSyntax>();
                {
                    this.N<SyntaxList<SyntaxNode>>();
                    {
                        this.T(TokenKind.Identifier, "baz");
                        this.T(TokenKind.ParenOpen);
                        this.T(TokenKind.ParenClose);
                        this.T(TokenKind.Semicolon);
                    }
                }
            }
            this.T(TokenKind.CurlyClose);
        }
    }

    [Fact]
    public void TestEmptyModuleDeclarationStatement()
    {
        this.ParseStatement("""
            module Foo { }
            """);

        this.N<DeclarationStatementSyntax>();
        {
            this.N<UnexpectedDeclarationSyntax>();
            {
                this.N<SyntaxList<SyntaxNode>>();
                {
                    this.N<ModuleDeclarationSyntax>();
                    {
                        this.T(TokenKind.KeywordModule);
                        this.T(TokenKind.Identifier, "Foo");
                    }
                }
            }
        }
    }
}
