using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
using IInternalScope = Draco.Compiler.Internal.Semantics.Symbols.IScope;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SymbolResolutionTests
{
    private static TSymbol GetInternalSymbol<TSymbol>(ISymbol? symbol)
        where TSymbol : IInternalSymbol
    {
        Assert.NotNull(symbol);
        var symbolBase = (SymbolBase)symbol!;
        return (TSymbol)symbolBase.Symbol;
    }

    private static void AssertParentOf(IInternalScope? parent, IInternalScope? child)
    {
        Assert.NotNull(child);
        Assert.False(ReferenceEquals(parent, child));
        Assert.True(ReferenceEquals(child!.Parent, parent));
    }

    [Fact]
    public void BasicScopeTree()
    {
        // func foo(n: int32) {         // b1
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
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            ImmutableArray.Create(
                FuncParam(Name("n"), NameTypeExpr(Name("int32")))),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x1"))),
                ExprStmt(BlockExpr(
                    DeclStmt(VariableDecl(Name("x2"))),
                    ExprStmt(BlockExpr(DeclStmt(VariableDecl(Name("x3"))))))),
                ExprStmt(BlockExpr(
                    DeclStmt(VariableDecl(Name("x4"))),
                    ExprStmt(BlockExpr(DeclStmt(VariableDecl(Name("x5"))))),
                    ExprStmt(BlockExpr(DeclStmt(VariableDecl(Name("x6")))))))))));

        var foo = tree.FindInChildren<ParseTree.Decl.Func>();
        var n = tree.FindInChildren<ParseTree.FuncParam>();
        var x1 = tree.FindInChildren<ParseTree.Decl.Variable>(0);
        var x2 = tree.FindInChildren<ParseTree.Decl.Variable>(1);
        var x3 = tree.FindInChildren<ParseTree.Decl.Variable>(2);
        var x4 = tree.FindInChildren<ParseTree.Decl.Variable>(3);
        var x5 = tree.FindInChildren<ParseTree.Decl.Variable>(4);
        var x6 = tree.FindInChildren<ParseTree.Decl.Variable>(5);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var symFoo = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(foo));
        var symn = GetInternalSymbol<IInternalSymbol.IParameter>(semanticModel.GetDefinedSymbolOrNull(n));
        var sym1 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x1));
        var sym2 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x2));
        var sym3 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x3));
        var sym4 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x4));
        var sym5 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x5));
        var sym6 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x6));

        // Assert

        AssertParentOf(sym2.DefiningScope, sym3.DefiningScope);
        AssertParentOf(sym1.DefiningScope, sym2.DefiningScope);
        AssertParentOf(sym4.DefiningScope, sym5.DefiningScope);
        AssertParentOf(sym4.DefiningScope, sym6.DefiningScope);
        AssertParentOf(sym1.DefiningScope, sym4.DefiningScope);

        AssertParentOf(symn.DefiningScope, sym1.DefiningScope);

        AssertParentOf(symFoo.DefiningScope, symn.DefiningScope);
        Assert.True(ReferenceEquals(symFoo.DefinedScope, symn.DefiningScope));
    }
}
