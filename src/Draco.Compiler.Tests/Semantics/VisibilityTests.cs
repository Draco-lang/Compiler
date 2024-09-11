using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class VisibilityTests
{
    [Fact]
    public void NestedModulePrivateMemberVisibility()
    {
        var compilation = CreateCompilation("""
            func foo() {}

            module M {
                func bar() {}
            }
            """);

        var fooSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(0);
        var barSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(1);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);

        var fooSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(fooSyntax));
        var barSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(barSyntax));

        Assert.True(fooSymbol.IsVisibleFrom(barSymbol));
        Assert.False(barSymbol.IsVisibleFrom(fooSymbol));
    }

    [Fact]
    public void NestedModuleInternalMemberVisibility()
    {
        var compilation = CreateCompilation("""
            func foo() {}

            module M {
                internal func bar() {}
            }
            """);

        var fooSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(0);
        var barSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(1);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);

        var fooSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(fooSyntax));
        var barSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(barSyntax));

        Assert.True(fooSymbol.IsVisibleFrom(barSymbol));
        Assert.True(barSymbol.IsVisibleFrom(fooSymbol));
    }

    [Fact]
    public void MutuallyExclusivePrivateMemberVisibility()
    {
        var compilation = CreateCompilation("""
            module A {
                func foo() {}
            }

            module B {
                func bar() {}
            }
            """);

        var fooSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(0);
        var barSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(1);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);

        var fooSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(fooSyntax));
        var barSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(barSyntax));

        Assert.False(fooSymbol.IsVisibleFrom(barSymbol));
        Assert.False(barSymbol.IsVisibleFrom(fooSymbol));
    }

    [Fact]
    public void MutuallyExclusiveInternalMemberVisibility()
    {
        var compilation = CreateCompilation("""
            module A {
                internal func foo() {}
            }

            module B {
                func bar() {}
            }
            """);

        var fooSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(0);
        var barSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(1);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);

        var fooSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(fooSyntax));
        var barSymbol = GetInternalSymbol<Symbol>(semanticModel.GetDeclaredSymbol(barSyntax));

        Assert.True(fooSymbol.IsVisibleFrom(barSymbol));
        Assert.False(barSymbol.IsVisibleFrom(fooSymbol));
    }
}
