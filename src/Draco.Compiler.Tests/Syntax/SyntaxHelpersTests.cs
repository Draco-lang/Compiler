using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.Syntax;
public class SyntaxHelpersTests
{

    [Fact]
    public void TestIfElseIfExpression()
    {
        var res = SyntaxTree.Parse("func main() { var a = if (true) 1 else if (false) 2 else 3 }");
        var statement = (((res.Root as CompilationUnitSyntax)!.Declarations.Single() as FunctionDeclarationSyntax)!.Body as BlockFunctionBodySyntax)!.Statements.Single() as DeclarationStatementSyntax;
        var variableDeclaration = statement!.Declaration as VariableDeclarationSyntax;
        var ifExpression = variableDeclaration!.Value!.Value as IfExpressionSyntax;
        Assert.True(ifExpression!.Else!.IsElseIf);
    }
}
