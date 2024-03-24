using Draco.Compiler.Api.Syntax;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Xunit.Abstractions;

namespace Draco.Compiler.Tests.Syntax;

public sealed class SyntaxTreeFormatterTests
{
    private readonly ITestOutputHelper logger;

    public SyntaxTreeFormatterTests(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    [Fact]
    public void TestFormatting()
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
        Console.WriteLine(actual);
        this.logger.WriteLine(actual);
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TestFormattingInlineMethod()
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

            func max(a:int32, b:int32): int32 = if (a > b) a else b;

            func main() {
                WriteLine(max(12, 34));
            }

            """;
        var actual = SyntaxTree.Parse(input).Format();
        Console.WriteLine(actual);
        this.logger.WriteLine(actual);
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TestFoldExpression()
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
        Console.WriteLine(actual);
        this.logger.WriteLine(actual);
        this.logger.WriteLine(expected);
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }
}
