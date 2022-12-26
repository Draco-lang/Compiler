using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using static Draco.Compiler.Internal.Syntax.Lexer;

namespace Draco.Compiler.Tests.Semantics;

public sealed class AstTests
{
    private Ast.CompilationUnit? GetAst(string sourceCode)
    {
        var parseTree = ParseTree.Parse(sourceCode);
        var compilation = Compilation.Create(parseTree);
        var db = compilation.GetSemanticModel().QueryDatabase;
        return AstBuilder.ToAst(db, parseTree) as Ast.CompilationUnit;
    }

    [Theory]
    [InlineData("/// One Line Doc Comment")]
    [InlineData("""
        /// Two Lines Doc Comment
        /// Second Line
        """)]
    [InlineData("""
        /// Two Lines Doc Comment with trivia between
        // normal comment
        /// Second Line
        """)]
    public void TestDocumentationComments(string docComment)
    {
        var code = $$"""
            {{docComment}}
            func main(){}
            """;
        var ast = this.GetAst(code);
        Assert.NotNull(ast);
        Assert.Single(ast!.Declarations);
        Assert.IsType<Ast.Decl.Func>(ast!.Declarations[0]);
        var func = ast!.Declarations[0] as Ast.Decl.Func;
        Assert.NotNull(func!.Documentation);
        var docCommentsExpected = docComment.Split(
            new string[] { "\r\n", "\r", "\n" },
            StringSplitOptions.None
            ).Where(x => x.StartsWith("///")).Select(x => x.TrimStart('/')).ToArray();
        var docCommentsActuall = func.Documentation!.Split(
            new string[] { "\r\n", "\r", "\n" },
            StringSplitOptions.None
            );
        Assert.Equal(docCommentsExpected.Length, docCommentsActuall.Length);
        for (int i = 0; i < docCommentsExpected.Length; i++)
        {
            Assert.Equal(docCommentsExpected[i], docCommentsActuall[i]);
        }
    }
}
