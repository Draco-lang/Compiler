using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Api.CodeFixes;

public sealed class ImportCodeFixProvider : CodeFixProvider
{
    private SyntaxTree SyntaxTree { get; }
    private SyntaxRange Range { get; }
    private SemanticModel SemanticModel { get; }

    public ImportCodeFixProvider(SyntaxTree tree, SyntaxRange range, SemanticModel semanticModel)
    {
        this.SyntaxTree = tree;
        this.Range = range;
        this.SemanticModel = semanticModel;
    }

    internal override DiagnosticTemplate DiagnosticToFix => SymbolResolutionErrors.ImportNotAtTop;

    internal override ImmutableArray<CodeFix> CodeFixes => ImmutableArray.Create(
        new CodeFix("Move import statement to be at the top of a scope", this.TopOfScope),
        new CodeFix("Move import statement  to be at the top of a file", this.TopOfFile));

    private ImmutableArray<TextEdit> TopOfScope()
    {
        var import = this.SyntaxTree.TraverseSubtreesIntersectingRange(this.Range).LastOrDefault(x => x is ImportDeclarationSyntax);
        if (import is null) throw new InvalidOperationException();
    }

    private ImmutableArray<TextEdit> TopOfFile() => ImmutableArray<TextEdit>.Empty;
}
