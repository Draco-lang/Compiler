using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SymbolResolutionTests
{
    [Fact]
    public void BasicScopeTree()
    {
        // func foo() {         // b1
        //     var x1;
        //     {                // b2
        //         var x2;
        //         { var x3; }  // b3
        //     }
        //     {                // b4
        //         var x4;
        //         { var x5; }  // b5
        //         { var x6; }  // b6
        //     }
        // }

        // Arrange
        var x1 = VariableDecl(Name("x1"));
        var x2 = VariableDecl(Name("x2"));
        var x3 = VariableDecl(Name("x3"));
        var x4 = VariableDecl(Name("x4"));
        var x5 = VariableDecl(Name("x5"));
        var x6 = VariableDecl(Name("x6"));

        var b3 = BlockExpr(DeclStmt(x3));
        var b5 = BlockExpr(DeclStmt(x5));
        var b6 = BlockExpr(DeclStmt(x6));

        var b2 = BlockExpr(DeclStmt(x2), ExprStmt(b3));
        var b4 = BlockExpr(DeclStmt(x5), ExprStmt(b5), ExprStmt(b6));

        var b1 = BlockExpr(DeclStmt(x1), ExprStmt(b2), ExprStmt(b4));

        var funcFoo = FuncDecl(
            Name("foo"),
            ImmutableArray.Create<ParseTree.FuncParam>(),
            null,
            BlockBodyFuncBody(b1));
        var tree = CompilationUnit(funcFoo);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var sym1 = semanticModel.GetDefinedSymbolOrNull(x1);
        var sym2 = semanticModel.GetDefinedSymbolOrNull(x2);
        var sym3 = semanticModel.GetDefinedSymbolOrNull(x3);
        var sym4 = semanticModel.GetDefinedSymbolOrNull(x4);
        var sym5 = semanticModel.GetDefinedSymbolOrNull(x5);
        var sym6 = semanticModel.GetDefinedSymbolOrNull(x6);

        // Assert
        Assert.NotNull(sym1);
        Assert.NotNull(sym2);
        Assert.NotNull(sym3);
        Assert.NotNull(sym4);
        Assert.NotNull(sym5);
        Assert.NotNull(sym6);

        Assert.False(sym1!.IsError);
        Assert.False(sym2!.IsError);
        Assert.False(sym3!.IsError);
        Assert.False(sym4!.IsError);
        Assert.False(sym5!.IsError);
        Assert.False(sym6!.IsError);
    }
}
