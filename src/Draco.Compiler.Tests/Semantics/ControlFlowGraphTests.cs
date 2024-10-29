using DiffEngine;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols.Source;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

// Test the building of CFGs itself
public sealed class ControlFlowGraphTests
{
    private readonly VerifySettings settings = new();

    public ControlFlowGraphTests()
    {
        DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.VisualStudio, DiffTool.Rider);

        this.settings.UseDirectory("ControlFlowGraphs");
    }

    private static IControlFlowGraph FunctionToCfg(SyntaxTree tree, string? name = null)
    {
        var functionSyntax = tree.GetNode<FunctionDeclarationSyntax>(predicate: decl => name is null || decl.Name.Text == name);
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var functionSymbol = GetInternalSymbol<SourceFunctionSymbol>(semanticModel.GetDeclaredSymbol(functionSyntax));
        return ControlFlowGraphBuilder.Build(functionSymbol.Body);
    }

    [Fact]
    public async Task EmptyMethod()
    {
        // Arrange
        // func main() {}
        var program = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody())));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }

    [Fact]
    public async Task UnconditionalBackwardsJump()
    {
        // Arrange
        // func main() {
        // loop:
        //    goto loop;
        // }
        var program = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(LabelDeclaration("loop")),
                ExpressionStatement(GotoExpression(NameLabel("loop")))))));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }

    [Fact]
    public async Task IfElse()
    {
        // Arrange
        // func foo(b: bool) {
        //     if (b) bar(); else baz();
        // }
        // func bar() {}
        // func baz() {}
        var program = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(Parameter("b", NameType("bool"))),
                null,
                BlockFunctionBody(
                    ExpressionStatement(IfExpression(
                        NameExpression("b"),
                        CallExpression(NameExpression("bar")),
                        CallExpression(NameExpression("baz")))))),
            FunctionDeclaration("bar", ParameterList(), null, BlockFunctionBody()),
            FunctionDeclaration("baz", ParameterList(), null, BlockFunctionBody())));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }

    [Fact]
    public async Task WhileLoop()
    {
        // Arrange
        // func foo() {
        //     var i = 0;
        //     while (i < 10) {
        //        bar();
        //        i += 1;
        //     }
        // }
        // func bar() {}
        var program = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VarDeclaration("i", null, LiteralExpression(0))),
                    ExpressionStatement(WhileExpression(
                        RelationalExpression(NameExpression("i"), ComparisonElement(LessThan, LiteralExpression(10))),
                        BlockExpression(
                            ExpressionStatement(CallExpression(NameExpression("bar"))),
                            ExpressionStatement(BinaryExpression(NameExpression("i"), PlusAssign, LiteralExpression(1)))))))),
            FunctionDeclaration("bar", ParameterList(), null, BlockFunctionBody())));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }

    [Fact]
    public async Task EarlyReturn()
    {
        // Arrange
        // func foo(b: bool) {
        //    bar();
        //    if (b) return;
        //    baz();
        // }
        // func bar() {}
        // func baz() {}
        var program = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(Parameter("b", NameType("bool"))),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("bar"))),
                    ExpressionStatement(IfExpression(
                        NameExpression("b"),
                        ReturnExpression())),
                    ExpressionStatement(CallExpression(NameExpression("baz"))))),
            FunctionDeclaration("bar", ParameterList(), null, BlockFunctionBody()),
            FunctionDeclaration("baz", ParameterList(), null, BlockFunctionBody())));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }

    [Fact]
    public async Task ConditionalGotoInAssignment()
    {
        // Arrange
        // func foo(b: bool) {
        //     val x = if (b) goto noassign else 1;
        // noassign:
        //     bar();
        // }
        // func bar() {}
        var program = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(Parameter("b", NameType("bool"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VarDeclaration("x", null, IfExpression(
                        NameExpression("b"),
                        GotoExpression(NameLabel("noassign")),
                        LiteralExpression(1)))),
                    DeclarationStatement(LabelDeclaration("noassign")),
                    ExpressionStatement(CallExpression(NameExpression("bar"))))),
            FunctionDeclaration("bar", ParameterList(), null, BlockFunctionBody())));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }

    [Fact]
    public async Task ForLoop()
    {
        // Arrange
        // func foo(s: Array<int32>) {
        //     for (i in s) bar(i);
        // }
        // func bar(x: int32) {}
        var program = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(Parameter("s", GenericType(NameType("Array"), NameType("int32")))),
                null,
                BlockFunctionBody(
                    ExpressionStatement(ForExpression(
                        "i",
                        NameExpression("s"),
                        CallExpression(NameExpression("bar"), NameExpression("i")))))),
            FunctionDeclaration("bar", ParameterList(Parameter("x", NameType("int32"))), null, BlockFunctionBody())));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }

    [Fact]
    public async Task ChainedComparison()
    {
        // Arrange
        // func foo(a: int32, b: int32, c: int32, d: int32): bool =
        //     { bar(); a } < { baz(); b } == { qux(); c } > { qwe(); d };
        // 
        // func bar() {}
        // func baz() {}
        // func qux() {}
        // func qwe() {}
        var program = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(
                    Parameter("a", NameType("int32")),
                    Parameter("b", NameType("int32")),
                    Parameter("c", NameType("int32")),
                    Parameter("d", NameType("int32"))),
                null,
                InlineFunctionBody(RelationalExpression(
                    BlockExpression([ExpressionStatement(CallExpression(NameExpression("bar")))], NameExpression("a")),
                    ComparisonElement(LessThan, BlockExpression([ExpressionStatement(CallExpression(NameExpression("baz")))], NameExpression("b"))),
                    ComparisonElement(Equal, BlockExpression([ExpressionStatement(CallExpression(NameExpression("qux")))], NameExpression("c"))),
                    ComparisonElement(GreaterThan, BlockExpression([ExpressionStatement(CallExpression(NameExpression("qwe")))], NameExpression("d")))))),
            FunctionDeclaration("bar", ParameterList(), null, BlockFunctionBody()),
            FunctionDeclaration("baz", ParameterList(), null, BlockFunctionBody()),
            FunctionDeclaration("qux", ParameterList(), null, BlockFunctionBody()),
            FunctionDeclaration("qwe", ParameterList(), null, BlockFunctionBody())));

        // Act
        var cfg = FunctionToCfg(program);
        var dot = cfg.ToDot();

        // Assert
        await Verify(dot, this.settings);
    }
}
