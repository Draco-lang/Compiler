using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffEngine;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols.Source;
using VerifyTests;
using VerifyXunit;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;
using static VerifyXunit.Verifier;

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
}
