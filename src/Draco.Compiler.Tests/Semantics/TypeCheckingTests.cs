using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class TypeCheckingTests : SemanticTestsBase
{
    [Fact]
    public void LocalVariableExplicitlyTyped()
    {
        // func main() {
        //     var x: int32 = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"), LiteralExpression(0)))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableTypeInferredFromValue()
    {
        // func main() {
        //     var x = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", value: LiteralExpression(0)))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableExplicitlyTypedWithoutValue()
    {
        // func main() {
        //     var x: int32;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableTypeInferredFromLaterAssignment()
    {
        // func main() {
        //     var x;
        //     x = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x")),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0)))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableTypeCanNotBeInferred()
    {
        // func main() {
        //     var x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x"))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CouldNotInferType);
        Assert.True(xSym.Type.IsError);
    }

    [Fact]
    public void LocalVariableIncompatibleType()
    {
        // func main() {
        //     var x: int32 = "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(
                    "x",
                    NameType("int32"),
                    StringExpression("Hello")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
        Assert.False(xSym.Type.IsError);
    }

    [Fact]
    public void LocalVariableIncompatibleTypeInferredFromUsage()
    {
        // func main() {
        //     var x;
        //     x = 0;
        //     x = "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x")),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0))),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, StringExpression("Hello")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Equal(IntrinsicSymbols.Int32, xSym.Type);
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
        Assert.False(xSym.Type.IsError);
    }

    [Fact]
    public void GlobalVariableExplicitlyTyped()
    {
        // var x: int32 = 0;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", NameType("int32"), LiteralExpression(0))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void GlobalVariableTypeInferredFromValue()
    {
        // var x = 0;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", value: LiteralExpression(0))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void GlobalVariableExplicitlyTypedWithoutValue()
    {
        // var x: int32;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", NameType("int32"))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void GlobalVariableTypeCanNotBeInferred()
    {
        // var x;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x")));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CouldNotInferType);
        Assert.True(xSym.Type.IsError);
    }

    [Fact]
    public void GlobalVariableIncompatibleType()
    {
        // var x: int32 = "Hello";

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration(
                "x",
                NameType("int32"),
                StringExpression("Hello"))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
        Assert.False(xSym.Type.IsError);
    }

    [Fact]
    public void BlockBodyFunctionReturnTypeMismatch()
    {
        // func foo(): int32 {
        //     return "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(ReturnExpression(StringExpression("Hello")))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void InlineBodyFunctionReturnTypeMismatch()
    {
        // func foo(): int32 = "Hello";

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            InlineFunctionBody(StringExpression("Hello")))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void IfConditionIsBool()
    {
        // func foo() {
        //     if (true) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(IfExpression(LiteralExpression(true), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void IfConditionIsNotBool()
    {
        // func foo() {
        //     if (1) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(IfExpression(LiteralExpression(1), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void WhileConditionIsBool()
    {
        // func foo() {
        //     while (true) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(WhileExpression(LiteralExpression(true), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void WhileConditionIsNotBool()
    {
        // func foo() {
        //     while (1) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(WhileExpression(LiteralExpression(1), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void IfElseTypeMismatch()
    {
        // func foo() {
        //     var x = if (true) 0 else "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(
                    "x",
                    value: IfExpression(
                        condition: LiteralExpression(true),
                        then: LiteralExpression(0),
                        @else: StringExpression("Hello"))))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    // TODO: Unspecified if we want this
    // There is a strong case against this, since with unit being implicit return type,
    // it's not intuitive to read
#if false
    [Fact]
    public void AllowNonUnitExpressionInInlineFuncReturningUnit()
    {
        // func foo(): int32 = 0;
        // func bar() = foo();

        // Arrange
        var tree = CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0))),
            FunctionDeclaration(
                bar",
                ParameterList(),
                null,
                InlineFunctionBody(CallExpr(NameExpression("foo")))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooDecl = tree.FindInChildren<ParseNode.Decl.Func>(0);
        var barDecl = tree.FindInChildren<ParseNode.Decl.Func>(1);
        var fooSymbol = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbol(fooDecl));
        var barSymbol = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbol(barDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(Internal.Types.Intrinsics.Int32, fooSymbol.ReturnType);
        Assert.Equal(Type.Unit, barSymbol.ReturnType);
    }
#endif

    [Fact]
    public void NeverTypeCompatibility()
    {
        // func foo() {
        // start:
        //     var x = if (true) 0 else return;
        //     var y = if (true) 0 else goto start;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(LabelDeclaration("start")),
                    DeclarationStatement(VariableDeclaration("x", value: IfExpression(LiteralExpression(true), LiteralExpression(0), ReturnExpression()))),
                    DeclarationStatement(VariableDeclaration("y", value: IfExpression(LiteralExpression(true), LiteralExpression(0), GotoExpression("start"))))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var yDecl = tree.FindInChildren<VariableDeclarationSyntax>(1);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var ySym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(yDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(IntrinsicSymbols.Int32, xSym.Type);
        Assert.Equal(IntrinsicSymbols.Int32, ySym.Type);
    }

    [Fact]
    public void NoOverloadForOperator()
    {
        // func foo() {
        //     var x = 1 + "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "x",
                        value: BinaryExpression(LiteralExpression(1), Plus, StringExpression("Hello"))))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Equal(IntrinsicSymbols.ErrorType, xSym.Type);
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
    }

    [Fact]
    public void OneVisibleAndOneNotVisibleOverloadImported()
    {
        // import FooModule;
        // func main(){
        //   foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo")))))),
            ToPath("Tests", "main.draco"));

        // func foo(): int32 = 0;
        // internal func foo(x: string): int32 = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
           FunctionDeclaration(
               "foo",
               ParameterList(),
               NameType("int32"),
               InlineFunctionBody(LiteralExpression(0))),
            FunctionDeclaration(
                Api.Semantics.Visibility.Internal,
                "foo",
                ParameterList(Parameter("x", NameType("string"))),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))),
           ToPath("Tests", "FooModule", "foo.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(main, foo),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
    }

    [Fact]
    public void OneVisibleAndOneNotVisibleOverloadFullyQualified()
    {
        // func main(){
        //   FooModule.foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(MemberExpression(NameExpression("FooModule"), "foo")))))),
            ToPath("Tests", "main.draco"));

        // func foo(): int32 = 0;
        // internal func foo(x: string): int32 = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
           FunctionDeclaration(
               "foo",
               ParameterList(),
               NameType("int32"),
               InlineFunctionBody(LiteralExpression(0))),
            FunctionDeclaration(
                Api.Semantics.Visibility.Internal,
                "foo",
                ParameterList(Parameter("x", NameType("string"))),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))),
           ToPath("Tests", "FooModule", "foo.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(main, foo),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
    }

    [Fact]
    public void OkOverloading()
    {
        // func foo(x: int32) { }
        // func foo(x: bool) { }
        //
        // func main() {
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("bool"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooInt32RefSyntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolDeclSyntax));
        var fooInt32RefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32RefSyntax));
        var fooBoolRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolRefSyntax));

        // Assert
        Assert.Empty(diags);
        Assert.NotSame(fooInt32DeclSym, fooBoolDeclSym);
        Assert.Same(fooInt32DeclSym, fooInt32RefSym);
        Assert.Same(fooBoolDeclSym, fooBoolRefSym);
    }

    [Fact]
    public void OkNestedOverloading()
    {
        // func foo(x: int32) {
        //     func foo(x: bool) { }
        //
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody())),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooInt32RefSyntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolDeclSyntax));
        var fooInt32RefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32RefSyntax));
        var fooBoolRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolRefSyntax));

        // Assert
        Assert.Empty(diags);
        Assert.NotSame(fooInt32DeclSym, fooBoolDeclSym);
        Assert.Same(fooInt32DeclSym, fooInt32RefSym);
        Assert.Same(fooBoolDeclSym, fooBoolRefSym);
    }

    [Fact]
    public void NestedOverloadNotVisibleFromOutside()
    {
        // func foo(x: int32) {
        //     func foo(x: bool) { }
        // }
        //
        // func main() {
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody())))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooInt32RefSyntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolDeclSyntax));
        var fooInt32RefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32RefSyntax));
        var fooBoolRefSym = semanticModel.GetDeclaredSymbol(fooBoolRefSyntax);

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
        Assert.NotSame(fooInt32DeclSym, fooBoolDeclSym);
        Assert.Same(fooInt32DeclSym, fooInt32RefSym);
        Assert.NotSame(fooBoolDeclSym, fooBoolRefSym);
        Assert.NotNull(fooBoolRefSym);
        Assert.True(fooBoolRefSym.IsError);
    }

    [Fact]
    public void IllegalOverloading()
    {
        // func foo(x: int32) { }
        // func foo(x: int32) { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody())));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Equal(2, diags.Length);
        AssertDiagnostic(diags, TypeCheckingErrors.IllegalOverloadDefinition);
    }

    [Fact]
    public void IllegalOverloadingWithGenerics()
    {
        // func foo<T>(x: int32, y: T) { }
        // func foo<T>(x: int32, y: T) { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("int32")), Parameter("y", NameType("T"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "foo",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("int32")), Parameter("y", NameType("T"))),
                null,
                BlockFunctionBody())));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Equal(2, diags.Length);
        AssertDiagnostic(diags, TypeCheckingErrors.IllegalOverloadDefinition);
    }

    [Fact]
    public void IllegalNestedOverloading()
    {
        // func foo(x: int32) {
        //     func foo(x: int32) { }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("int32"))),
                        null,
                        BlockFunctionBody()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.IllegalOverloadDefinition);
    }

    [Fact]
    public void NestedMatchingLocalOverloads()
    {
        // func foo(x: int32) {
        //     func foo(x: bool) { }
        //
        //     foo(0);
        //     foo(true);
        // }
        //
        // func main() {
        //     func foo(x: bool) {}
        //
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody(
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody(
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true))))))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolInFooDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooBoolInMainDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(3);
        var fooInt32Ref1Syntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolInFooRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;
        var fooInt32Ref2Syntax = tree.FindInChildren<CallExpressionSyntax>(2).Function;
        var fooBoolInMainRefSyntax = tree.FindInChildren<CallExpressionSyntax>(3).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolInFooDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInFooDeclSyntax));
        var fooBoolInMainDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInMainDeclSyntax));
        var fooInt32Ref1Sym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32Ref1Syntax));
        var fooInt32Ref2Sym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32Ref2Syntax));
        var fooBoolInFooRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInFooRefSyntax));
        var fooBoolInMainRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInMainRefSyntax));

        // Assert
        Assert.Empty(diags);
        Assert.Same(fooInt32DeclSym, fooInt32Ref1Sym);
        Assert.Same(fooInt32DeclSym, fooInt32Ref2Sym);
        Assert.Same(fooBoolInFooDeclSym, fooBoolInFooRefSym);
        Assert.Same(fooBoolInMainDeclSym, fooBoolInMainRefSym);
        Assert.NotSame(fooBoolInFooDeclSym, fooBoolInMainDeclSym);
    }

    [Fact]
    public void AccessingField()
    {
        // func main() {
        //     import System;
        //     var x = String.Empty;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("System")),
                DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("String"), "Empty")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var consoleRef = tree.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
             syntaxTrees: ImmutableArray.Create(tree),
             metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes))).ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var stringEmptySym = GetMemberSymbol<FieldSymbol>(GetInternalSymbol<TypeSymbol>(semanticModel.GetReferencedSymbol(consoleRef)), "Empty");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.String);
        Assert.Equal(stringEmptySym.Type, IntrinsicSymbols.String);
    }

    [Fact]
    public void AccessingProperty()
    {
        // func main() {
        //     import System;
        //     var x = Console.WindowWidth;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("System")),
                DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("Console"), "WindowWidth")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var consoleRef = tree.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
             syntaxTrees: ImmutableArray.Create(tree),
             metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes))).ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var windowWidthSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(consoleRef)), "WindowWidth");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
        Assert.Equal(windowWidthSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void AccessingIndexer()
    {
        // func main() {
        //     import System.Collections.Generic;
        //     var list = List<int32>();
        //     var x = list[0];
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("System", "Collections", "Generic")),
                DeclarationStatement(VariableDeclaration("list", null, CallExpression(GenericExpression(NameExpression("List"), NameType("int32"))))),
                DeclarationStatement(VariableDeclaration("x", null, IndexExpression(NameExpression("list"), LiteralExpression(0))))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(1);
        var listRef = tree.FindInChildren<IndexExpressionSyntax>(0).Indexed;

        // Act
        var compilation = Compilation.Create(
             syntaxTrees: ImmutableArray.Create(tree),
             metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes))).ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var indexSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(listRef)).Type, "Item");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
        Assert.Equal(indexSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void IllegalCallToNonFunctionType()
    {
        // func foo() {
        //     var a = 0;
        //     a();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("a", null, LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("a")))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CallNonFunction);
    }

    [Fact]
    public void ExplicitGenericFunction()
    {
        // func identity<T>(x: T): T = x;
        //
        // func main() {
        //     var a = identity<int32>(1);
        //     var b = identity<string>("foo");
        //     var c = identity<int32>("foo");
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "identity",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("T"))),
                NameType("T"),
                InlineFunctionBody(NameExpression("x"))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "a",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("int32")),
                            LiteralExpression(0)))),
                    DeclarationStatement(VariableDeclaration(
                        "b",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("string")),
                            StringExpression("foo")))),
                    DeclarationStatement(VariableDeclaration(
                        "c",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("int32")),
                            StringExpression("foo"))))))));

        var identitySyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var firstCallSyntax = tree.FindInChildren<CallExpressionSyntax>(0);

        var aSyntax = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var bSyntax = tree.FindInChildren<VariableDeclarationSyntax>(1);
        var cSyntax = tree.FindInChildren<VariableDeclarationSyntax>(2);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var identitySym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(identitySyntax));
        var firstCalledSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(firstCallSyntax.Function));

        var aSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(aSyntax));
        var bSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(bSyntax));
        var cSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(cSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        // NOTE: This might not be the best error...
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);

        Assert.True(identitySym.IsGenericDefinition);
        Assert.True(firstCalledSym.IsGenericInstance);
        Assert.False(firstCalledSym.IsGenericDefinition);
        Assert.Same(identitySym, firstCalledSym.GenericDefinition);
        Assert.Single(firstCalledSym.GenericArguments);
        Assert.Same(IntrinsicSymbols.Int32, firstCalledSym.GenericArguments[0]);

        Assert.Same(IntrinsicSymbols.Int32, aSym.Type);
        Assert.Same(IntrinsicSymbols.String, bSym.Type);
        Assert.True(cSym.Type.IsError);
    }

    [Fact]
    public void ExplicitGenericFunctionWithWrongNumberOfArgs()
    {
        // func identity<T>(x: T): T = x;
        //
        // func main() {
        //     var a = identity<int32, int32>(1);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "identity",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("T"))),
                NameType("T"),
                InlineFunctionBody(NameExpression("x"))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "a",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("int32"), NameType("int32")),
                            LiteralExpression(0))))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoGenericFunctionWithParamCount);
    }

    [Fact]
    public void InstantiateIllegalConstruct()
    {
        // func main() {
        //     var a = 0;
        //     a<int32>()
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("a", null, LiteralExpression(0))),
                    ExpressionStatement(CallExpression(GenericExpression(NameExpression("a"), NameType("int32"))))))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NotGenericConstruct);
    }

    [Fact]
    public void ExplicitGenericTypeWithWrongNumberOfArgs()
    {
        // import System.Collections.Generic;
        // var l: List<int32, int32>;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Generic"),
            VariableDeclaration(
                "l",
                GenericType(NameType("List"), NameType("int32"), NameType("int32")))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.GenericTypeParamCountMismatch);
    }

    [Fact]
    public void InferGenericFunctionParameterTypeFromUse()
    {
        // func identity<T>(x: T): T = x;
        //
        // func main() {
        //     var x = identity(0);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "identity",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("T"))),
                NameType("T"),
                InlineFunctionBody(NameExpression("x"))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "x",
                        null,
                        CallExpression(NameExpression("identity"), LiteralExpression(0))))))));

        var callSyntax = tree.FindInChildren<CallExpressionSyntax>();
        var varSyntax = tree.FindInChildren<VariableDeclarationSyntax>();

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var calledSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(callSyntax.Function));
        var varSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(varSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.True(calledSym.IsGenericInstance);
        Assert.Single(calledSym.GenericArguments);
        Assert.Equal(IntrinsicSymbols.Int32, calledSym.GenericArguments[0], SymbolEqualityComparer.Default);
        Assert.Equal(IntrinsicSymbols.Int32, varSym.Type, SymbolEqualityComparer.Default);
    }

    [Fact]
    public void InferGenericCollectionTypeFromUse()
    {
        // import System.Collections.Generic;
        //
        // func main() {
        //     var s = Stack();
        //     s.Push(0);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Generic"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("s", null, CallExpression(NameExpression("Stack")))),
                    ExpressionStatement(CallExpression(
                        MemberExpression(NameExpression("s"), "Push"),
                        LiteralExpression(0)))))));

        var callSyntax = tree.FindInChildren<CallExpressionSyntax>();
        var varSyntax = tree.FindInChildren<VariableDeclarationSyntax>();

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var calledSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(callSyntax.Function));
        var stackSym = calledSym.ReturnType;
        var varSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(varSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.True(calledSym.IsGenericInstance);
        Assert.True(stackSym.IsGenericInstance);
        Assert.Equal(IntrinsicSymbols.Int32, calledSym.GenericArguments[0], SymbolEqualityComparer.Default);
        Assert.Equal(IntrinsicSymbols.Int32, stackSym.GenericArguments[0], SymbolEqualityComparer.Default);
    }

    [Fact]
    public void CanNotInferGenericCollectionTypeFromUse()
    {
        // import System.Collections.Generic;
        //
        // func main() {
        //     var s = Stack();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Generic"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("s", null, CallExpression(NameExpression("Stack"))))))));

        var callSyntax = tree.FindInChildren<CallExpressionSyntax>();
        var varSyntax = tree.FindInChildren<VariableDeclarationSyntax>();

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.InferenceIncomplete);
    }

    [Fact]
    public void ExactMatchOverloadPriorityOverGeneric()
    {
        // func identity<T>(x: T): T = x;
        // func identity(x: int32): int32 = x;
        //
        // func main() {
        //     identity(0);
        //     identity<int32>(0);
        //     identity(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "identity",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("T"))),
                NameType("T"),
                InlineFunctionBody(NameExpression("x"))),
            FunctionDeclaration(
                "identity",
                ParameterList(Parameter("x", NameType("int32"))),
                NameType("int32"),
                InlineFunctionBody(NameExpression("x"))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("identity"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(
                        GenericExpression(NameExpression("identity"), NameType("int32")),
                        LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("identity"), LiteralExpression(true)))))));

        var genericIdentitySyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var int32IdentitySyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var call1Syntax = tree.FindInChildren<CallExpressionSyntax>(0);
        var call2Syntax = tree.FindInChildren<CallExpressionSyntax>(1);
        var call3Syntax = tree.FindInChildren<CallExpressionSyntax>(2);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var genericIdentitySym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(genericIdentitySyntax));
        var int32IdentitySym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(int32IdentitySyntax));
        var call1Sym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call1Syntax.Function));
        var call2Sym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call2Syntax.Function));
        var call3Sym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call3Syntax.Function));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.Equal(int32IdentitySym, call1Sym);
        Assert.True(call2Sym.IsGenericInstance);
        Assert.True(call3Sym.IsGenericInstance);
        Assert.Equal(genericIdentitySym, call2Sym.GenericDefinition);
        Assert.Equal(genericIdentitySym, call3Sym.GenericDefinition);
    }

    [Fact]
    public void AmbiguousOverload()
    {
        // func foo<T>(x: T, y: int32) {}
        // func foo<T>(x: int32, y: T) {}
        //
        // func main() {
        //     foo(0, 0);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("T")), Parameter("y", NameType("int32"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "foo",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("int32")), Parameter("y", NameType("T"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0), LiteralExpression(0)))))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.AmbiguousOverloadedCall);
    }

    [Fact]
    public void AssignDerivedTypeToBaseType()
    {
        // import System;
        // func foo()
        // {
        //   var x: Object = Random();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System"),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", NameType("Object"), CallExpression(NameExpression("Random"))))))));

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(main),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);
        var wellKnownTypes = new WellKnownTypes(compilation);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(xDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(wellKnownTypes.SystemObject, xSym.Type);
    }

    [Fact]
    public void AssignBaseTypeToDerivedType()
    {
        // import System;
        // func foo()
        // {
        //   var x: String = Object();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System"),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", NameType("String"), CallExpression(NameExpression("Object"))))))));

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(main),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);
        var wellKnownTypes = new WellKnownTypes(compilation);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
        Assert.Equal(wellKnownTypes.SystemString, xSym.Type);
    }

    [Fact]
    public void SuplyDerivedTypeToBaseTypeParameter()
    {
        // import System;
        // func foo()
        // {
        //   bar(Random());
        // }
        //
        // func bar(x: Object) { }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System"),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("bar"), CallExpression(NameExpression("Random")))))),
            FunctionDeclaration(
                "bar",
                ParameterList(Parameter("x", NameType("Object"))),
                null,
                BlockFunctionBody())));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(main),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);
        var wellKnownTypes = new WellKnownTypes(compilation);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void SuplyBaseTypeToDerivedTypeParameter()
    {
        // import System;
        // func foo()
        // {
        //   bar(Object());
        // }
        //
        // func bar(x: String) { }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System"),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("bar"), CallExpression(NameExpression("Object")))))),
            FunctionDeclaration(
                "bar",
                ParameterList(Parameter("x", NameType("String"))),
                null,
                BlockFunctionBody())));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(main),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);
        var wellKnownTypes = new WellKnownTypes(compilation);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
    }

    [Fact]
    public void IfStatementCommonTypeResult()
    {
        // import System;
        // func foo()
        // {
        //   var x = if(true) Random() else Object();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System"),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", null, IfExpression(LiteralExpression(true), CallExpression(NameExpression("Random")), CallExpression(NameExpression("Object")))))))));

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(main),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);
        var wellKnownTypes = new WellKnownTypes(compilation);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(xDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(wellKnownTypes.SystemObject, xSym.Type);
    }
}
