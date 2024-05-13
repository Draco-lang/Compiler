using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Syntax.Formatting;
using Xunit.Abstractions;

namespace Draco.Compiler.Tests.Syntax;

public sealed class SyntaxTreeFormatterTests(ITestOutputHelper logger)
{
    [Fact]
    public void SomeCodeSampleShouldBeFormattedCorrectly()
    {
        var input = """"
             func  main  ( )  {

            var   x   :  int32   = 5+

              4  + 5  ;

             val singleLineString =   ""  ;
                    var   multilineString   =  #"""
                    something
                    test
                """# ;
                val  y
                =   4-2
                mod   4+3;
              while(true){
             x = 7 ;
            var t = 4;
            x.Function();
              }
                 val  x = 4;
             var t   = 7    ;
               if(x > t){
             myLabel:
                val x = if
                  (t ==5)3 else 4 ;
               } else{
                var s = 4/1*  6 ;
               }
            {
            val  z
            = 4 ;
               }
              while  (t  < 5  ) x =
                 4;
             if  ( x >=  7 ) t  =4; else t  = 3
             ;
               var a = {
               0
            };
            goto
               myLabel ;
             return   x;
            }
            """";

        var expected = """"
            func main() {
                var x: int32 = 5 + 4 + 5;
                val singleLineString = "";
                var multilineString = #"""
                        something
                        test
                    """#;
                val y = 4 - 2 mod 4 + 3;
                while (true) {
                    x = 7;
                    var t = 4;
                    x.Function();
                }
                val x = 4;
                var t = 7;
                if (x > t) {
                myLabel:
                    val x = if (t == 5) 3 else 4;
                }
                else {
                    var s = 4 / 1 * 6;
                }
                {
                    val z = 4;
                }
                while (t < 5) x = 4;
                if (x >= 7) t = 4;
                else t = 3;
                var a = {
                    0
                };
                goto myLabel;
                return x;
            }

            """";

        var actual = SyntaxTree.Parse(input).Format();
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void InlineMethodShouldBeFormattedCorrectly()
    {
        var input = """
            import System.Console;

            func max(a:int32, b:int32): int32 = if (a > b) a else b;

            func main() {
                WriteLine(max(12, 34));
            }
            """;

        var expected = """
            import System.Console;

            func max(a: int32, b: int32): int32 = if (a > b) a else b;

            func main() {
                WriteLine(max(12, 34));
            }

            """;
        var actual = SyntaxTree.Parse(input).Format();
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void SimpleExpressionShouldBeFormattedCorrectly()
    {
        var input = """
            func aLongMethodName() = 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10;
            """;
        var expected = """
            func aLongMethodName() = 1
                                   + 2
                                   + 3
                                   + 4
                                   + 5
                                   + 6
                                   + 7
                                   + 8
                                   + 9
                                   + 10;

            """;
        var actual = SyntaxTree.Parse(input).Format(new Internal.Syntax.Formatting.FormatterSettings()
        {
            LineWidth = 60
        });
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ExpressionInMultiLineStringDoesNotChange()
    {
        var input = """"
            func main() {
                val someMultiLineString = """
                    the result:\{1 + 2 + 3 + 4 + 5
                    + 6 + 7 + 8 + 9 + 10}
                    """;
            }

            """";
        var actual = SyntaxTree.Parse(input).Format(new Internal.Syntax.Formatting.FormatterSettings()
        {
            LineWidth = 50
        });
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void MultiReturnInMultiLineStringArePreserved()
    {
        var input = """"
            func main() {
                val someMultiLineString = """
                    bla bla


                    bla bla

                    """;
            }

            """";
        var actual = SyntaxTree.Parse(input).Format();
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void MultiReturnInMultiLineStringArePreserved2()
    {
        var input = """"
            func main() {
                val someMultiLineString = """
                    bla bla


                    bla bla


                    """;
            }

            """";
        var actual = SyntaxTree.Parse(input).Format();
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void IfElseChainFormatsCorrectly()
    {
        var input = """"
            func main() {
                if (false)
                expr1
                else if (false)
                    expr2
            else if (false) expr3
                else    expr4
            }
            """";
        var expected = """"
            func main() {
                if (false) expr1
                else if (false) expr2
                else if (false) expr3
                else expr4
            }

            """";
        var actual = SyntaxTree.Parse(input).Format();
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TooLongArgsFoldsInsteadOfExpr()
    {
        var input = """
            func main(lots: Of, arguments: That, will: Be, fold: But) = nnot + this;
            """;
        var expected = """
            func main(
                lots: Of,
                arguments: That,
                will: Be,
                fold: But) = nnot + this;

            """;
        var actual = SyntaxTree.Parse(input).Format(new Internal.Syntax.Formatting.FormatterSettings()
        {
            LineWidth = 60
        });
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NoLineReturnInSingleLineString()
    {
        var input = """"
            func main() {
                val value = "Value: \{if (input < value) "low" else "high"}";
            }

            """";
        var actual = SyntaxTree.Parse(input).Format(new Internal.Syntax.Formatting.FormatterSettings()
        {
            LineWidth = 10
        });
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Sample1()
    {
        var input = """"
            import System;
            import System.Console;

            func main() {
                val value = Random.Shared.Next(1, 101);
                while (true) {
                    Write("Guess a number (1-100): ");
                    val input = Convert.ToInt32(ReadLine());
                    if (input == value) goto break;
                    WriteLine("Incorrect. Too \{if (input < value) "low" else "high"}");
                }
                WriteLine("You guessed it!");
            }

            """";
        var actual = SyntaxTree.Parse(input).Format(new Internal.Syntax.Formatting.FormatterSettings()
        {
            LineWidth = 50
        });
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void CursedSample()
    {
        var input = """"
            //test
            func //test
                main
                // another
                // foobar
                () {
                var
                    // test
                    opponent = "R";
                var me = "P";
                if // heh
                    (me == opponent) return println("draw");
                if (
                    // heh
                    me == "R") {
                    println(if (opponent == "P") "lose" else "win");
                }
                else if ( // heh
                    me == "P") {
                    println(if (opponent == "R") "win" else "lose");
                }
                else if (me == "S") {
                    println(if (opponent == "P") "win" else "lose");
                }
            } // oh hello
            // oops.

            """";
        var actual = SyntaxTree.Parse(input).Format();
        logger.WriteLine(actual);
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void CursedSample2()
    {
        var input = """"
            func foo(a: int32) {
                if ({
                    var x = a * 2;
                    x > 50
                }) {
                    WriteLine("ohno");
                }
            }

            """";
        var actual = SyntaxTree.Parse(input).Format();
        logger.WriteLine(actual);
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void DontMergeTokens()
    {
        var input = """"
            func foo() {
                var x
                + =
                1;
            }

            """";

        var actual = SyntaxTree.Parse(input).Format(new FormatterSettings()
        {
            SpaceAroundBinaryOperators = false
        });
        logger.WriteLine(actual);
        Assert.Equal(input, actual, ignoreLineEndingDifferences: true);
    }
}
