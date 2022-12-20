using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.Syntax;

public sealed class ParseTreeFormatterTests
{
    [Fact]
    public void TestFormatting()
    {
        var input = """"
                myLabelNonIndented:
             func  main  ( )  {   
                    
            var   x   :  int32   = 5+ 

              4  + 5  ;

             val singleLineString =   ""  ;
                    var   multilineString   =  #"""
                    something
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
            goto   
               myLabel ;
             return   x;
            }
            """";

        var expected = """"
            myLabelNonIndented:
            func main() {
                var x: int32 = 5 + 4 + 5;
                val singleLineString = "";
                var multilineString = #"""
                    something
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
                if (x >= 7) t = 4; else t = 3;
                goto myLabel;
                return x;
            }

            """";

        var actual = ParseTree.Parse(input).Format().ToString();

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }
}
