using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Syntax.Extensions;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Api.Services.CodeFixes;

/// <summary>
/// Provides <see cref="CodeFix"/>es for import issues.
/// </summary>
public sealed class ImportCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> DiagnosticCodes { get; } = [SymbolResolutionErrors.ImportNotAtTop.Code];

    public override ImmutableArray<CodeFix> GetCodeFixes(Diagnostic diagnostic, SemanticModel semanticModel, SourceSpan span) =>
        GetCodeFixesImpl(diagnostic, semanticModel, span).ToImmutableArray();

    private static IEnumerable<CodeFix> GetCodeFixesImpl(Diagnostic diagnostic, SemanticModel semanticModel, SourceSpan span)
    {
        var tree = semanticModel.Tree;

        // Check if the diagnostic is found for the import syntax
        var importSyntax = semanticModel.Tree.Root
            .TraverseIntersectingSpan(span)
            .OfType<ImportDeclarationSyntax>()
            .FirstOrDefault(import => diagnostic.Location.Span?.Intersects(import.Span) ?? false);

        // If not found, bail out
        if (importSyntax is null) yield break;

        // Otherwise, we want to generate 2 options, depending on where the import is
        // The 2 options are
        //  - Move the import to the top of the file
        //  - Move the import to the top of the scope
        // If the import is already file-level, we don't need to generate the second option

        if (importSyntax.Parent?.Parent is CompilationUnitSyntax)
        {
            // Only top of file is relevant
            var topOfFile = TopOfFile(importSyntax);
            if (topOfFile is null) yield break;

            yield return new CodeFix("move import to top of file", tree.CalculateEdits(topOfFile));
        }
        else
        {
            // Both options are relevant
            var topOfFile = TopOfFile(importSyntax);
            var topOfScope = TopOfScope(importSyntax);

            if (topOfFile is not null) yield return new CodeFix("move import to top of file", tree.CalculateEdits(topOfFile));
            if (topOfScope is not null) yield return new CodeFix("move import to top of scope", tree.CalculateEdits(topOfScope));
        }
    }

    private static SyntaxTree? TopOfFile(ImportDeclarationSyntax importSyntax)
    {
        var tree = importSyntax.Tree;
        if (tree.Root is not CompilationUnitSyntax compilationUnit) return null;

        return MoveToTop(importSyntax, compilationUnit.Declarations, declaration => declaration);
    }

    private static SyntaxTree? TopOfScope(ImportDeclarationSyntax importSyntax)
    {
        // Search for the first block that contains the import
        var ancestorStatements = FindScopeAncestor(importSyntax);
        if (ancestorStatements is null) return null;

        return MoveToTop(importSyntax, ancestorStatements, statement => (statement as DeclarationStatementSyntax)?.Declaration);
    }

    private static SyntaxTree? MoveToTop<TNode>(
        ImportDeclarationSyntax importSyntax,
        SyntaxList<TNode> syntaxList,
        Func<TNode, DeclarationSyntax?> getDeclaration)
        where TNode : SyntaxNode
    {
        var tree = importSyntax.Tree;

        // We need to navigate to the end of the imports
        var lastImportOnTop = syntaxList
            .TakeWhile(node => getDeclaration(node) is ImportDeclarationSyntax)
            .LastOrDefault();

        if (lastImportOnTop is null)
        {
            // We insert before the first declaration
            return tree.InsertBefore(importSyntax, syntaxList[0]);
        }
        else
        {
            // Insert after the last import
            return tree.InsertAfter(importSyntax, lastImportOnTop);
        }
    }

    private static SyntaxList<StatementSyntax>? FindScopeAncestor(ImportDeclarationSyntax importSyntax)
    {
        // Search for the first block that contains the import
        var scope = importSyntax.Parent;
        while (scope is not null)
        {
            if (scope is BlockExpressionSyntax block) return block.Statements;
            if (scope is BlockFunctionBodySyntax blockFunctionBody) return blockFunctionBody.Statements;
            scope = scope.Parent;
        }
        return default;
    }
}
