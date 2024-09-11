using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
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

    [Fact]
    public void AccessingPrivateStaticMemberIsIllegal()
    {
        var compilation = CreateCompilation("""
            func main() {
                M.foo();
            }

            module M {
                func foo() {}
            }
            """);

        var callSyntax = compilation.SyntaxTrees[0].FindInChildren<CallExpressionSyntax>();
        var declarationSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(1);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
        var diagnostics = compilation.Diagnostics;

        var calledSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(callSyntax));
        var declarationSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(declarationSyntax));

        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, SymbolResolutionErrors.InaccessibleSymbol);
        // We resolve to the real symbol, so not an error
        Assert.False(calledSymbol.IsError);
        Assert.Same(declarationSymbol, calledSymbol);
    }

    [Fact]
    public void AccessingPrivateInstanceMemberIsIllegal()
    {
        var csReference = CompileCSharpToMetadataReference("""
            public class Person
            {
                private int age;
            }
            """);

        var compilation = CreateCompilation("""
            func main() {
                val p = Person();
                val a = p.age;
            }
            """,
            additionalReferences: [csReference]);

        var memberSyntax = compilation.SyntaxTrees[0].FindInChildren<MemberExpressionSyntax>();

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
        var diagnostics = compilation.Diagnostics;

        var accessedSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(memberSyntax));

        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, SymbolResolutionErrors.InaccessibleSymbol);
        // We resolve to the real symbol, so not an error
        Assert.False(accessedSymbol.IsError);
        Assert.Same(accessedSymbol, compilation.WellKnownTypes.SystemInt32);
    }

    [Fact]
    public void AccessingPrivateIndexerIsIllegal()
    {
        var csReference = CompileCSharpToMetadataReference("""
            public class Person
            {
                private int this[int index] => index;
            }
            """);

        var compilation = CreateCompilation("""
            func main() {
                val p = Person();
                val a = p[0];
            }
            """,
            additionalReferences: [csReference]);

        var indexSyntax = compilation.SyntaxTrees[0].FindInChildren<IndexExpressionSyntax>();

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
        var diagnostics = compilation.Diagnostics;

        var indexSymbol = GetInternalSymbol<PropertySymbol>(semanticModel.GetReferencedSymbol(indexSyntax));

        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, SymbolResolutionErrors.InaccessibleSymbol);
        // We resolve to the real symbol, so not an error
        Assert.False(indexSymbol.IsError);
        Assert.True(indexSymbol.IsIndexer);
    }

    [Fact]
    public void AccessingPrivateOverloadIsIllegal()
    {
        var compilation = CreateCompilation("""
            func main() {
                M.overloaded("asd");
            }

            module M {
                func overloaded(x: string) {}
                public func overloaded(n: int32) {}
            }
            """);

        var callSyntax = compilation.SyntaxTrees[0].FindInChildren<CallExpressionSyntax>();
        var stringDeclarationSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(0);
        var int32DeclarationSyntax = compilation.SyntaxTrees[0].FindInChildren<FunctionDeclarationSyntax>(1);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
        var diagnostics = compilation.Diagnostics;

        var calledSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(callSyntax));
        var stringDeclarationSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(stringDeclarationSyntax));
        var int32DeclarationSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(int32DeclarationSyntax));

        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, SymbolResolutionErrors.InaccessibleSymbol);
        // We resolve to the real symbol, so not an error
        Assert.False(calledSymbol.IsError);
        Assert.Same(stringDeclarationSymbol, calledSymbol);
    }
}
